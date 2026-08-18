FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props global.json .editorconfig ./
COPY src/Namadno.AI.Support.Domain/Namadno.AI.Support.Domain.csproj src/Namadno.AI.Support.Domain/
COPY src/Namadno.AI.Support.Application/Namadno.AI.Support.Application.csproj src/Namadno.AI.Support.Application/
COPY src/Namadno.AI.Support.Infrastructure/Namadno.AI.Support.Infrastructure.csproj src/Namadno.AI.Support.Infrastructure/
COPY src/Namadno.AI.Support.Api/Namadno.AI.Support.Api.csproj src/Namadno.AI.Support.Api/

RUN dotnet restore src/Namadno.AI.Support.Api/Namadno.AI.Support.Api.csproj

COPY src/ src/
COPY copy/ copy/
RUN dotnet publish src/Namadno.AI.Support.Api/Namadno.AI.Support.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_EnableDiagnostics=0

EXPOSE 8080
USER $APP_UID

HEALTHCHECK --interval=15s --timeout=5s --start-period=40s --retries=5 \
    CMD curl -fsS http://127.0.0.1:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "Namadno.AI.Support.Api.dll"]
