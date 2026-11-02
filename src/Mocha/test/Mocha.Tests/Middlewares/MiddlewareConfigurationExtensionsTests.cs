namespace Mocha.Tests;

public class MiddlewareConfigurationExtensionsTests
{
    [Fact]
    public void DispatchAppend_Should_AddMiddleware_When_AfterKeyIsNull()
    {
        // arrange
        var configurations = new List<Action<List<DispatchMiddlewareConfiguration>>>();
        var middleware = new DispatchMiddlewareConfiguration((_, next) => next, "test-key");

        // act
        configurations.Append(middleware, null);

        // assert
        var pipeline = new List<DispatchMiddlewareConfiguration>();
        configurations[0](pipeline);
        Assert.Single(pipeline);
        Assert.Equal("test-key", pipeline[0].Key);
    }

    [Fact]
    public void DispatchAppend_Should_InsertAfterKey_When_KeyExists()
    {
        // arrange
        var configurations = new List<Action<List<DispatchMiddlewareConfiguration>>>();
        var first = new DispatchMiddlewareConfiguration((_, next) => next, "first");
        var second = new DispatchMiddlewareConfiguration((_, next) => next, "second");
        var toInsert = new DispatchMiddlewareConfiguration((_, next) => next, "inserted");

        var pipeline = new List<DispatchMiddlewareConfiguration> { first, second };

        // act
        configurations.Append(toInsert, "first");
        configurations[0](pipeline);

        // assert
        Assert.Equal(3, pipeline.Count);
        Assert.Equal("first", pipeline[0].Key);
        Assert.Equal("inserted", pipeline[1].Key);
        Assert.Equal("second", pipeline[2].Key);
    }

    [Fact]
    public void DispatchAppend_Should_ThrowException_When_KeyNotFound()
    {
        // arrange
        var configurations = new List<Action<List<DispatchMiddlewareConfiguration>>>();
        var middleware = new DispatchMiddlewareConfiguration((_, next) => next, "test");
        configurations.Append(middleware, "nonexistent");

        var pipeline = new List<DispatchMiddlewareConfiguration>
        {
            new DispatchMiddlewareConfiguration((_, next) => next, "existing")
        };

        // act & assert
        var ex = Assert.Throws<InvalidOperationException>(() => configurations[0](pipeline));
        Assert.Contains("nonexistent", ex.Message);
    }

    [Fact]
    public void DispatchPrepend_Should_InsertAtBeginning_When_BeforeKeyIsNull()
    {
        // arrange
        var configurations = new List<Action<List<DispatchMiddlewareConfiguration>>>();
        var middleware = new DispatchMiddlewareConfiguration((_, next) => next, "prepended");
        configurations.Prepend(middleware, null);

        var pipeline = new List<DispatchMiddlewareConfiguration>
        {
            new DispatchMiddlewareConfiguration((_, next) => next, "existing")
        };

        // act
        configurations[0](pipeline);

        // assert
        Assert.Equal(2, pipeline.Count);
        Assert.Equal("prepended", pipeline[0].Key);
        Assert.Equal("existing", pipeline[1].Key);
    }

    [Fact]
    public void DispatchPrepend_Should_InsertBeforeKey_When_KeyExists()
    {
        // arrange
        var configurations = new List<Action<List<DispatchMiddlewareConfiguration>>>();
        var first = new DispatchMiddlewareConfiguration((_, next) => next, "first");
        var second = new DispatchMiddlewareConfiguration((_, next) => next, "second");
        var toInsert = new DispatchMiddlewareConfiguration((_, next) => next, "inserted");

        var pipeline = new List<DispatchMiddlewareConfiguration> { first, second };

        // act
        configurations.Prepend(toInsert, "second");
        configurations[0](pipeline);

        // assert
        Assert.Equal(3, pipeline.Count);
        Assert.Equal("first", pipeline[0].Key);
        Assert.Equal("inserted", pipeline[1].Key);
        Assert.Equal("second", pipeline[2].Key);
    }

    [Fact]
    public void DispatchPrepend_Should_ThrowException_When_KeyNotFound()
    {
        // arrange
        var configurations = new List<Action<List<DispatchMiddlewareConfiguration>>>();
        var middleware = new DispatchMiddlewareConfiguration((_, next) => next, "test");
        configurations.Prepend(middleware, "nonexistent");

        var pipeline = new List<DispatchMiddlewareConfiguration>
        {
            new DispatchMiddlewareConfiguration((_, next) => next, "existing")
        };

        // act & assert
        var ex = Assert.Throws<InvalidOperationException>(() => configurations[0](pipeline));
        Assert.Contains("nonexistent", ex.Message);
    }

