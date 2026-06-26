using System.Text.Json;
using Ams2CareerCompanion.Core.Interfaces;
using Ams2CareerCompanion.Core.Models;
using Microsoft.Data.Sqlite;

namespace Ams2CareerCompanion.Infrastructure.Persistence;

public sealed class SqliteCareerRepository : ICareerRepository
{
    private const string CurrentCareerKey = "current_career_id";
    private readonly string _dbPath;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public SqliteCareerRepository(string dataDirectory)
    {
        Directory.CreateDirectory(dataDirectory);
        _dbPath = Path.Combine(dataDirectory, "career.db");
        InitializeDatabase();
    }

    public async Task<CareerState?> LoadCurrentCareerAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var careerId = await ReadAppStateAsync(connection, CurrentCareerKey, cancellationToken);
        if (string.IsNullOrWhiteSpace(careerId))
        {
            return null;
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT data_json FROM careers WHERE id = $id";
        command.Parameters.AddWithValue("$id", careerId);

        var payload = await command.ExecuteScalarAsync(cancellationToken) as string;
        return payload is null ? null : JsonSerializer.Deserialize<CareerState>(payload, _jsonOptions);
    }

    public async Task<IReadOnlyList<CareerSummary>> ListCareersAsync(CancellationToken cancellationToken = default)
    {
        var items = new List<CareerSummary>();

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        var currentCareerId = await ReadAppStateAsync(connection, CurrentCareerKey, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT data_json
            FROM careers
            ORDER BY created_utc DESC;
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var payload = reader.GetString(0);
            var career = JsonSerializer.Deserialize<CareerState>(payload, _jsonOptions);
            if (career is null)
            {
                continue;
            }

            items.Add(new CareerSummary
            {
                Id = career.Id,
                Name = career.Name,
                CreatedUtc = career.CreatedUtc,
                Level = career.Progression.Level,
                ActiveLeagueId = career.ActiveLeagueId,
                IsCurrent = string.Equals(currentCareerId, career.Id.ToString("D"), StringComparison.OrdinalIgnoreCase)
            });
        }

        return items;
    }

