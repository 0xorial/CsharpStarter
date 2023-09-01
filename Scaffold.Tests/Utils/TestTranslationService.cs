using Scaffold.Api;

namespace Scaffold.Tests.Utils;

public class TestTranslationService : ITranslationService
{
    public int TotalCalls = 0;
    public async Task<string> GetTranslation(string text)
    {
        TotalCalls++;
        return text + "_translated";
    }
}