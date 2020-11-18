using HotChocolate.Language;
using HotChocolate.Types;
using Snapshooter.Xunit;
using Xunit;
using DirectiveLocation = HotChocolate.Types.DirectiveLocation;

namespace HotChocolate.Configuration
{
    public class TypeTrimmerTests
    {
        [Fact]
        private void RemoveUnusedTypes()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("abc")
                    .Field("field")
                    .Type<StringType>()
                    .Resolver("test"))
                .AddMutationType(c => c
                    .Name("def")
                    .Field("field")
                    .Type<IntType>()
                    .Resolver("test"))
                .AddSubscriptionType(c => c
                    .Name("ghi")
                    .Field("field")
                    .Type<BooleanType>()
                    .Resolver("test"))
                .AddObjectType(c => c
                    .Name("thisTypeWillBeRemoved")
                    .Field("field")
                    .Type<StringType>()
                    .Resolver("test"))
                .AddInputObjectType(c => c
                    .Name("thisTypeWillBeRemovedInput")
                    .Field("field")
                    .Type<StringType>())
                .AddType<FloatType>()
                .ModifyOptions(o => o.RemoveUnreachableTypes = true)
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        private void Interface_Implementors_Correctly_Detected()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("abc")
                    .Field("field")
                    .Type(new NamedTypeNode("def"))
                    .Resolver("test"))
                .AddInterfaceType(c => c
                    .Name("def")
                    .Field("field")
                    .Type<StringType>())
                .AddObjectType(c => c
                    .Name("ghi")
                    .Implements(new NamedTypeNode("def"))
                    .Field("field")
                    .Type<StringType>()
                    .Resolver("test"))
                .AddType<FloatType>()
                .ModifyOptions(o => o.RemoveUnreachableTypes = true)
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        private void Union_Set_Is_Correctly_Detected()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("abc")
                    .Field("field")
                    .Type(new NamedTypeNode("def"))
                    .Resolver("test"))
                .AddUnionType(c => c
                    .Name("def")
                    .Type(new NamedTypeNode("ghi")))
                .AddObjectType(c => c
                    .Name("ghi")
                    .Field("field")
                    .Type<StringType>()
                    .Resolver("test"))
                .AddType<FloatType>()
                .ModifyOptions(o => o.RemoveUnreachableTypes = true)
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        private void Unused_TypeSystem_Directives_Are_Removed()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("abc")
                    .Field("field")
                    .Type(new NamedTypeNode("def"))
                    .Resolver("test"))
                .AddInterfaceType(c => c
                    .Name("def")
                    .Field("field")
                    .Type<StringType>())
                .AddObjectType(c => c
                    .Name("ghi")
                    .Directive("_abc")
                    .Implements(new NamedTypeNode("def"))
                    .Field("field")
                    .Type<StringType>()
                    .Resolver("test"))
                .AddDirectiveType(new DirectiveType(d => d
                    .Name("_abc")
                    .Location(DirectiveLocation.Object)))
                .AddDirectiveType(new DirectiveType(d => d
                    .Name("_def")
                    .Location(DirectiveLocation.Object)))
                .AddType<FloatType>()
                .ModifyOptions(o => o.RemoveUnreachableTypes = true)
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        private void Executable_Directives_Are_Never_Removed()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("abc")
                    .Field("field")
                    .Type(new NamedTypeNode("def"))
                    .Resolver("test"))
                .AddInterfaceType(c => c
                    .Name("def")
                    .Field("field")
                    .Type<StringType>())
                .AddObjectType(c => c
                    .Name("ghi")
                    .Implements(new NamedTypeNode("def"))
                    .Field("field")
                    .Type<StringType>()
                    .Resolver("test"))
                .AddDirectiveType(new DirectiveType(d => d
                    .Name("_abc")
                    .Location(DirectiveLocation.Query)))
                .AddType<FloatType>()
                .ModifyOptions(o => o.RemoveUnreachableTypes = true)
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        private void Executable_Directives_Are_Never_Removed_2()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("abc")
                    .Field("field")
                    .Type(new NamedTypeNode("def"))
                    .Resolver("test"))
                .AddInterfaceType(c => c
                    .Name("def")
                    .Field("field")
                    .Type<StringType>())
                .AddObjectType(c => c
                    .Name("ghi")
                    .Implements(new NamedTypeNode("def"))
                    .Field("field")
                    .Type<StringType>()
                    .Resolver("test"))
                .AddDirectiveType(new DirectiveType(d => d
                    .Name("_abc")
                    .Location(DirectiveLocation.Object| DirectiveLocation.Query)))
                .AddType<FloatType>()
                .ModifyOptions(o => o.RemoveUnreachableTypes = true)
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }
    }
}