    public async Task SaveCareerAsync(CareerState career, bool setAsCurrent, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(career, _jsonOptions);

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using (var command = connection.CreateCommand())
        {
            command.CommandText =
                """
                INSERT INTO careers (id, name, created_utc, data_json)
                VALUES ($id, $name, $createdUtc, $data)
                ON CONFLICT(id) DO UPDATE SET
                    name = excluded.name,
                    created_utc = excluded.created_utc,
                    data_json = excluded.data_json;
                """;
            command.Parameters.AddWithValue("$id", career.Id.ToString("D"));
            command.Parameters.AddWithValue("$name", career.Name);
            command.Parameters.AddWithValue("$createdUtc", career.CreatedUtc.ToString("O"));
            command.Parameters.AddWithValue("$data", payload);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        if (setAsCurrent)
        {
            await WriteAppStateAsync(connection, CurrentCareerKey, career.Id.ToString("D"), cancellationToken);
        }
    }

    public async Task SetCurrentCareerAsync(Guid careerId, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await WriteAppStateAsync(connection, CurrentCareerKey, careerId.ToString("D"), cancellationToken);
    }

    public async Task DeleteCareerAsync(Guid careerId, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var currentCareerId = await ReadAppStateAsync(connection, CurrentCareerKey, cancellationToken);

        await using (var deleteResults = connection.CreateCommand())
        {
            deleteResults.CommandText = "DELETE FROM race_results WHERE career_id = $careerId";
            deleteResults.Parameters.AddWithValue("$careerId", careerId.ToString("D"));
            await deleteResults.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var deleteCareer = connection.CreateCommand())
        {
            deleteCareer.CommandText = "DELETE FROM careers WHERE id = $careerId";
            deleteCareer.Parameters.AddWithValue("$careerId", careerId.ToString("D"));
            await deleteCareer.ExecuteNonQueryAsync(cancellationToken);
        }

        if (!string.Equals(currentCareerId, careerId.ToString("D"), StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await using var nextCareerCommand = connection.CreateCommand();
        nextCareerCommand.CommandText =
            """
            SELECT id
            FROM careers
            ORDER BY created_utc DESC
            LIMIT 1;
            """;

        var nextCareerId = await nextCareerCommand.ExecuteScalarAsync(cancellationToken) as string;
        if (string.IsNullOrWhiteSpace(nextCareerId))
        {
            await WriteAppStateAsync(connection, CurrentCareerKey, string.Empty, cancellationToken);
            return;
        }

        await WriteAppStateAsync(connection, CurrentCareerKey, nextCareerId, cancellationToken);
    }

    public async Task<bool> HasLoggedRaceAsync(Guid careerId, Guid draftId, Guid? automationRunId, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT EXISTS(
                SELECT 1
                FROM race_results
                WHERE career_id = $careerId
                  AND (
                    draft_id = $draftId
                    OR ($automationRunId IS NOT NULL AND automation_run_id = $automationRunId)
                  )
            );
            """;
        command.Parameters.AddWithValue("$careerId", careerId.ToString("D"));
        command.Parameters.AddWithValue("$draftId", draftId.ToString("D"));
        command.Parameters.AddWithValue("$automationRunId", automationRunId?.ToString("D") ?? (object)DBNull.Value);

        var exists = await command.ExecuteScalarAsync(cancellationToken);
        return exists is 1L or 1;
    }

    public async Task AppendRaceResultAsync(Guid careerId, RaceResultConfirmed result, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(result, _jsonOptions);

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT OR IGNORE INTO race_results (id, draft_id, automation_run_id, career_id, completed_utc, data_json)
            VALUES ($id, $draftId, $automationRunId, $careerId, $completedUtc, $data);
            """;
        command.Parameters.AddWithValue("$id", result.Id.ToString("D"));
        command.Parameters.AddWithValue("$draftId", result.Draft.Id.ToString("D"));
        command.Parameters.AddWithValue("$automationRunId", result.Draft.AutomationRunId?.ToString("D") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$careerId", careerId.ToString("D"));
        command.Parameters.AddWithValue("$completedUtc", result.Draft.CompletedUtc.ToString("O"));
        command.Parameters.AddWithValue("$data", payload);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RaceResultConfirmed>> LoadRecentResultsAsync(Guid careerId, int count, CancellationToken cancellationToken = default)
    {
        var items = new List<RaceResultConfirmed>();

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT data_json
            FROM race_results
            WHERE career_id = $careerId
            ORDER BY completed_utc DESC
            LIMIT $count;
            """;
        command.Parameters.AddWithValue("$careerId", careerId.ToString("D"));
        command.Parameters.AddWithValue("$count", count);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var payload = reader.GetString(0);
            var item = JsonSerializer.Deserialize<RaceResultConfirmed>(payload, _jsonOptions);
            if (item is not null)
            {
                items.Add(item);
            }
        }

        return items;
    }

    public async Task<IReadOnlyList<RaceResultConfirmed>> LoadRaceHistoryAsync(Guid careerId, CancellationToken cancellationToken = default)
    {
        var items = new List<RaceResultConfirmed>();

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT data_json
            FROM race_results
            WHERE career_id = $careerId
            ORDER BY completed_utc DESC;
            """;
        command.Parameters.AddWithValue("$careerId", careerId.ToString("D"));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var payload = reader.GetString(0);
            var item = JsonSerializer.Deserialize<RaceResultConfirmed>(payload, _jsonOptions);
            if (item is not null)
            {
                items.Add(item);
            }
        }

        return items;
    }

    private void InitializeDatabase()
    {
        using var connection = CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS careers (
                id TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                created_utc TEXT NOT NULL,
                data_json TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS race_results (
                id TEXT PRIMARY KEY,
                draft_id TEXT,
                automation_run_id TEXT,
                career_id TEXT NOT NULL,
                completed_utc TEXT NOT NULL,
                data_json TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS app_state (
                key TEXT PRIMARY KEY,
                value_json TEXT NOT NULL
            );
            """;
        command.ExecuteNonQuery();

        EnsureColumnExists(connection, "race_results", "draft_id", "TEXT");
        EnsureColumnExists(connection, "race_results", "automation_run_id", "TEXT");

        using var indexCommand = connection.CreateCommand();
        indexCommand.CommandText =
            """
            CREATE UNIQUE INDEX IF NOT EXISTS idx_race_results_career_draft
            ON race_results(career_id, draft_id)
            WHERE draft_id IS NOT NULL;

            CREATE UNIQUE INDEX IF NOT EXISTS idx_race_results_career_run
            ON race_results(career_id, automation_run_id)
            WHERE automation_run_id IS NOT NULL;
            """;
        indexCommand.ExecuteNonQuery();
    }

    private SqliteConnection CreateConnection()
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = _dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        };

        return new SqliteConnection(builder.ConnectionString);
    }

    private static async Task<string?> ReadAppStateAsync(SqliteConnection connection, string key, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT value_json FROM app_state WHERE key = $key";
        command.Parameters.AddWithValue("$key", key);
        return await command.ExecuteScalarAsync(cancellationToken) as string;
    }

    private static async Task WriteAppStateAsync(SqliteConnection connection, string key, string value, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO app_state (key, value_json)
            VALUES ($key, $value)
            ON CONFLICT(key) DO UPDATE SET
                value_json = excluded.value_json;
            """;
        command.Parameters.AddWithValue("$key", key);
        command.Parameters.AddWithValue("$value", value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void EnsureColumnExists(SqliteConnection connection, string tableName, string columnName, string columnDefinition)
    {
        using var pragmaCommand = connection.CreateCommand();
        pragmaCommand.CommandText = $"PRAGMA table_info({tableName});";

        using var reader = pragmaCommand.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        using var alterCommand = connection.CreateCommand();
        alterCommand.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition};";
        alterCommand.ExecuteNonQuery();
    }
}
