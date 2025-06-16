
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0 AS base
WORKDIR /home/site/wwwroot
EXPOSE 8080

# Install system dependencies required by Playwright
RUN apt-get update && apt-get install -y \
    wget \
    unzip \
    fontconfig \
    fonts-liberation \
    libasound2 \
    libatk-bridge2.0-0 \
    libatk1.0-0 \
    libatspi2.0-0 \
    libcairo2 \
    libcups2 \
    libdbus-1-3 \
    libdrm2 \
    libfontconfig1 \
    libgbm1 \
    libgconf-2-4 \
    libgdk-pixbuf2.0-0 \
    libglib2.0-0 \
    libgtk-3-0 \
    libharfbuzz0b \
    libnspr4 \
    libnss3 \
    libpango-1.0-0 \
    libpangocairo-1.0-0 \
    libx11-6 \
    libx11-xcb1 \
    libxcb1 \
    libxcomposite1 \
    libxcursor1 \
    libxdamage1 \
    libxext6 \
    libxfixes3 \
    libxi6 \
    libxrandr2 \
    libxrender1 \
    libxss1 \
    libxtst6 \
    xvfb \
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

# Install Playwright and browsers in the build stage
FROM build AS playwright-install
WORKDIR /app/build
RUN dotnet tool install --global Microsoft.Playwright.CLI
ENV PATH="${PATH}:/root/.dotnet/tools"
RUN playwright install chromium --with-deps

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /home/site/wwwroot
COPY --from=publish /app/publish .
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true

# Copy Playwright browsers from the build stage
COPY --from=playwright-install /root/.cache/ms-playwright /root/.cache/ms-playwright

# Install Playwright CLI in the final image
RUN dotnet tool install --global Microsoft.Playwright.CLI
ENV PATH="${PATH}:/root/.dotnet/tools"

# Copy Playwright browsers to the path where runtime will look
COPY --from=playwright-install /root/.cache/ms-playwright /home/.cache/ms-playwright

# Set environment variables
ENV PATH="/usr/bin/chromedriver:${PATH}" \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true \
    FUNCTIONS_WORKER_RUNTIME=dotnet-isolated \
    TIMER_SCHEDULE="0 * * * * *" \
    EMAIL_FROM="asmlmonitoring@gmail.com" \
    EMAIL_RECIPIENTS="prince.jake.tamayo@accenture.com" \
    PLAYWRIGHT_BROWSERS_PATH=/home/.cache/ms-playwright