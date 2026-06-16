FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src
COPY ["src/FGC.Users.API/FGC.Users.API.csproj", "FGC.Users.API/"]
COPY ["src/FGC.Users.Application/FGC.Users.Application.csproj", "FGC.Users.Application/"]
COPY ["src/FGC.Users.Domain/FGC.Users.Domain.csproj", "FGC.Users.Domain/"]
COPY ["src/FGC.Users.Infrastructure/FGC.Users.Infrastructure.csproj", "FGC.Users.Infrastructure/"]
RUN dotnet restore "FGC.Users.API/FGC.Users.API.csproj"
COPY src/ .
RUN dotnet publish "FGC.Users.API/FGC.Users.API.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

USER $APP_UID
ENTRYPOINT ["dotnet", "FGC.Users.API.dll"]
