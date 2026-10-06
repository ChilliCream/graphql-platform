using CookieCrumble;

namespace Mocha.Transport.Postgres.Tests.Connection;

public class PostgresMigrationScriptTests
{
    [Fact]
    public void GenerateMigrationsSql_Should_ReturnDeterministicSql_When_UsingDefaults()
    {
        // arrange
        var options = new PostgresSchemaOptions();

        // act
        var script = PostgresTransportSchema.GenerateMigrationsSql(options);
        var repeatedScript = PostgresTransportSchema.GenerateMigrationsSql(options);

        // assert
        Assert.Equal(script, repeatedScript);
        Assert.Equal(script.ReplaceLineEndings("\n"), script);
        script.MatchSnapshot();
    }
}
