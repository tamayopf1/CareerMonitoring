FROM mcr.microsoft.com/playwright/dotnet:v1.40.0-jammy AS playwright-base

FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0 AS base
WORKDIR /home/site/wwwroot
EXPOSE 8080

# Copy Playwright browsers and dependencies from the Playwright base image
COPY --from=playwright-base /ms-playwright /ms-playwright
COPY --from=playwright-base /usr/lib/x86_64-linux-gnu /usr/lib/x86_64-linux-gnu
COPY --from=playwright-base /usr/bin/playwright /usr/bin/playwright

# Install minimal required system packages
RUN apt-get update && apt-get install -y \
    libnss3 \
    libatk-bridge2.0-0 \
    libdrm2 \
    libxcomposite1 \
    libxdamage1 \
    libxrandr2 \
    libgbm1 \
    libxss1 \
    libasound2 \
    --no-install-recommends && \
    apt-get clean && rm -rf /var/lib/apt/lists/*

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ASMLMonitoring.csproj", "."]
RUN dotnet restore "./ASMLMonitoring.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./ASMLMonitoring.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ASMLMonitoring.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /home/site/wwwroot
COPY --from=publish /app/publish .
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true

# Set environment variables
ENV PATH="/usr/bin/chromedriver:${PATH}" \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true \
    FUNCTIONS_WORKER_RUNTIME=dotnet-isolated \
    TIMER_SCHEDULE="0 * * * * *" \
    EMAIL_FROM="asmlmonitoring@gmail.com" \
    EMAIL_RECIPIENTS="prince.jake.tamayo@accenture.com"
    PLAYWRIGHT_BROWSERS_PATH=/ms-playwright