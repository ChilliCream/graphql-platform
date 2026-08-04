namespace Mocha.Transport.Postgres.Tests;

public class PostgresTransportConfigurationTests
{
    [Fact]
    public void DefaultName_Should_BePostgres()
    {
        Assert.Equal("postgres", PostgresTransportConfiguration.DefaultName);
    }

    [Fact]
    public void DefaultSchema_Should_BePostgres()
    {
        Assert.Equal("postgres", PostgresTransportConfiguration.DefaultSchema);
    }

    [Fact]
    public void Constructor_Should_SetDefaults_When_Created()
    {
        // act
        var config = new PostgresTransportConfiguration();

        // assert
        Assert.Equal("postgres", config.Name);
        Assert.Equal("postgres", config.Schema);
        Assert.Equal("localhost", config.Host);
        Assert.Equal(5432, config.Port);
        Assert.Null(config.ConnectionString);
        Assert.Empty(config.Topics);
        Assert.Empty(config.Queues);
        Assert.Empty(config.Subscriptions);
    }

    [Fact]
    public void ConnectionString_Should_BeSettable()
    {
        // arrange
        var config = new PostgresTransportConfiguration();

        // act
        config.ConnectionString = "Host=myhost;Database=mydb";

        // assert
        Assert.Equal("Host=myhost;Database=mydb", config.ConnectionString);
    }

    [Fact]
    public void Host_Should_BeSettable()
    {
        // arrange
        var config = new PostgresTransportConfiguration();

        // act
        config.Host = "myhost.example.com";

        // assert
        Assert.Equal("myhost.example.com", config.Host);
    }

    [Fact]
    public void Port_Should_BeSettable()
    {
        // arrange
        var config = new PostgresTransportConfiguration();

        // act
        config.Port = 5433;

        // assert
        Assert.Equal(5433, config.Port);
    }
}
