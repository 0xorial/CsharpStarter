using Flurl.Http;
using Scaffold.Api;
using Scaffold.Tests.Utils;

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

    [TestMethod]
    public async Task Writing_items_works()
    {
        using var h = await TestHelper.Create();

        var client = h.GetClient();
        var response1 = await client.Request().PostAsync(new StringContent("test123"));
        var response2 = await client.Request().PostAsync(new StringContent("test456"));
        var response3 = await client.Request("list").GetJsonAsync<ItemRecord[]>();
        await response1.AssertOk();
        await response2.AssertOk();
        response3.Length.Should().Be(2);
        response3.Should().ContainSingle(x => x.Text == "test123");
        response3.Should().ContainSingle(x => x.Text == "test456");
    }

    [TestMethod]
    public async Task Translates()
    {
        using var h = await TestHelper.Create();

        var client = h.GetClient();
        var response1 = await client.Request().PostAsync(new StringContent("test123"));
        await response1.AssertOk();

        h.ExternalServices.TimeProvider.Advance(TimeSpan.FromSeconds(11));
        var response3 = await client.Request("list").GetJsonAsync<ListedItemDto[]>();
        response3[0].TranslatedText.Should().EndWith("_translated");

        h.ExternalServices.TranslationService.TotalCalls.Should().Be(1);
    }
}