    [Fact]
    public void DispatchCombine_Should_MergeConfigurations_When_NoModifiers()
    {
        // arrange
        var base1 = new DispatchMiddlewareConfiguration((_, next) => next, "base1");
        var base2 = new DispatchMiddlewareConfiguration((_, next) => next, "base2");
        var baseArray = System.Collections.Immutable.ImmutableArray.Create(base1, base2);

        var config1 = new DispatchMiddlewareConfiguration((_, next) => next, "config1");
        var configs = new List<DispatchMiddlewareConfiguration> { config1 };

        // act
        var result = baseArray.Combine(configs, Array.Empty<Action<List<DispatchMiddlewareConfiguration>>>());

        // assert
        Assert.Equal(3, result.Length);
        Assert.Equal("config1", result[0].Key);
        Assert.Equal("base1", result[1].Key);
        Assert.Equal("base2", result[2].Key);
    }

    [Fact]
    public void DispatchCombine_Should_ReturnBaseArray_When_NoConfigsOrModifiers()
    {
        // arrange
        var base1 = new DispatchMiddlewareConfiguration((_, next) => next, "base1");
        var baseArray = System.Collections.Immutable.ImmutableArray.Create(base1);

        // act
        var result = baseArray.Combine(
            Array.Empty<DispatchMiddlewareConfiguration>(),
            Array.Empty<Action<List<DispatchMiddlewareConfiguration>>>());

        // assert
        Assert.Single(result);
        Assert.Equal("base1", result[0].Key);
    }

    [Fact]
    public void ReceiveAppend_Should_InsertAfterKey_When_KeyExists()
    {
        // arrange
        var configurations = new List<Action<List<ReceiveMiddlewareConfiguration>>>();
        var first = new ReceiveMiddlewareConfiguration((_, next) => next, "first");
        var second = new ReceiveMiddlewareConfiguration((_, next) => next, "second");
        var toInsert = new ReceiveMiddlewareConfiguration((_, next) => next, "inserted");

        var pipeline = new List<ReceiveMiddlewareConfiguration> { first, second };

        // act - using the extension method on the list
        configurations.Append(toInsert, "first");
        configurations[0](pipeline);

        // assert
        Assert.Equal(3, pipeline.Count);
        Assert.Equal("first", pipeline[0].Key);
        Assert.Equal("inserted", pipeline[1].Key);
        Assert.Equal("second", pipeline[2].Key);
    }

    [Fact]
    public void ReceiveAppend_Should_ThrowException_When_KeyNotFound()
    {
        // arrange
        var configurations = new List<Action<List<ReceiveMiddlewareConfiguration>>>();
        var middleware = new ReceiveMiddlewareConfiguration((_, next) => next, "test");
        configurations.Append(middleware, "nonexistent");

        var pipeline = new List<ReceiveMiddlewareConfiguration>
        {
            new ReceiveMiddlewareConfiguration((_, next) => next, "existing")
        };

        // act & assert
        var ex = Assert.Throws<InvalidOperationException>(() => configurations[0](pipeline));
        Assert.Contains("nonexistent", ex.Message);
    }

    [Fact]
    public void ReceivePrepend_Should_InsertBeforeKey_When_KeyExists()
    {
        // arrange
        var configurations = new List<Action<List<ReceiveMiddlewareConfiguration>>>();
        var first = new ReceiveMiddlewareConfiguration((_, next) => next, "first");
        var second = new ReceiveMiddlewareConfiguration((_, next) => next, "second");
        var toInsert = new ReceiveMiddlewareConfiguration((_, next) => next, "inserted");

        var pipeline = new List<ReceiveMiddlewareConfiguration> { first, second };

        // act
        configurations.Prepend(toInsert, "second");
        configurations[0](pipeline);

        // assert
        Assert.Equal(3, pipeline.Count);
        Assert.Equal("first", pipeline[0].Key);
        Assert.Equal("inserted", pipeline[1].Key);
        Assert.Equal("second", pipeline[2].Key);
    }

