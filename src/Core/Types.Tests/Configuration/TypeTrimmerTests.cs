using HotChocolate.Language;
using HotChocolate.Types;
using Snapshooter.Xunit;
using Xunit;

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
    }
}
