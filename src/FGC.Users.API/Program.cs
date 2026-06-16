using System.Text;
using Azure.Messaging.ServiceBus;
using FGC.Users.API.Middlewares;
using FGC.Users.Domain.Entities;
using FGC.Users.Domain.Enums;
using FGC.Users.Domain.ValueObjects;
using FGC.Users.Application.Commands.AuthenticateUser;
using FGC.Users.Application.Commands.CreateUser;
using FGC.Users.Application.Commands.UpdateProfile;
using FGC.Users.Application.Interfaces;
using FGC.Users.Application.Queries.GetProfile;
using FGC.Users.Infrastructure.Audit;
using FGC.Users.Infrastructure.EventPublishers;
using FGC.Users.Infrastructure.Identity;
using FGC.Users.Infrastructure.Persistence;
using FGC.Users.Infrastructure.Persistence.Repositories;
using FGC.Users.Infrastructure.Security;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Sinks.ApplicationInsights.TelemetryConverters;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    var configuration = builder.Configuration;

    // Serilog
    builder.Host.UseSerilog((context, svcProvider, loggerConfig) => loggerConfig
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(svcProvider)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId()
        .Enrich.WithProperty("ServiceName", "FGC.Users")
        .WriteTo.Console(outputTemplate:
            "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
        .WriteTo.Conditional(
            _ => !string.IsNullOrEmpty(context.Configuration["ApplicationInsights:ConnectionString"]),
            wt => wt.ApplicationInsights(
                context.Configuration["ApplicationInsights:ConnectionString"],
                new TraceTelemetryConverter())));

    // Controllers
    builder.Services.AddControllers()
        .AddJsonOptions(opts => { opts.JsonSerializerOptions.PropertyNamingPolicy = null; });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Description = "Insira o token JWT. Exemplo: eyJhbGciOi..."
        });
        options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    // Database — Azure SQL Serverless (auto-pause/resume may take up to 60s)
    var connectionString = configuration.GetValue<string>("ConnectionStrings:Default")
        ?? "Server=localhost,1433;Database=FGCUsersDb;User Id=sa;Password=Your_password123;TrustServerCertificate=true;";
    builder.Services.AddDbContext<ApplicationDbContext>(opt =>
        opt.UseSqlServer(connectionString, sql =>
            sql.EnableRetryOnFailure(
                maxRetryCount: 6,
                maxRetryDelay: TimeSpan.FromSeconds(60),
                errorNumbersToAdd: null)));

    // CQRS Handlers
    builder.Services.AddScoped<CreateUserCommandHandler>();
    builder.Services.AddScoped<AuthenticateUserCommandHandler>();
    builder.Services.AddScoped<UpdateProfileCommandHandler>();
    builder.Services.AddScoped<GetProfileQueryHandler>();

    // Infrastructure
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<IAuditService, AuditService>();
    builder.Services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
    builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

    // Event Publisher
    var serviceBusConn = configuration.GetValue<string>("ServiceBus:ConnectionString");
    if (!string.IsNullOrEmpty(serviceBusConn))
    {
        builder.Services.AddSingleton(_ => new ServiceBusClient(serviceBusConn));
        builder.Services.AddSingleton<IEventPublisher, ServiceBusEventPublisher>();
    }
    else
    {
        builder.Services.AddSingleton<IEventPublisher, InMemoryEventPublisher>();
    }

    // Authentication
    var jwtIssuer = configuration.GetValue<string>("Jwt:Issuer") ?? "fgc.local";
    var jwtAudience = configuration.GetValue<string>("Jwt:Audience") ?? "fgc.clients";
    var jwtKey = configuration.GetValue<string>("Jwt:Key") ?? "super-secret-key-for-dev-environment-only";
    var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true
        };
    });

    builder.Services.AddAuthorization();

    // FluentValidation
    builder.Services.AddFluentValidationAutoValidation();
    builder.Services.AddValidatorsFromAssemblyContaining<CreateUserCommandValidator>();

    // Middleware services
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICorrelationContext, CorrelationContext>();

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();

        // Seed: create default admin user if not exists
        if (!db.Users.IgnoreQueryFilters().Any(u => u.Email == "superadmin@fgc.com"))
        {
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var hashedPassword = new Password(passwordHasher.Hash("superadmin!123"));
            var admin = User.Create("Super Admin", "superadmin@fgc.com", hashedPassword, UserRole.Admin);
            db.Users.Add(admin);
            db.SaveChanges();
        }
    }

    app.UseMiddleware<CorrelationMiddleware>();
    app.UseSerilogRequestLogging();
    app.UseMiddleware<RequestResponseLoggingMiddleware>();

    app.UseSwagger();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("FGC.Users API");
        options.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
        options.WithOpenApiRoutePattern("/swagger/{documentName}/swagger.json");
    });

    app.UseHttpsRedirection();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
