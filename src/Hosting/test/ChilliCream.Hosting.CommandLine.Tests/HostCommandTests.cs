using System.CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace ChilliCream.Hosting.CommandLine.Tests;

[Collection<ProcessExitCodeCollection>]
public sealed class HostCommandTests
{
    [Theory]
    [InlineData("jobs", "list")]
    [InlineData("status")]
    [InlineData("--config", "./test.yml", "jobs", "list")]
    [InlineData("--config=./test.yml", "status")]
    [InlineData("--help")]
    [InlineData("--OtherKey", "jobs", "--help")]
    public async Task RunWithCommandsAsync_Should_ExitBeforeStartup_When_CommandIsSelected(params string[] args)
    {
        // arrange
        var host = CreateHost(args, out var started);

        // act
        var exitCode = await host.RunWithCommandsAsync(args, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(0, exitCode);
        Assert.False(started.IsCancellationRequested);
    }

    [Theory]
    [InlineData("jobs", "list", "--config", "./test.yml")]
    [InlineData("--config")]
    [InlineData("jobs", "missing")]
    [InlineData("jbos", "list")]
    [InlineData("--OtherKey", "jobs", "list")]
    [InlineData("--OtherKey", "value", "jbos")]
    [InlineData("--OtherKey")]
    [InlineData("=value")]
    [InlineData("--config", "./test.yml", "--OtherKey")]
    [InlineData("--OtherKey", "jobs", "--count", "invalid")]
    [InlineData("--OtherKey", "--count")]
    [InlineData("--flag=unexpected")]
    [InlineData("--OtherKey", "value", "--flag", "status")]
    public async Task RunWithCommandsAsync_Should_ReturnError_When_InvocationIsInvalid(params string[] args)
    {
        // arrange
        var host = CreateHost(args, out var started);

        // act
        var exitCode = await host.RunWithCommandsAsync(args, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(1, exitCode);
        Assert.False(started.IsCancellationRequested);
    }

    [Theory]
    [InlineData("--config", "./test.yml")]
    [InlineData("--config=./test.yml")]
    [InlineData("--config", "./test.yml", "--OtherKey", "jobs")]
    public async Task RunWithCommandsAsync_Should_StartWithConfig_When_OnlyHostOptionsArePresent(params string[] args)
    {
        // arrange
        var host = CreateHost(args, out var started);
        var configPath = host.Services.GetRequiredService<IConfiguration>()["config"];

        // act
        var exitCode = await host.RunWithCommandsAsync(args, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(0, exitCode);
        Assert.True(started.IsCancellationRequested);
        Assert.Equal("./test.yml", configPath);
    }

    [Theory]
    [InlineData("--OtherKey", "jobs")]
    [InlineData("/OtherKey", "jobs")]
    [InlineData("OtherKey=jobs")]
    [InlineData("--OtherKey=jobs")]
    [InlineData("/OtherKey=jobs")]
    [InlineData("OtherKey=jobs", "--urls", "http://localhost:5000")]
    public async Task RunWithCommandsAsync_Should_StartHost_When_ConfigurationValueMatchesCommand(params string[] args)
    {
        // arrange
        var host = CreateHost(args, out var started);
        var value = host.Services.GetRequiredService<IConfiguration>()["OtherKey"];

        // act
        var exitCode = await host.RunWithCommandsAsync(args, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(0, exitCode);
        Assert.True(started.IsCancellationRequested);
        Assert.Equal("jobs", value);
    }

    [Theory]
    [InlineData(0, false, "jobs", "list")]
    [InlineData(0, false, "status")]
    [InlineData(0, false, "--config", "./test.yml", "jobs", "list")]
    [InlineData(1, false, "jobs", "list", "--config", "./test.yml")]
    [InlineData(0, true, "--config", "./test.yml")]
    [InlineData(1, false, "jbos", "list")]
    [InlineData(0, true, "--OtherKey", "jobs")]
    public void RunWithCommands_Should_ReturnExitCode_When_CommandOrHostFinishes(
        int expectedExitCode,
        bool expectStarted,
        params string[] args)
    {
        // arrange
        var host = CreateHost(args, out var started);

        // act
        var exitCode = host.RunWithCommands(args);

        // assert
        Assert.Equal(expectedExitCode, exitCode);
        Assert.Equal(expectStarted, started.IsCancellationRequested);
    }

    [Fact]
    public void RunWithCommands_Should_ExecuteOnCallingThread_When_HandlerIsSynchronous()
    {
        // arrange
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddHostCommandContributor<ThreadCommandContributor>();
        var host = builder.Build();
        var callingThread = Environment.CurrentManagedThreadId;

        // act
        var exitCode = host.RunWithCommands(["thread"]);

        // assert
        Assert.Equal(callingThread, exitCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RunWithCommands_Should_ReturnProcessExitCode_When_ApplicationSetsItDuringShutdown(bool runAsync)
    {
        // arrange
        var previousExitCode = Environment.ExitCode;
        var host = CreateHost([], out _);
        var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
        lifetime.ApplicationStopped.Register(() => Environment.ExitCode = 23);

        try
        {
            // act
            var exitCode = runAsync
                ? await host.RunWithCommandsAsync([], TestContext.Current.CancellationToken)
                : host.RunWithCommands([]);

            // assert
            Assert.Equal(23, exitCode);
        }
        finally
        {
            Environment.ExitCode = previousExitCode;
        }
    }

    private sealed class ThreadCommandContributor : IHostCommandContributor
    {
        public void Configure(RootCommand root)
        {
            var command = new Command("thread");
            command.SetAction(_ => Environment.CurrentManagedThreadId);
            root.Subcommands.Add(command);
        }
    }

    private sealed class JobCommandContributor : IHostCommandContributor
    {
        public void Configure(RootCommand root)
        {
            root.Options.Add(new Option<string>("--config"));
            root.Options.Add(new Option<int>("--count"));
            root.Options.Add(new Option<bool>("--flag") { Arity = ArgumentArity.Zero });
            var jobs = new Command("jobs");
            var list = new Command("list");
            list.SetAction(_ => { });
            jobs.Subcommands.Add(list);
            root.Subcommands.Add(jobs);
        }
    }

    private sealed class StatusCommandContributor : IHostCommandContributor
    {
        public void Configure(RootCommand root)
        {
            var status = new Command("status");
            status.SetAction(_ => { });
            root.Subcommands.Add(status);
        }
    }

    private static IHost CreateHost(string[] args, out CancellationToken started)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddHostCommandContributor<JobCommandContributor>();
        builder.Services.AddHostCommandContributor<StatusCommandContributor>();

        var host = builder.Build();
        var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
        started = lifetime.ApplicationStarted;
        started.Register(lifetime.StopApplication);
        return host;
    }
}

[CollectionDefinition(DisableParallelization = true)]
public sealed class ProcessExitCodeCollection;
