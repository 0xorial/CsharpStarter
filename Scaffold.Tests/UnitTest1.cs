using FluentAssertions;
using Flurl.Http;

namespace Scaffold.Tests;

[TestClass]
public class UnitTest1
{
    [TestMethod]
    public async Task GetHelloWorld()
    {
        using var h = await TestHelper.Create();
        var client = h.GetClient();
        var response = await client.Request().GetAsync();
        var responseText = await response.GetStringAsync();
        responseText.Should().Be("Hello World!");
    }
}