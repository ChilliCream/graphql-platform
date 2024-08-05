using CookieCrumble;
using HotChocolate.Fusion.Types.Completion;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planing;

public class ParsingTests
{
    [Fact]
    public void Test()
    {
        var compositeSchemaDoc = Utf8GraphQLParser.Parse(FileResource.Open("fusion1.graphql"));
        var compositeSchema = CompositeSchemaBuilder.Create(compositeSchemaDoc);
        Assert.Equal("Query", compositeSchema.QueryType.Name);
    }
}