    [Fact]
    public void ReceiveCombine_Should_MergeConfigurationsAndModifiers()
    {
        // arrange
        var base1 = new ReceiveMiddlewareConfiguration((_, next) => next, "base1");
        var baseArray = System.Collections.Immutable.ImmutableArray.Create(base1);

        var config1 = new ReceiveMiddlewareConfiguration((_, next) => next, "config1");
        var configs = new List<ReceiveMiddlewareConfiguration> { config1 };

        var modifiers = new List<Action<List<ReceiveMiddlewareConfiguration>>>
        {
            pipeline => pipeline.Add(new ReceiveMiddlewareConfiguration((_, next) => next, "modified"))
        };

        // act
        var result = baseArray.Combine(configs, modifiers);

        // assert
        Assert.Equal(3, result.Length);
        Assert.Equal("config1", result[0].Key);
        Assert.Equal("base1", result[1].Key);
        Assert.Equal("modified", result[2].Key);
    }

    [Fact]
    public void ConsumerAppend_Should_InsertAfterKey_When_KeyExists()
    {
        // arrange
        var configurations = new List<Action<List<ConsumerMiddlewareConfiguration>>>();
        var first = new ConsumerMiddlewareConfiguration((_, next) => next, "first");
        var second = new ConsumerMiddlewareConfiguration((_, next) => next, "second");
        var toInsert = new ConsumerMiddlewareConfiguration((_, next) => next, "inserted");

        var pipeline = new List<ConsumerMiddlewareConfiguration> { first, second };

        // act
        configurations.Append(toInsert, "first");
        configurations[0](pipeline);

        // assert
        Assert.Equal(3, pipeline.Count);
        Assert.Equal("first", pipeline[0].Key);
        Assert.Equal("inserted", pipeline[1].Key);
        Assert.Equal("second", pipeline[2].Key);
    }

    [Fact]
    public void ConsumerPrepend_Should_InsertBeforeKey_When_KeyExists()
    {
        // arrange
        var configurations = new List<Action<List<ConsumerMiddlewareConfiguration>>>();
        var first = new ConsumerMiddlewareConfiguration((_, next) => next, "first");
        var second = new ConsumerMiddlewareConfiguration((_, next) => next, "second");
        var toInsert = new ConsumerMiddlewareConfiguration((_, next) => next, "inserted");

        var pipeline = new List<ConsumerMiddlewareConfiguration> { first, second };

        // act
        configurations.Prepend(toInsert, "second");
        configurations[0](pipeline);

        // assert
        Assert.Equal(3, pipeline.Count);
        Assert.Equal("first", pipeline[0].Key);
        Assert.Equal("inserted", pipeline[1].Key);
        Assert.Equal("second", pipeline[2].Key);
    }

    [Fact]
    public void ConsumerCombine_Should_MergeConfigurationsAndModifiers()
    {
        // arrange
        var base1 = new ConsumerMiddlewareConfiguration((_, next) => next, "base1");
        var baseArray = System.Collections.Immutable.ImmutableArray.Create(base1);

        var config1 = new ConsumerMiddlewareConfiguration((_, next) => next, "config1");
        var configs = new List<ConsumerMiddlewareConfiguration> { config1 };

        var modifiers = new List<Action<List<ConsumerMiddlewareConfiguration>>>
        {
            pipeline => pipeline.Add(new ConsumerMiddlewareConfiguration((_, next) => next, "modified"))
        };

        // act
        var result = baseArray.Combine(configs, modifiers);

        // assert
        Assert.Equal(3, result.Length);
        Assert.Equal("config1", result[0].Key);
        Assert.Equal("base1", result[1].Key);
        Assert.Equal("modified", result[2].Key);
    }

    [Fact]
    public void DispatchAppendAndPrepend_Should_PreserveOrder_When_MultipleOperations()
    {
        // arrange
        var configurations = new List<Action<List<DispatchMiddlewareConfiguration>>>();
        var first = new DispatchMiddlewareConfiguration((_, next) => next, "first");
        var last = new DispatchMiddlewareConfiguration((_, next) => next, "last");
        var pipeline = new List<DispatchMiddlewareConfiguration> { first, last };

        // act - append after first, then prepend before last
        configurations.Append(new DispatchMiddlewareConfiguration((_, next) => next, "after-first"), "first");
        configurations.Prepend(new DispatchMiddlewareConfiguration((_, next) => next, "before-last"), "last");

        foreach (var config in configurations)
        {
            config(pipeline);
        }

        // assert
        Assert.Equal(4, pipeline.Count);
        Assert.Equal("first", pipeline[0].Key);
        Assert.Equal("after-first", pipeline[1].Key);
        Assert.Equal("before-last", pipeline[2].Key);
        Assert.Equal("last", pipeline[3].Key);
    }
}
