namespace Scaffold.Api;

public interface ITranslationService
{
    Task<string> GetTranslation(string text);
}

public class TranslationService: ITranslationService
{
    public Task<string> GetTranslation(string text)
    {
        // in real world it would make a call to another service
        throw new NotImplementedException();
    }
}
