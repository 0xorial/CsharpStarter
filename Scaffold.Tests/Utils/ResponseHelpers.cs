using Flurl.Http;

namespace Scaffold.Tests.Utils;

public static class ResponseHelpers
{
    public static async Task AssertOk(this IFlurlResponse response)
    {
        string responseText = string.Empty;
        if (response.StatusCode != 200)
        {
            responseText = await response.GetStringAsync();
        }

        response.StatusCode.Should().Be(200, responseText);
    }
}