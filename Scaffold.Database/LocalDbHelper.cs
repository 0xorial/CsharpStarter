using Microsoft.Data.SqlClient;

namespace Scaffold.Database;

public class LocalDbHelper
{
    public static void EnsureLocalDbCreated(string connectionString)
    {
        if (LocalDbExist(connectionString))
        {
            return;
        }

        var builder = new SqlConnectionStringBuilder(connectionString);
        var initialCatalog = builder.InitialCatalog;
        builder.InitialCatalog = "master";
        using var connection = new SqlConnection(builder.ConnectionString);
        using var command = new SqlCommand($"CREATE DATABASE {initialCatalog}") { Connection = connection };
        connection.Open();
        command.ExecuteNonQuery();
    }

    private static bool LocalDbExist(string connectionString)
    {
        using var connection = new SqlConnection(connectionString);
        using var command = new SqlCommand("SELECT 1") { Connection = connection };
        try
        {
            connection.Open(SqlConnectionOverrides.OpenWithoutRetry);
            command.ExecuteScalar();
        }
        catch (SqlException e)
        {
            if (IsDoesNotExist(e))
            {
                return false;
            }

            throw;
        }
        finally
        {
            // Clear connection pool for the database connection since after the 'create database' call, a previously
            // invalid connection may now be valid.
            SqlConnection.ClearPool(connection);
        }

        return true;
    }

    /// <summary>
    /// Copied from EF Core:
    /// https://github.com/dotnet/efcore/blob/a778126bfcd74594ad3fee585c92029f6d002b17/src/EFCore.SqlServer/Storage/Internal/SqlServerDatabaseCreator.cs#L299
    /// </summary>
    private static bool IsDoesNotExist(SqlException exception)
        => exception.Number == 4060 || exception.Number == 1832 || exception.Number == 5120;
}