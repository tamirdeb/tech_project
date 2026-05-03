using AventStack.ExtentReports;
using AventStack.ExtentReports.Reporter;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace WikipediaAutomation.Tests.Tests;

/// <summary>
/// Playwright lifecycle + ExtentReports wiring. All test fixtures inherit from this.
/// </summary>
public abstract class BaseTest : PageTest
{
    private static ExtentReports? _extent;
    private static readonly object Lock = new();
    protected ExtentTest? Report;

    [OneTimeSetUp]
    public void InitReport()
    {
        lock (Lock)
        {
            if (_extent != null) return;

            var dir = Path.Combine(TestContext.CurrentContext.WorkDirectory, "TestResults");
            Directory.CreateDirectory(dir);

            var spark = new ExtentSparkReporter(Path.Combine(dir, "Report.html"));
            spark.Config.Theme = AventStack.ExtentReports.Reporter.Config.Theme.Dark;
            spark.Config.DocumentTitle = "Wikipedia Automation Report";
            spark.Config.ReportName = "Test Results";

            _extent = new ExtentReports();
            _extent.AttachReporter(spark);
            _extent.AddSystemInfo("Browser", "Chromium");
            _extent.AddSystemInfo("Framework", "Playwright + NUnit");
        }
    }

    [SetUp]
    public void StartTest()
    {
        var name = TestContext.CurrentContext.Test.Name;
        Report = _extent!.CreateTest(name);
    }

    [TearDown]
    public async Task EndTest()
    {
        var status = TestContext.CurrentContext.Result.Outcome.Status;
        var message = TestContext.CurrentContext.Result.Message;

        if (status == TestStatus.Failed)
        {
            try
            {
                var screenshot = await Page.ScreenshotAsync(new() { FullPage = true });
                Report?.Fail(message,
                    MediaEntityBuilder
                        .CreateScreenCaptureFromBase64String(Convert.ToBase64String(screenshot))
                        .Build());
            }
            catch
            {
                Report?.Fail(message);
            }
        }
        else if (status == TestStatus.Passed)
        {
            Report?.Pass("Passed");
        }
    }

    [OneTimeTearDown]
    public void FlushReport() => _extent?.Flush();

    protected void Log(string msg) => Report?.Info(msg);
}
