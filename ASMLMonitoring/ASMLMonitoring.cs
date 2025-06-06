using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium;
using SendGrid.Helpers.Mail;
using SendGrid;
using System.Net;

namespace ASMLMonitoring;

public class ASMLMonitoring
{
    private readonly ILogger _logger;

    public ASMLMonitoring(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<ASMLMonitoring>();
    }

    [Function("ASMLMonitoring_EN")]
    public async Task RunEN([TimerTrigger("%TIMER_SCHEDULE_EN%")] TimerInfo myTimer)
    => await Monitor("https://www.asml.com/en/careers/find-your-job");

    [Function("ASMLMonitoring_DE")]
    public async Task RunDE([TimerTrigger("%TIMER_SCHEDULE_EN%")] TimerInfo myTimer)
    => await Monitor("https://www.asml.com/de-de/karriere/stellenangebote");

    public async Task Monitor(string url)
    {
            var options = new ChromeOptions();
            options.AddArgument("--headless");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            options.BinaryLocation = "/usr/bin/google-chrome";

            using var driver = new ChromeDriver(options);

            driver.Navigate().GoToUrl(url);

            // Wait up to 10 seconds for the job list to load
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
            var selector = Environment.GetEnvironmentVariable("JOB_ELEMENT_SELECTOR") ?? "li[class^='orders-']";
            var jobItems = driver.FindElements(By.CssSelector(selector));

            if (jobItems.Count == 0)
            {
                _logger.LogInformation("No Joblisting found.");
                await SendEmailAsync($"No Joblisting found on {url}", url);    
            }
            else
            {
                _logger.LogInformation($"Jobs found on {url}");
            }

            driver.Quit();
    }

    public async Task SendEmailAsync(string messageBody, string url)
    {
        var apiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY");
        var recipientsString = Environment.GetEnvironmentVariable("EMAIL_RECIPIENTS");
        var fromEmail = Environment.GetEnvironmentVariable("EMAIL_FROM");
        var subject = Environment.GetEnvironmentVariable("EMAIL_SUBJECT") ?? $"ASML Jobs Monitoring Alert: No Jobs Found on {url}";

        if (string.IsNullOrEmpty(recipientsString))
        {
            throw new InvalidOperationException("EMAIL_RECIPIENTS environment variable not set");
        }

        var recipients = recipientsString.Split(',')
            .Select(email => email.Trim())
            .Where(email => !string.IsNullOrEmpty(email))
            .ToList();

        var client = new SendGridClient(apiKey);
        var msg = new SendGridMessage();
        msg.SetFrom(fromEmail, "ASML Jobs Monitoring");
        msg.SetSubject(subject);
        msg.AddContent(MimeType.Text, messageBody);

        foreach (var email in recipients)
        {
            msg.AddTo(email);
        }

        var response = await client.SendEmailAsync(msg);
        var recipientList = string.Join(", ", recipients);
        Console.WriteLine($"Email sent to {recipients.Count} recipients [{recipientList}]. Status: {response.StatusCode}");
    }
}
