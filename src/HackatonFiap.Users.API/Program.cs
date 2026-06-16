using System.Text;
using Azure.Messaging.ServiceBus;
using HackatonFiap.Users.API.Middlewares;
using HackatonFiap.Users.Domain.Entities;
using HackatonFiap.Users.Domain.Enums;
using HackatonFiap.Users.Domain.ValueObjects;
using HackatonFiap.Users.Application.Commands.AuthenticateUser;
using HackatonFiap.Users.Application.Interfaces;
using HackatonFiap.Users.Application.Queries.GetProfile;
using HackatonFiap.Users.Infrastructure.Audit;
using HackatonFiap.Users.Infrastructure.EventPublishers;
using HackatonFiap.Users.Infrastructure.Identity;
using HackatonFiap.Users.Infrastructure.Persistence;
using HackatonFiap.Users.Infrastructure.Persistence.Repositories;
using HackatonFiap.Users.Infrastructure.Security;
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
        .Enrich.WithProperty("ServiceName", "HackatonFiap.Users")
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
    builder.Services.AddScoped<AuthenticateUserCommandHandler>();
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
    var jwtIssuer = configuration.GetValue<string>("Jwt:Issuer") ?? "conexaosolidaria.local";
    var jwtAudience = configuration.GetValue<string>("Jwt:Audience") ?? "conexaosolidaria.clients";
    var jwtKey = configuration.GetValue<string>("Jwt:Key");
    if (string.IsNullOrEmpty(jwtKey))
    {
        if (!builder.Environment.IsDevelopment())
            throw new InvalidOperationException("Jwt:Key must be configured (Key Vault/secret/env) outside Development.");
        // DEV sem chave configurada: gera chave aleatória e a disponibiliza via IConfiguration (tokens invalidam ao reiniciar).
        jwtKey = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48));
        configuration["Jwt:Key"] = jwtKey;
        Log.Warning("Jwt:Key nao configurada — usando chave aleatoria de DEV (tokens invalidam ao reiniciar).");
    }
    if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
        throw new InvalidOperationException("Jwt:Key must be at least 32 bytes of high-entropy data.");
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
    builder.Services.AddValidatorsFromAssemblyContaining<HackatonFiap.Users.Application.Queries.GetProfile.GetProfileQuery>();

    // Middleware services
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICorrelationContext, CorrelationContext>();

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();

        // Seed: provisional Owner if none exists (hardened in a later task)
        if (!db.Users.IgnoreQueryFilters().Any(u => u.IsOwner))
        {
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var ownerPwd = configuration.GetValue<string>("Owner:Password");
            if (string.IsNullOrEmpty(ownerPwd))
            {
                if (!app.Environment.IsDevelopment())
                    throw new InvalidOperationException("Owner:Password must be configured (Key Vault/secret) outside Development.");
                // DEV sem senha configurada: gera senha forte unica e loga uma vez (sem literal em codigo).
                ownerPwd = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(18)) + "Aa1!";
                Log.Warning("Owner seed criado com senha gerada para DEV (configure Owner:Password para fixar): {OwnerPassword}", ownerPwd);
            }
            var owner = User.CreateOwner(
                Document.Create(configuration.GetValue<string>("Owner:Document") ?? "52998224725", PersonType.Individual),
                configuration.GetValue<string>("Owner:Name") ?? "Owner",
                configuration.GetValue<string>("Owner:Email") ?? "owner@conexaosolidaria.org",
                new Password(passwordHasher.Hash(ownerPwd)));
            db.Users.Add(owner);
            db.SaveChanges();
        }
    }

    app.UseMiddleware<CorrelationMiddleware>();
    app.UseSerilogRequestLogging();
    app.UseMiddleware<RequestResponseLoggingMiddleware>();

    app.UseSwagger();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("HackatonFiap.Users API");
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
