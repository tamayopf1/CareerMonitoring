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

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ASMLMonitoring.csproj", "."]
RUN dotnet restore "./ASMLMonitoring.csproj"
COPY . .
RUN dotnet publish "./ASMLMonitoring.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Install Playwright CLI and browsers in the publish output
WORKDIR /app/publish
RUN dotnet tool install --global Microsoft.Playwright.CLI \
    && export PATH="$PATH:/root/.dotnet/tools" \
    && playwright install --with-deps

RUN pwsh bin/Release/net8.0/playwright.ps1 install

FROM base AS final
WORKDIR /home/site/wwwroot
COPY --from=build /app/publish .
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true \
    FUNCTIONS_WORKER_RUNTIME=dotnet-isolated \
    TIMER_SCHEDULE="0 * * * * *" \
    EMAIL_FROM="asmlmonitoring@gmail.com" \
    EMAIL_RECIPIENTS="prince.jake.tamayo@accenture.com" \
    PLAYWRIGHT_BROWSERS_PATH=0 

# No need to copy browser cache manually; PLAYWRIGHT_BROWSERS_PATH=0 ensures correct location
