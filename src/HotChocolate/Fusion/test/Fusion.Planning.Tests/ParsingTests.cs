using CookieCrumble;
using HotChocolate.Fusion.Planning.Completion;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planing;

public class ParsingTests
{
    [Fact]
    public async Task Test()
    {
        var compositeSchema = FileResource.Open("fusion1.graphql");
        var builder = new CompositeSchemaBuilder();
        builder.Test(Utf8GraphQLParser.Parse(compositeSchema));


    }
}
