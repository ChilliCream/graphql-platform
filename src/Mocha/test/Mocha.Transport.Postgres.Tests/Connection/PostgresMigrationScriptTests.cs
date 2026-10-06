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

        // assert
        Assert.Equal(script, PostgresTransportSchema.GenerateMigrationsSql(options));
        Assert.Equal(script.ReplaceLineEndings("\n"), script);
        script.MatchSnapshot();
    }

    [Theory]
    [InlineData("public; DROP SCHEMA public CASCADE")]
    [InlineData("\"unclosed")]
    [InlineData("\"invalid\"quote\"")]
    [InlineData("")]
    public void GenerateMigrationsSql_Should_RejectInvalidIdentifier_When_SchemaIsMalformed(string schema)
    {
        // arrange
        var options = new PostgresSchemaOptions { Schema = schema };

        // act
        var error = Assert.Throws<ArgumentException>(
            () => PostgresTransportSchema.GenerateMigrationsSql(options));

        // assert
        Assert.Equal("identifier", error.ParamName);
    }
}
