using System.Threading.Tasks;
using CookieCrumble;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.Directives;

public class TagDirectiveTests
{
    [Fact]
    public async Task EnsureAllLocationsAreApplied()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .AddObjectType<Foo>()
                .AddType<FooDirective>()
                .SetSchema(d => d.Tag("OnSchema"))
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Tag("OnObjectType")]
    public class Query
    {
        [Tag("OnObjectField")]
        public IFoo GetFoo([Tag("OnObjectFieldArg")] string a) => new Foo();

        public FooEnum GetFooEnum(FooInput input) => FooEnum.Foo;
    }

    [Tag("OnInterface")]
    public interface IFoo
    {
        [Tag("OnInterfaceField")]
        string Bar([Tag("OnInterfaceFieldArg")] string baz);
    }

    public class Foo : IFoo
    {
        public string Bar(string baz) => "Bar" + baz;
    }

    [Tag("OnEnum")]
    public enum FooEnum
    {
        [Tag("OnEnumValue")]
        Foo,
        Bar,
    }

    [Tag("OnInputObjectType")]
    public class FooInput
    {
        [Tag("OnInputObjectField")]
        public string Bar { get; set; }
    }

    [DirectiveType(DirectiveLocation.Query)]
    public class FooDirective
    {
        [Tag("OnDirectiveArgument")]
        public string Arg { get; set; }
    }
}
