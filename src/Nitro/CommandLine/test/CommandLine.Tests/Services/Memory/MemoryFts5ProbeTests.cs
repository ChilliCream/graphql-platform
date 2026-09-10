using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Tests.Memory;

/// <summary>
/// Verifies that the bundled SQLitePCLRaw <c>e_sqlite3</c> provider this
/// tool ships supports FTS5: creates a real virtual table and queries it
/// through the managed/native interop path the memory index will use.
/// This is a one-time infrastructure guarantee, not a test of memory
/// search behavior, which is out of scope for this slice.
/// </summary>
public sealed class MemoryFts5ProbeTests
{
    static MemoryFts5ProbeTests() => SQLitePCL.Batteries_V2.Init();

    [Fact]
    public async Task Fts5_Should_CreateAndQueryVirtualTable_When_UsingTheBundledProvider()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(cancellationToken);

        await ExecuteAsync(connection, "CREATE VIRTUAL TABLE curated_fts USING fts5(id UNINDEXED, body);");
        await ExecuteAsync(
            connection,
            "INSERT INTO curated_fts (id, body) VALUES ('mem-01', 'the quick brown fox jumps over the lazy dog');");
        await ExecuteAsync(
            connection,
            "INSERT INTO curated_fts (id, body) VALUES ('mem-02', 'an entirely unrelated entry about oceans');");

        // act
        var matches = new List<string>();
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT id FROM curated_fts WHERE curated_fts MATCH 'quick' ORDER BY id;";
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                matches.Add(reader.GetString(0));
            }
        }

        // assert
        Assert.Equal(["mem-01"], matches);
    }

    private static async Task ExecuteAsync(SqliteConnection connection, string commandText)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }
}
