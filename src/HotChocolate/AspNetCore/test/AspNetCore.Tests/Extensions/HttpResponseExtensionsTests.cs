using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace HotChocolate.AspNetCore.Extensions;

public sealed class HttpResponseExtensionsTests
{
    [Theory]
    [InlineData(new string[0], "Accept", new[] { "Accept" })]
    [InlineData(new[] { "Origin" }, "Accept", new[] { "Origin", "Accept" })]
    [InlineData(new[] { "accept" }, "Accept", new[] { "accept" })]
    [InlineData(new[] { "Accept" }, "x-bar, x-foo", new[] { "Accept", "x-bar, x-foo" })]
    [InlineData(new[] { "Accept" }, "accept, x-foo", new[] { "Accept", "x-foo" })]
    [InlineData(new[] { "Origin, Accept" }, "ACCEPT", new[] { "Origin, Accept" })]
    [InlineData(new[] { "Origin, , Accept" }, "accept", new[] { "Origin, , Accept" })]
    [InlineData(new[] { "*" }, "Accept", new[] { "*" })]
    [InlineData(new[] { "Origin", "*" }, "Accept, x-foo", new[] { "Origin", "*" })]
    public void AppendVary_Should_AddOnlyNamesNotYetListed_When_NamesAreGiven(
        string[] existing,
        string fieldNames,
        string[] expected)
    {
        // arrange
        IHeaderDictionary headers = new HeaderDictionary();

        if (existing.Length > 0)
        {
            headers.Vary = existing;
        }

        // act
        headers.AppendVary(fieldNames);

        // assert
        Assert.Equal(expected, headers.Vary.ToArray());
    }

    [Fact]
    public void AppendVary_Should_PreserveValuesAndAccessOrder_When_HeadersAreCustom()
    {
        // arrange
        (string Name, string[] Existing, string FieldNames)[] cases =
        [
            ("SingleMissing", ["Accept"], " accept , X-foo "),
            ("MultipleMissing", ["Accept"], " accept , X-foo , X-foo "),
            ("AllFound", ["Accept"], "aCcEpT, ACCEPT"),
            ("AllMissing", ["Accept"], " X-foo ,  X-bar "),
            ("Empty", ["Accept"], " , \t , "),
            ("Wildcard", ["Origin", "*"], "Accept, X-foo"),
            ("MultipleExistingValues", ["Origin, Accept", "x-bar"], "origin, X-Foo, ACCEPT"),
            ("RepeatedFoundNames", ["Accept"], "accept, X-foo, ACCEPT"),
            ("MissingWildcard", ["Accept"], "Accept, *")
        ];
        var results = new List<object>();

        // act
        foreach (var item in cases)
        {
            var headers = new RecordingHeaderDictionary
            {
                ["Vary"] = item.Existing
            };
            IHeaderDictionary customHeaders = headers;
            customHeaders.AppendVary(item.FieldNames);
            results.Add(new { item.Name, Vary = headers["Vary"].ToArray(), headers.Calls });
        }

        // assert
        Snapshot.Create()
            .Add(results, "Vary Values And Header Access")
            .MatchMarkdownSnapshot();
    }

    private sealed class RecordingHeaderDictionary : HeaderDictionary, IHeaderDictionary
    {
        public List<string> Calls { get; } = [];

        StringValues IHeaderDictionary.Vary
        {
            get
            {
                Calls.Add("get");
                return this["Vary"];
            }
            set
            {
                Calls.Add("set");
                this["Vary"] = value;
            }
        }
    }
}
