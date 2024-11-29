using System.Collections.Concurrent;
using Moq;

namespace StrawberryShake;

public class OperationRequestTests
{
    [Fact]
    public void Equals_With_Variables_1()
    {
        // arrange
        var document = new Mock<IDocument>();

        var a = new OperationRequest(
            null,
            "abc",
            document.Object,
            new Dictionary<string, object?> { { "a", "a" }, });

        var b = new OperationRequest(
            null,
            "abc",
            document.Object,
            new Dictionary<string, object?> { { "a", "a" }, });

        // act
        // assert
        Assert.True(a.Equals(b));
    }

    [Fact]
    public void Equals_With_Variables_2()
    {
        // arrange
        var document = new Mock<IDocument>();

        var a = new OperationRequest(
            null,
            "abc",
            document.Object,
            new Dictionary<string, object?> { { "a", "a" }, });

        var b = new OperationRequest(
            null,
            "abc",
            document.Object,
            new ConcurrentDictionary<string, object?>(
                new Dictionary<string, object?> { { "a", "a" }, }));

        // act
        // assert
        Assert.True(a.Equals(b));
    }

    [Fact]
    public void Equals_With_Variables_3()
    {
        // arrange
        var document = new Mock<IDocument>();

        var a = new OperationRequest(
            null,
            "abc",
            document.Object,
            new Dictionary<string, object?>
            {
                {
                    "a",
                    new ConcurrentDictionary<string, object?>(
                        new Dictionary<string, object?> { { "a", "b" }, })
                },
            });

        var b = new OperationRequest(
            null,
            "abc",
            document.Object,
            new Dictionary<string, object?>
            {
                {
                    "a",
                    new ConcurrentDictionary<string, object?>(
                        new Dictionary<string, object?> { { "a", "b" }, })
                },
            });

        // act
        // assert
        Assert.True(a.Equals(b));
    }

    [Fact]
    public void Equals_With_Variables_4()
    {
        // arrange
        var document = new Mock<IDocument>();
        var dict = new Dictionary<string, object?> { { "a", "a" }, };

        var a = new OperationRequest(
            null,
            "abc",
            document.Object,
            dict);

        var b = new OperationRequest(
            null,
            "abc",
            document.Object,
            dict);

        // act
        // assert
        Assert.True(a.Equals(b));
    }

    [Fact]
    public void Equals_With_Variables_5()
    {
        // arrange
        var document = new Mock<IDocument>();
        var dict = new Dictionary<string, object?> { { "a", "a" }, };

        var a = new OperationRequest(
            null,
            "abc",
            document.Object,
            new Dictionary<string, object?> { { "a", dict }, });

        var b = new OperationRequest(
            null,
            "abc",
            document.Object,
            new Dictionary<string, object?> { { "a", dict }, });

        // act
        // assert
        Assert.True(a.Equals(b));
    }

    [Fact]
    public void NotEquals_With_Variables_1()
    {
        // arrange
        var document = new Mock<IDocument>();

        var a = new OperationRequest(
            null,
            "abc",
            document.Object,
            new Dictionary<string, object?> { { "a", "b" }, });

        var b = new OperationRequest(
            null,
            "abc",
            document.Object,
            new Dictionary<string, object?> { { "a", "a" }, });

        // act
        // assert
        Assert.False(a.Equals(b));
    }

    [Fact]
    public void NotEquals_With_Variables_3()
    {
        // arrange
        var document = new Mock<IDocument>();

        var a = new OperationRequest(
            null,
            "abc",
            document.Object,
            new Dictionary<string, object?> { { "a", "a" }, });

        var b = new OperationRequest(
            null,
            "abc",
            document.Object,
            new Dictionary<string, object?> { { "a", "a" }, { "b", "a" }, });

        // act
        // assert
        Assert.False(a.Equals(b));
    }

    [Fact]
    public void NotEquals_With_Variables_4()
    {
        // arrange
        var document = new Mock<IDocument>();

        var a = new OperationRequest(
            null,
            "abc",
            document.Object,
            new Dictionary<string, object?> { { "a", "a" }, });

        var b = new OperationRequest(
            null,
            "abc",
            document.Object,
            new ConcurrentDictionary<string, object?>(
                new Dictionary<string, object?> { { "a", "b" }, }));

        // act
        // assert
        Assert.False(a.Equals(b));
    }

    [Fact]
    public void NotEquals_With_Variables_5()
    {
        // arrange
        var document = new Mock<IDocument>();

        var a = new OperationRequest(
            null,
            "abc",
            document.Object,
            new Dictionary<string, object?> { { "a", "a" }, { "b", "a" }, });

        var b = new OperationRequest(
            null,
            "abc",
            document.Object,
            new ConcurrentDictionary<string, object?>(
                new Dictionary<string, object?> { { "a", "b" }, }));

        // act
        // assert
        Assert.False(a.Equals(b));
    }

