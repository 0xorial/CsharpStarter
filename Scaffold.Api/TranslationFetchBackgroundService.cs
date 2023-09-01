using System.Data;
using Dapper;

namespace Scaffold.Api;

public class TranslationFetchBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public TranslationFetchBackgroundService(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(500, stoppingToken);

            using var scope = _serviceScopeFactory.CreateScope();
            using var connection = scope.ServiceProvider.GetRequiredService<IDbConnection>();
            connection.Open();
            var items = (await connection.QueryAsync<ItemRecord>(
                @$"SELECT * FROM Items 
LEFT OUTER JOIN Translations ON Items.Id = Translations.ItemId
WHERE TranslatedText IS NULL")).ToArray();
            if (items.Any())
            {
                var translationService = scope.ServiceProvider.GetRequiredService<ITranslationService>();
                foreach (var itemRecord in items)
                {
                    var translation = await translationService.GetTranslation(itemRecord.Text);
                    await connection.ExecuteAsync(
                        $"INSERT INTO Translations (ItemId, TranslatedText) VALUES ({itemRecord.Id}, '{translation}')");
                }
            }
        }
    }
}