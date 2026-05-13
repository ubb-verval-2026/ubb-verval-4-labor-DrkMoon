using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using FluentAssertions;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace DatesAndStuff.Web.Tests;

[TestFixture]
public class BlazedemoTests
{
    private IWebDriver driver;
    private StringBuilder verificationErrors;
    private const string BaseURL = "https://blazedemo.com/";
    private bool acceptNextAlert = true;

    private Process? _blazorProcess;

    [OneTimeSetUp]
    public void StartBlazorServer()
    {
        var webProjectPath = Path.GetFullPath(Path.Combine(
            Assembly.GetExecutingAssembly().Location,
            "../../../../../../src/DatesAndStuff.Web/DatesAndStuff.Web.csproj"
            ));

        var webProjFolderPath = Path.GetDirectoryName(webProjectPath);

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            //Arguments = $"run --project \"{webProjectPath}\"",
            Arguments = "dotnet run --no-build",
            WorkingDirectory = webProjFolderPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        _blazorProcess = Process.Start(startInfo);

        // Wait for the app to become available
        var client = new HttpClient();
        var timeout = TimeSpan.FromSeconds(30);
        var start = DateTime.Now;

        while (DateTime.Now - start < timeout)
        {
            try
            {
                var result = client.GetAsync(BaseURL).Result;
                if (result.IsSuccessStatusCode)
                {
                    break;
                }
            }
            catch (Exception e)
            {
                Thread.Sleep(1000);
            }
        }
    }

    [OneTimeTearDown]
    public void StopBlazorServer()
    {
        if (_blazorProcess != null && !_blazorProcess.HasExited)
        {
            _blazorProcess.Kill(true);
            _blazorProcess.Dispose();
        }
    }

    [SetUp]
    public void SetupTest()
    {
        driver = new ChromeDriver();
        verificationErrors = new StringBuilder();
    }

    [TearDown]
    public void TeardownTest()
    {
        try
        {
            driver.Quit();
            driver.Dispose();
        }
        catch (Exception)
        {
            // Ignore errors if unable to close the browser
        }
        Assert.That(verificationErrors.ToString(), Is.EqualTo(""));
    }

    [Test]
    public void Blazedemo_SearchingFlightsBetweenMexicoCityAndDublin_ShouldreturnAtLeastThree()
    {
        // Arrange
        double minPrice = 400.0; 
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
        driver.Navigate().GoToUrl(BaseURL);
        driver.FindElement(By.Name("fromPort")).Click();
        new SelectElement(driver.FindElement(By.Name("fromPort"))).SelectByText("Mexico City");
        driver.FindElement(By.Name("toPort")).Click();
        new SelectElement(driver.FindElement(By.Name("toPort"))).SelectByText("Dublin");
        driver.FindElement(By.XPath("//input[@value='Find Flights']")).Click();
        var flightsTable = wait.Until(ExpectedConditions.ElementExists(By.CssSelector("table.table")));
        var flightRows = flightsTable.FindElements(By.CssSelector("tbody tr"));

        flightRows.Count.Should().BeGreaterThanOrEqualTo(3, "There should be at least 3 flights available between Mexico City and Dublin");
        int i = 0;
        foreach (var row in flightRows)
        {
            // A BlazeDemo táblázatában az ár a 6. oszlop (td[6])
            var priceText = row.FindElement(By.XPath("./td[6]")).Text;

            // Szöveg átalakítása számmal összehasonlítható formátumra ($472.56 -> 472.56)
            if (double.TryParse(priceText.Replace("$", ""), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double actualPrice))
            {
                if (actualPrice < minPrice)
                {
                    i++;
                    ITakesScreenshot elementScreenshot = row as ITakesScreenshot;
                    Screenshot shot = elementScreenshot.GetScreenshot();

                    string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    shot.SaveAsFile(Path.Combine(desktopPath, $"CheapFlight_RowOnly{i}.png"));
                }
            }
        }
    }

    private bool IsElementPresent(By by)
    {
        try
        {
            driver.FindElement(by);
            return true;
        }
        catch (NoSuchElementException)
        {
            return false;
        }
    }

    private bool IsAlertPresent()
    {
        try
        {
            driver.SwitchTo().Alert();
            return true;
        }
        catch (NoAlertPresentException)
        {
            return false;
        }
    }

    private string CloseAlertAndGetItsText()
    {
        try
        {
            IAlert alert = driver.SwitchTo().Alert();
            string alertText = alert.Text;
            if (acceptNextAlert)
            {
                alert.Accept();
            }
            else
            {
                alert.Dismiss();
            }
            return alertText;
        }
        finally
        {
            acceptNextAlert = true;
        }
    }
}