    [Fact]
    public void NotEquals_With_Variables_6()
    {
        // arrange
        var document = new Mock<IDocument>();

        var a = new OperationRequest(
            null,
            "abc",
            document.Object,
            new Dictionary<string, object?> { { "b", "a" }, });

        var b = new OperationRequest(
            null,
            "abc",
            document.Object,
            new Dictionary<string, object?> { { "a", "a" }, { "b", "a" }, });

        // act
        // assert
        Assert.False(a.Equals(b));
    }

    [Fact]
    public void NotEquals_With_Variables_7()
    {
        // arrange
        var document = new Mock<IDocument>();

        var a = new OperationRequest(
            null,
            "abc",
            document.Object,
            new Dictionary<string, object?>
            {
                {
                    "a",
                    new ConcurrentDictionary<string, object?>(
                        new Dictionary<string, object?> { { "a", "b" }, })
                },
            });

        var b = new OperationRequest(
            null,
            "abc",
            document.Object,
            new Dictionary<string, object?>
            {
                {
                    "a",
                    new ConcurrentDictionary<string, object?>(
                        new Dictionary<string, object?> { { "c", "b" }, })
                },
            });

        // act
        // assert
        Assert.False(a.Equals(b));
    }

    [Fact]
    public void NotEquals_With_Variables_8()
    {
        // arrange
        var document = new Mock<IDocument>();

        var a = new OperationRequest(
            null,
            "abc",
            document.Object,
            new Dictionary<string, object?>
            {
                {
                    "a",
                    new ConcurrentDictionary<string, object?>(
                        new Dictionary<string, object?> { { "a", "b" }, })
                },
            });

        var b = new OperationRequest(
            null,
            "abc",
            document.Object,
            new Dictionary<string, object?>
            {
                {
                    "a",
                    new ConcurrentDictionary<string, object?>(
                        new Dictionary<string, object?> { { "a", "c" }, })
                },
            });

        // act
        // assert
        Assert.False(a.Equals(b));
    }

    [Fact]
    public void NotEquals_With_Variables_9()
    {
        // arrange
        var document = new Mock<IDocument>();

        var a = new OperationRequest(
            null,
            "abc",
            document.Object,
            new Dictionary<string, object?> { { "a", "b" }, });

        var b = new OperationRequest(
            null,
            "abc",
            document.Object,
            null);

        // act
        // assert
        Assert.False(a.Equals(b));
    }

    [Fact]
    public void NotEquals_With_Variables_10()
    {
        // arrange
        var document = new Mock<IDocument>();

        var a = new OperationRequest(
            null,
            "abc",
            document.Object,
            new Dictionary<string, object?>
            {
                {
                    "a",
                    new ConcurrentDictionary<string, object?>(
                        new Dictionary<string, object?> { { "a", null }, })
                },
            });

        var b = new OperationRequest(
            null,
            "abc",
            document.Object,
            new Dictionary<string, object?>
            {
                {
                    "a",
                    new ConcurrentDictionary<string, object?>(
                        new Dictionary<string, object?> { { "c", "b" }, })
                },
            });

        // act
        // assert
        Assert.False(a.Equals(b));
    }

    [Fact]
    public void Equals_With_Variables_List()
    {
        // arrange
        var document = new Mock<IDocument>();

        var a = new OperationRequest(
            null,
            "abc",
            document.Object,
            new Dictionary<string, object?> { { "a", new List<object?> { 1, 2, 3, } }, });

        var b = new OperationRequest(
            null,
            "abc",
            document.Object,
            new Dictionary<string, object?> { { "a", new List<object?> { 1, 2, 3, } }, });

        // act
        // assert
        Assert.True(a.Equals(b));

        b = new OperationRequest(
            null,
            "abc",
            document.Object,
            new Dictionary<string, object?> { { "a", new List<object?> { 1, 3, 2, } }, });

        // act
        // assert
        Assert.False(a.Equals(b));

        b = new OperationRequest(
            null,
            "abc",
            document.Object,
            new Dictionary<string, object?> { { "a", new List<object?> { 1, 3, } }, });

        // act
        // assert
        Assert.False(a.Equals(b));

        b = new OperationRequest(
            null,
            "abc",
            document.Object,
            new Dictionary<string, object?> { { "a", new List<object?> { 1, 2, 3, 4, } }, });

        // act
        // assert
        Assert.False(a.Equals(b));
    }

