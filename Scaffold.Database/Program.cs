using System.Text;
using FluentMigrator.Runner;
using FluentMigrator.Runner.Initialization;
using FluentMigrator.Runner.Logging;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Scaffold.Database.Migrations;

namespace Scaffold.Database
{
    public class Program
    {
        private const string IsInPreviewModeArgumentKey = "p";
        private const string OutputFilePathArgumentKey = "o";
        private const string ConnectionStringArgumentKey = "c";
        private const string DowngradeDatabaseArgumentKey = "d";
        private const string AppSettingsConnectionStringArgumentKey = "cc";
        private static bool isInPreviewMode = true;
        private static string outputFilePath = default!;
        private static string connectionString = default!;
        private static int? downgradeVersion = null;

        public static void Main(params string[] args)
        {
            var builder = new ConfigurationBuilder();
            builder.AddCommandLine(args);

            var configRoot = builder.Build();

            if (!string.IsNullOrWhiteSpace(configRoot[IsInPreviewModeArgumentKey]))
            {
                isInPreviewMode = bool.Parse(configRoot[IsInPreviewModeArgumentKey]);
            }

            Console.WriteLine($"Preview: '{isInPreviewMode}'");

            outputFilePath = System.IO.Path.GetTempFileName();

            if (!string.IsNullOrWhiteSpace(configRoot[OutputFilePathArgumentKey]))
            {
                outputFilePath = configRoot[OutputFilePathArgumentKey];
            }

            Console.WriteLine($"Output File Path: '{outputFilePath}'");

            if (!string.IsNullOrWhiteSpace(configRoot[DowngradeDatabaseArgumentKey]))
            {
                downgradeVersion = int.Parse(configRoot[DowngradeDatabaseArgumentKey]);
            }

            Console.WriteLine(downgradeVersion.HasValue
                ? $"Downgrade version: '{downgradeVersion.Value}'"
                : "Update database to latest version");

            if (!string.IsNullOrWhiteSpace(configRoot[ConnectionStringArgumentKey]))
            {
                connectionString = configRoot[ConnectionStringArgumentKey];
            }
            else
            {
                var config = new ConfigurationBuilder().AddJsonFile("appsettings.db.json")
                    .Build();
                if (!string.IsNullOrWhiteSpace(configRoot[AppSettingsConnectionStringArgumentKey]))
                {
                    var connectionStringName = configRoot[AppSettingsConnectionStringArgumentKey];
                    connectionString = config.GetConnectionString(connectionStringName);
                }
                else
                {
                    connectionString = config.GetConnectionString("ConnectionStringLocal");
                }
            }

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new Exception("Connection string missing!");
            }

            Console.WriteLine($"Connection String: '{connectionString}'");

            if (connectionString.Contains("(localdb)"))
            {
                LocalDbHelper.EnsureLocalDbCreated(connectionString);
            }

            var sb = new StringBuilder();
            var serviceProvider = CreateServices(sb);
            using var scope = serviceProvider.CreateScope();

            if (downgradeVersion.HasValue)
            {
                DownGradeDatabase(scope.ServiceProvider, downgradeVersion.Value);
            }
            else
            {
                UpdateDatabase(scope.ServiceProvider);
            }

            Console.Write(sb);
        }

        private static IServiceProvider CreateServices(StringBuilder sb)
        {
            return new ServiceCollection().AddFluentMigratorCore()
                .Configure<RunnerOptions>(cfg =>
                {
                    // cfg.NoConnection = true;
                    // cfg.StartVersion = -1;
                })
                .ConfigureRunner(rb =>
                {
                    rb.AddSqlServer()
                        .AsGlobalPreview(isInPreviewMode)
                        .ConfigureGlobalProcessorOptions(opt => { opt.PreviewOnly = isInPreviewMode; })
                        .WithGlobalConnectionString(connectionString)
                        .ScanIn(typeof(M001_InitialModel).Assembly)
                        .For.Migrations();
                })
                .AddLogging(builder => builder.AddFluentMigratorConsole())
                .AddSingleton<ILoggerProvider, LogFileFluentMigratorLoggerProvider>()
                .Configure<FluentMigratorLoggerOptions>(opt => { opt.ShowSql = true; })
                .Configure<LogFileFluentMigratorLoggerOptions>(opt =>
                {
                    opt.OutputGoBetweenStatements = true;
                    opt.ShowSql = true;
                    opt.OutputFileName = outputFilePath;
                })
                .BuildServiceProvider();
        }

        private static void UpdateDatabase(IServiceProvider serviceProvider)
        {
            var runner = serviceProvider.GetRequiredService<IMigrationRunner>();
            runner.MigrateUp();
        }

        //just for test purpose, we need to see how to implement it in real condition (CI/CD,...)
        private static void DownGradeDatabase(IServiceProvider serviceProvider, int version)
        {
            var runner = serviceProvider.GetRequiredService<IMigrationRunner>();
            runner.MigrateDown(version);
        }
    }
}