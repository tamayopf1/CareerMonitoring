FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0

# Install Chrome dependencies (your existing code)
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

# Install ChromeDriver
RUN CHROME_DRIVER_VERSION=$(curl -sS chromedriver.storage.googleapis.com/LATEST_RELEASE) && \
    wget -N https://chromedriver.storage.googleapis.com/$CHROME_DRIVER_VERSION/chromedriver_linux64.zip && \
    unzip chromedriver_linux64.zip && \
    mv chromedriver /usr/bin/chromedriver && \
    chmod +x /usr/bin/chromedriver

# Copy function app files from the ASMLMonitoring subdirectory
COPY ./ASMLMonitoring /home/site/wwwroot
WORKDIR /home/site/wwwroot

# Pre-warm extension bundles (this helps avoid timeout during startup)
ENV AzureWebJobsScriptRoot=/home/site/wwwroot
RUN if [ -f "host.json" ]; then \
    timeout 300 /azure-functions-host/Microsoft.Azure.WebJobs.Script.WebHost || true; \
    fi

# Set environment variables
ENV PATH="/usr/bin/chromedriver:${PATH}" \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true \
    FUNCTIONS_WORKER_RUNTIME=dotnet-isolated \
    TIMER_SCHEDULE="0 * * * * *" \
    EMAIL_FROM="asmlmonitoring@gmail.com" \
    EMAIL_RECIPIENTS="prince.jake.tamayo@accenture.com"