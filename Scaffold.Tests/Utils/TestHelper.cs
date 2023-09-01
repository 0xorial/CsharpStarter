using Flurl.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Respawn;
using Scaffold.Api;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Scaffold.Tests.Utils;

public class TestHelper : IDisposable
{
    public ExternalServices ExternalServices { get; }
    public bool AllowLogsErrors = false;
    private readonly WebApplication _webApplication;
    private readonly AccumulatingLogEventSink _logSink;

    public static async Task<TestHelper> Create(IEnumerable<KeyValuePair<string, string?>>? configOverrides = null)
    {
        var sink = new AccumulatingLogEventSink();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddJsonFile("appsettings.Tests.json")
            .AddEnvironmentVariables()
            .AddInMemoryCollection(configOverrides ?? Array.Empty<KeyValuePair<string, string?>>());
        await Program.ConfigureBuilderAsync(builder);


        var externals = new ExternalServices();

        builder.Services.AddSingleton<TimeProvider>(externals.TimeProvider);
        builder.Services.AddSingleton<ITranslationService>(externals.TranslationService);

        builder.Host.UseSerilog((context, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .WriteTo.Sink(sink);
        });

        var connectionString = builder.Configuration.GetConnectionString("main");
        await Checkpoint.Reset(connectionString);

        var app = builder.Build();

        // this is very useful in particular to capture console output,
        // due to to how Microsoft.VisualStudio.TestPlatform works (see ThreadSafeStringWriter)
        app.GetTestServer().PreserveExecutionContext = true;

        await Program.ConfigureApplicationAsync(app);
        await app.StartAsync();

        return new TestHelper(app, sink, externals);
    }

    private static readonly Checkpoint Checkpoint = new()
    {
        TablesToIgnore = new[] { "VersionInfo" }
    };

    private static void MakeCommonConfiguration(IConfigurationBuilder builder,
        IEnumerable<KeyValuePair<string, string?>> configOverrides)
    {
        builder
            .AddJsonFile("appsettings.Tests.json")
            .AddEnvironmentVariables()
            .AddInMemoryCollection(configOverrides);
    }

    public static void EnsureLocalDbExistsAndMigrateUp()
    {
        var configurationBuilder = new ConfigurationBuilder();
        MakeCommonConfiguration(configurationBuilder, Array.Empty<KeyValuePair<string, string?>>());
        var configuration = configurationBuilder.Build();
        var connectionString = configuration.GetConnectionString("main");
        Database.Program.Main("--p", "false", "--c", connectionString!);
    }

    public FlurlClient GetClient()
    {
        return new FlurlClient(_webApplication.GetTestClient());
    }

    private TestHelper(WebApplication webApplication, AccumulatingLogEventSink accumulatingLogEventSink,
        ExternalServices externalServices)
    {
        ExternalServices = externalServices;
        _webApplication = webApplication;
        _logSink = accumulatingLogEventSink;
    }


    public void Dispose()
    {
        ((IDisposable) _webApplication).Dispose();
        _logSink.Dispose();

        var logs = _logSink.Logs.ToArray();
        // foreach (var l in logs)
        // {
        //     var s = FormatLogEvent(l);
        //     Console.Write(s);
        // }


        if (!AllowLogsErrors)
        {
            // there is a strange error in docker, like this one:
            // https://stackoverflow.com/questions/55760907/net-core-warning-no-xml-encryptor-configured
            // but suggested solutions didn't work, so just ignore the warnings...
            var errors = logs
                .Where(x =>
                    x.Level is LogEventLevel.Error or LogEventLevel.Fatal)
                .Select(FormatLogEvent)
                .ToArray();
            if (errors.Any())
            {
                throw new Exception(string.Join(Environment.NewLine, errors));
            }
        }
    }

    private static string FormatLogEvent(LogEvent l)
    {
        return
            $"[{l.Timestamp:HH:mm:ss} {l.Level.ToString().Substring(0, 3).ToUpperInvariant()}] {l.RenderMessage()}{Environment.NewLine}{l.Exception}";
    }

    private class AccumulatingLogEventSink : ILogEventSink, IDisposable
    {
        public readonly List<LogEvent> Logs = new();
        private bool _disposed;

        public void Emit(LogEvent l)
        {
            lock (Logs)
            {
                Logs.Add(l);

                if (_disposed)
                {
                    throw new Exception("Log output written after app was disposed.");
                }
            }
        }

        public void Dispose()
        {
            lock (Logs)
            {
                _disposed = true;
            }
        }
    }
}