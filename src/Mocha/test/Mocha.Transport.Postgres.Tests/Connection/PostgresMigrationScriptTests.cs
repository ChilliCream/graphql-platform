using CookieCrumble;

namespace Mocha.Transport.Postgres.Tests.Connection;

public class PostgresMigrationScriptTests
{
    [Fact]
    public void GenerateMigrationScript_Should_ReturnDeterministicSql_When_UsingDefaults()
    {
        // arrange
        var options = new PostgresSchemaOptions();

        // act
        var script = PostgresTransportSchema.GenerateMigrationScript(options);

        // assert
        Assert.Equal(script, PostgresTransportSchema.GenerateMigrationScript(options));
        Assert.Equal(script.ReplaceLineEndings("\n"), script);
        script.MatchSnapshot();
    }

    [Theory]
    [InlineData("public; DROP SCHEMA public CASCADE")]
    [InlineData("\"unclosed")]
    [InlineData("\"invalid\"quote\"")]
    [InlineData("")]
    public void GenerateMigrationScript_Should_RejectInvalidIdentifier_When_SchemaIsMalformed(string schema)
    {
        // arrange
        var options = new PostgresSchemaOptions { Schema = schema };

        // act
        var error = Assert.Throws<ArgumentException>(
            () => PostgresTransportSchema.GenerateMigrationScript(options));

        // assert
        Assert.Equal("identifier", error.ParamName);
    }
}
