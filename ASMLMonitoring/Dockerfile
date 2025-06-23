FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0 AS base
WORKDIR /home/site/wwwroot
EXPOSE 8080

# Install Chrome dependencies
RUN apt-get update && apt-get install -y \
    wget gnupg unzip curl libxss1 libappindicator1 \
    libnss3 libx11-xcb1 libxcomposite1 libxcursor1 \
    libxdamage1 libgbm1 libvulkan1 libxi6 libxtst6 fonts-liberation \
    libasound2 libatk-bridge2.0-0 libgtk-3-0 xdg-utils \
    --no-install-recommends && \
    apt-get clean && rm -rf /var/lib/apt/lists/*

# Install Chrome
RUN wget https://dl.google.com/linux/direct/google-chrome-stable_current_amd64.deb && \
    dpkg -i google-chrome-stable_current_amd64.deb || apt-get -fy install

# Install ChromeDriver (Updated method for Chrome 115+)
RUN CHROME_VERSION=$(google-chrome --version | cut -d ' ' -f3 | cut -d '.' -f1-3) && \
    echo "Chrome version: $CHROME_VERSION" && \
    CHROMEDRIVER_VERSION=$(curl -s "https://googlechromelabs.github.io/chrome-for-testing/LATEST_RELEASE_$CHROME_VERSION") && \
    echo "ChromeDriver version: $CHROMEDRIVER_VERSION" && \
    wget -N "https://storage.googleapis.com/chrome-for-testing-public/$CHROMEDRIVER_VERSION/linux64/chromedriver-linux64.zip" && \
    unzip chromedriver-linux64.zip && \
    mv chromedriver-linux64/chromedriver /usr/bin/chromedriver && \
    chmod +x /usr/bin/chromedriver && \
    rm -rf chromedriver-linux64.zip chromedriver-linux64

# Verify installation
RUN google-chrome --version && chromedriver --version

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
    TIMER_SCHEDULE_EN="0 * * * * *" \
    TIMER_SCHEDULE_DE="0 * * * * *" \
    EMAIL_FROM="asmlmonitoring@gmail.com"