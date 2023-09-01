using FluentAssertions;
using Flurl.Http;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Scaffold.Tests;

[TestClass]
public class UnitTest1
{
    [TestMethod]
    public async Task GetHelloWorld()
    {
        await using var factory = new WebApplicationFactory<Program>();
        var client = new FlurlClient(factory.CreateClient());
        var response = await client.Request().GetAsync();
        var responseText = await response.GetStringAsync();
        responseText.Should().Be("Hello World!");
    }
}