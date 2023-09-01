using System.Data;
using Dapper;

namespace Scaffold.Api;

public class TranslationFetchBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly TimeProvider _timeProvider;

    public TranslationFetchBackgroundService(IServiceScopeFactory serviceScopeFactory, TimeProvider timeProvider)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _timeProvider = timeProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await _timeProvider.Delay(TimeSpan.FromSeconds(10), stoppingToken);
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