namespace HotChocolate.Types.Introspection;

public class BM25TokenizerTests
{
    [Fact]
    public void Tokenize_Should_ReturnEmpty_When_InputIsNull()
    {
        // act
        var tokens = BM25Tokenizer.Tokenize(null!);

        // assert
        Assert.Empty(tokens);
    }

    [Fact]
    public void Tokenize_Should_ReturnEmpty_When_InputIsEmpty()
    {
        // act
        var tokens = BM25Tokenizer.Tokenize(string.Empty);

        // assert
        Assert.Empty(tokens);
    }

    [Fact]
    public void Tokenize_Should_ReturnEmpty_When_InputIsWhitespace()
    {
        // act
        var tokens = BM25Tokenizer.Tokenize("   ");

        // assert
        Assert.Empty(tokens);
    }

    [Fact]
    public void Tokenize_Should_ReturnLowercaseToken_When_SimpleWord()
    {
        // act
        var tokens = BM25Tokenizer.Tokenize("Product");

        // assert
        Assert.Equal(["product"], tokens);
    }

    [Fact]
    public void Tokenize_Should_SplitCamelCase_When_CamelCaseInput()
    {
        // act
        var tokens = BM25Tokenizer.Tokenize("productName");

        // assert
        Assert.Equal(["product", "name"], tokens);
    }

    [Fact]
    public void Tokenize_Should_SplitPascalCase_When_PascalCaseInput()
    {
        // act
        var tokens = BM25Tokenizer.Tokenize("ProductName");

        // assert
        Assert.Equal(["product", "name"], tokens);
    }

    [Fact]
    public void Tokenize_Should_SplitOnNonAlphanumeric_When_SpaceSeparated()
    {
        // act
        var tokens = BM25Tokenizer.Tokenize("product name description");

        // assert
        Assert.Equal(["product", "name", "description"], tokens);
    }

    [Fact]
    public void Tokenize_Should_FilterSingleCharTokens()
    {
        // act
        var tokens = BM25Tokenizer.Tokenize("a product b");

        // assert
        Assert.Equal(["product"], tokens);
    }

    [Fact]
    public void Tokenize_Should_HandleMixedSeparators()
    {
        // act
        var tokens = BM25Tokenizer.Tokenize("get_product-info");

        // assert
        Assert.Equal(["get", "product", "info"], tokens);
    }

    [Fact]
    public void Tokenize_Should_SplitAcronymBoundary_When_XMLParser()
    {
        // act
        var tokens = BM25Tokenizer.Tokenize("XMLParser");

        // assert
        Assert.Equal(["xml", "parser"], tokens);
    }

    [Fact]
    public void Tokenize_Should_HandleAllUppercase()
    {
        // act
        var tokens = BM25Tokenizer.Tokenize("ID");

        // assert
        Assert.Equal(["id"], tokens);
    }

    [Fact]
    public void Tokenize_Should_HandleMultipleCamelCaseWords()
    {
        // act
        var tokens = BM25Tokenizer.Tokenize("getProductNameById");

        // assert
        Assert.Equal(["get", "product", "name", "by", "id"], tokens);
    }

    [Fact]
    public void Tokenize_Should_HandleDescriptionText()
    {
        // act
        var tokens = BM25Tokenizer.Tokenize("The product name field");

        // assert
        Assert.Equal(["the", "product", "name", "field"], tokens);
    }
}
