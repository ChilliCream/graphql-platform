using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types
{
    public class InputObjectTypeParseLiteralTests
    {
        [Fact]
        public void ParseLiteralWithFieldWithNoProperty()
        {
            // arrange
            ISchema schema = Schema.Create(
                c =>
                {
                    c.RegisterQueryType<Query>();
                    c.RegisterType<SearchParametersType>();
                });

            SearchParametersType type = schema.GetType<SearchParametersType>("SearchParametersInput");

            // act
            var parsed = type.ParseLiteral(new ObjectValueNode(new ObjectFieldNode("term", "name")));

            // assert
            Assert.IsType<SearchParameters>(parsed);
        }

        [Fact]
        public void ParseLiteralWithDefaultValues()
        {
            // arrange
            ISchema schema = Schema.Create(
                c =>
                {
                    c.RegisterQueryType(new ObjectType(d =>
                    {
                        d.Name("Query");
                        d.Field("search")
                            .Type<StringType>()
                            .Argument("filter", a => a.Type(new NamedTypeNode("ExportParametersInput")))
                            .Resolver("found");
                    }));
                    c.RegisterType<ExportParametersType>();
                });

            InputObjectType type = schema.GetType<InputObjectType>("ExportParametersInput");

            // act
            var parsed = type.ParseLiteral(new ObjectValueNode(new ObjectFieldNode("first", "first")));

            // assert
            Assert.IsType<SearchParameters>(parsed);
        }

        public class Query
        {
            public string Search(SearchParameters parameters) => parameters.Term;
        }

        public class SearchParameters
        {
            public string Term { get; set; }

            public string Order { get; set; }
        }

        public class SearchParametersType : InputObjectType<SearchParameters>
        {
            protected override void Configure(IInputObjectTypeDescriptor<SearchParameters> descriptor)
            {
                descriptor.Field("term")
                    .Type<NonNullType<StringType>>();
            }
        }

        public class ExportParametersType : InputObjectType
        {
            protected override void Configure(IInputObjectTypeDescriptor descriptor)
            {
                descriptor.Name("ExportParametersInput");

                descriptor.Field("first")
                    .Type<StringType>();

                descriptor.Field("second")
                    .Type<StringType>();
            }
        }
    }
}
