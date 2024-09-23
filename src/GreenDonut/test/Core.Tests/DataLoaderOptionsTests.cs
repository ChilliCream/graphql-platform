using Xunit;

namespace GreenDonut;

public class DataLoaderOptionsTests
{
    [Fact(DisplayName = "Constructor: Should set all properties 1")]
    public void ConstructorAllProps1()
    {
        // act
        var options = new DataLoaderOptions
        {
            Cache = new PromiseCache(1),
            MaxBatchSize = 1,
            DiagnosticEvents = new DataLoaderDiagnosticEventListener(),
        };

        // assert
        Assert.NotNull(options.Cache);
        Assert.Equal(1, options.MaxBatchSize);
        Assert.NotNull(options.DiagnosticEvents);
    }

    [Fact(DisplayName = "Constructor: Should set all properties 2")]
    public void ConstructorAllProps2()
    {
        // act
        var options = new DataLoaderOptions
        {
            Cache = null,
            MaxBatchSize = 10,
            DiagnosticEvents = null,
        };

        // assert
        Assert.Null(options.Cache);
        Assert.Equal(10, options.MaxBatchSize);
        Assert.Null(options.DiagnosticEvents);
    }

    [Fact(DisplayName = "Constructor: Should result in defaults")]
    public void ConstructorEmpty()
    {
        // act
        var options = new DataLoaderOptions();

        // assert
        Assert.Null(options.Cache);
        Assert.Equal(1024, options.MaxBatchSize);
        Assert.Null(options.DiagnosticEvents);
    }

    [Fact(DisplayName = "Copy: Should copy all property values")]
    public void Copy()
    {
        // arrange
        var options = new DataLoaderOptions
        {
            Cache = new PromiseCache(1),
            MaxBatchSize = 1,
            DiagnosticEvents = new DataLoaderDiagnosticEventListener(),
        };

        // act
        var copy = options.Copy();

        // assert
        Assert.NotNull(copy.Cache);
        Assert.Equal(1, copy.MaxBatchSize);
        Assert.NotNull(copy.DiagnosticEvents);
    }
}