    [Fact]
    public void Equals_With_Variables_Dictionary()
    {
        // arrange
        var document = new Mock<IDocument>();

        var a = new OperationRequest(
            null,
            "abc",
            document.Object,
            new Dictionary<string, object?> { { "a", new Dictionary<string, object?> { { "b", new List<object?> { 1, 2, 3, } }, } }, });

        var b = new OperationRequest(
            null,
            "abc",
            document.Object,
            new Dictionary<string, object?> { { "a", new Dictionary<string, object?> { { "b", new List<object?> { 1, 2, 3, } }, } }, });

        // act
        // assert
        Assert.True(a.Equals(b));

        b = new OperationRequest(
            null,
            "abc",
            document.Object,
            new Dictionary<string, object?> { { "a", new Dictionary<string, object?> { { "b", new List<object?> { 1, 3, 2, } }, } }, });

        // act
        // assert
        Assert.False(a.Equals(b));

        b = new OperationRequest(
            null,
            "abc",
            document.Object,
            new Dictionary<string, object?> { { "a", new Dictionary<string, object?> { { "b", new List<object?> { 1, 3, } }, } }, });

        // act
        // assert
        Assert.False(a.Equals(b));

        b = new OperationRequest(
            null,
            "abc",
            document.Object,
            new Dictionary<string, object?> { { "a", new Dictionary<string, object?> { { "b", new List<object?> { 1, 2, 3, 4, } }, } }, });

        // act
        // assert
        Assert.False(a.Equals(b));
    }

    [Fact]
    public void Equals_With_Variables_JSON()
    {
        // arrange
        var document = new Mock<IDocument>();

        var a = new OperationRequest(
            null,
            "abc",
            document.Object,
            new Dictionary<string, object?> {
                { "a", new Dictionary<string, object?>
                    {
                        { "b", new Dictionary<string, object?>
                            {
                                { "c", "123456" },
                                { "d", new Dictionary<string, object?>
                                    {
                                        { "e", new List<object?> { 1, 2, 3, 4, } },
                                        { "f", true }, }
                                    },
                                { "g", 123 },
                            }
                        },
                    }
                },
            });

        var b = new OperationRequest(
            null,
            "abc",
            document.Object,
            new Dictionary<string, object?> {
                { "a", new Dictionary<string, object?>
                    {
                        { "b", new Dictionary<string, object?>
                            {
                                { "c", "123456" },
                                { "d", new Dictionary<string, object?>
                                    {
                                        { "e", new List<object?> { 1, 2, 3, 4, } },
                                        { "f", true }, }
                                    },
                                { "g", 123 },
                            }
                        },
                    }
                },
            });

        // act
        // assert
        Assert.True(a.Equals(b));
    }

    [Fact]
    public void Equals_With_Variables_KeyValuePair()
    {
        // arrange
        var document = new Mock<IDocument>();
        var a = new OperationRequest(
            null,
            "abc",
            document.Object,
            new Dictionary<string, object?>
            {
                { "a", new List<KeyValuePair<string, object?>>
                    {
                        new KeyValuePair<string, object?>("b", new List<KeyValuePair<string, object?>>
                        {
                            new KeyValuePair<string, object?>("id", "123456"),
                        }),
                    }
                },
            });

        var b = new OperationRequest(
            null,
            "abc",
            document.Object,
            new Dictionary<string, object?>
            {
                { "a", new List<KeyValuePair<string, object?>>
                    {
                        new KeyValuePair<string, object?>("b", new List<KeyValuePair<string, object?>>
                        {
                            new KeyValuePair<string, object?>("id", "123456"),
                        }),
                    }
                },
            });

        // act
        // assert
        Assert.True(a.Equals(b));
    }

    [Fact]
    public void Equals_No_Variables()
    {
        // arrange
        var document = new Mock<IDocument>();

        var a = new OperationRequest(
            null,
            "abc",
            document.Object);

        var b = new OperationRequest(
            null,
            "abc",
            document.Object);

        // act
        // assert
        Assert.True(a.Equals(b));
    }

    [Fact]
    public void GetHashCode_With_Variables()
    {
        // arrange
        var document = new Mock<IDocument>();

        var a = new OperationRequest(
            null,
            "abc",
            document.Object,
            new Dictionary<string, object?> { { "a", "a" }, });

        var b = new OperationRequest(
            null,
            "abc",
            document.Object,
            new Dictionary<string, object?> { { "a", "a" }, });

        // act
        var hashCodeA = a.GetHashCode();
        var hashCodeB = b.GetHashCode();

        // assert
        Assert.Equal(hashCodeA, hashCodeB);
    }

    [Fact]
    public void GetHashCode_No_Variables()
    {
        // arrange
        var document = new Mock<IDocument>();

        var a = new OperationRequest(
            null,
            "abc",
            document.Object);

        var b = new OperationRequest(
            null,
            "abc",
            document.Object);

        // act
        var hashCodeA = a.GetHashCode();
        var hashCodeB = b.GetHashCode();

        // assert
        Assert.Equal(hashCodeA, hashCodeB);
    }

    [Fact]
    public void Deconstruct()
    {
        // arrange
        var document = new Mock<IDocument>();

        var request = new OperationRequest(
            null,
            "abc",
            document.Object);

        // act
        var (id, name, doc, vars, ext, contextData, _, strategy) = request;

        // assert
        Assert.Equal(request.Id, id);
        Assert.Equal(request.Name, name);
        Assert.Equal(request.Document, doc);
        Assert.Equal(request.Variables, vars);
        Assert.Null(ext);
        Assert.Null(contextData);
        Assert.Equal(request.Strategy, strategy);
    }
}
