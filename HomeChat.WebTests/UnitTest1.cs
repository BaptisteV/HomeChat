using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;

namespace HomeChat.WebTests;

public class UnitTest1
{
    private static IWebDriver _driverChrome;

    public UnitTest1()
    {
        _driverChrome = new FirefoxDriver();
    }

    [Fact]
    public void Test1()
    {
        _driverChrome.Navigate().GoToUrl("http://monsite.com");
    }
}