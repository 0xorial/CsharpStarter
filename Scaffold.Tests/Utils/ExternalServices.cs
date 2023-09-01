using Microsoft.Extensions.Time.Testing;

namespace Scaffold.Tests.Utils;

public class ExternalServices
{
    public readonly TestTranslationService TranslationService = new();
    public readonly FakeTimeProvider TimeProvider = new();

}