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

    [Function("ASMLMonitoring")]
    public async Task Run([TimerTrigger("%TIMER_SCHEDULE%")] TimerInfo myTimer)
    {
            var options = new ChromeOptions();
            options.AddArgument("--headless");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            options.BinaryLocation = "/usr/bin/google-chrome";

            using var driver = new ChromeDriver(options);

            driver.Navigate().GoToUrl("https://www.asml.com/en/careers/find-your-job");

            // Wait up to 10 seconds for the job list to load
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
            var selector = Environment.GetEnvironmentVariable("JOB_ELEMENT_SELECTOR") ?? "li[class^='orders-']";
            var jobItems = driver.FindElements(By.CssSelector(selector));

            if (jobItems.Count == 0)
            {
                _logger.LogInformation("No Joblisting found.");
                await SendEmailAsync("No Joblisting found.");    
            }
            else
            {
                _logger.LogInformation($"Jobs found.");
            }

            driver.Quit();
    }

    public async Task SendEmailAsync(string messageBody)
    {
        var apiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY");
        var recipientsString = Environment.GetEnvironmentVariable("EMAIL_RECIPIENTS");
        var fromEmail = Environment.GetEnvironmentVariable("EMAIL_FROM");
        var subject = Environment.GetEnvironmentVariable("EMAIL_SUBJECT") ?? "ASML Jobs Monitoring Alert: No Jobs Found.";

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
        Console.WriteLine($"Email sent to {recipients.Count} recipients. Status: {response.StatusCode}");
    }
}
