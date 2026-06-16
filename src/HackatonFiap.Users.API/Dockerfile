FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src
COPY ["src/HackatonFiap.Users.API/HackatonFiap.Users.API.csproj", "HackatonFiap.Users.API/"]
COPY ["src/HackatonFiap.Users.Application/HackatonFiap.Users.Application.csproj", "HackatonFiap.Users.Application/"]
COPY ["src/HackatonFiap.Users.Domain/HackatonFiap.Users.Domain.csproj", "HackatonFiap.Users.Domain/"]
COPY ["src/HackatonFiap.Users.Infrastructure/HackatonFiap.Users.Infrastructure.csproj", "HackatonFiap.Users.Infrastructure/"]
RUN dotnet restore "HackatonFiap.Users.API/HackatonFiap.Users.API.csproj"
COPY src/ .
RUN dotnet publish "HackatonFiap.Users.API/HackatonFiap.Users.API.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

USER $APP_UID
ENTRYPOINT ["dotnet", "HackatonFiap.Users.API.dll"]
