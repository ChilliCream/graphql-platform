using HotChocolate.Execution;
using HotChocolate.Types.Descriptors;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class CustomScalarTests
{
    [Fact]
    public async Task CustomDirective_With_Directives_Fluent()
    {
        // arrange + act
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddType<FluentCustomScalarType>()
            .BuildSchemaAsync();

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public async Task CustomDirective_With_Directives_Annotation()
    {
        // arrange + act
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddType<AnnotationCustomScalarType>()
            .BuildSchemaAsync();

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public async Task CustomDirective_With_Directives_SDL()
    {
        // arrange + act
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddDocumentFromString(
                """
                schema {
                  query: Query
                }

                type Query {
                  sayHello: Custom!
                }

                directive @custom on SCALAR

                scalar Custom @custom
                """)
            .AddType(new StringType())
            .AddType(new StringType("Custom"))
            .UseField(_ => _)
            .BuildSchemaAsync();

        // assert
        schema.MatchSnapshot();
    }

    public class Query
    {
        [GraphQLType("Custom!")]
        public string SayHello() => "hello";
    }

    public class FluentCustomScalarType : StringType
    {
        public FluentCustomScalarType()
            : base("Custom")
        {
        }

        protected override void Configure(IScalarTypeDescriptor descriptor)
        {
            descriptor.Directive<CustomDirective>();
        }
    }

    [CustomDirective]
    public class AnnotationCustomScalarType : StringType
    {
        public AnnotationCustomScalarType()
            : base("Custom")
        {
        }
    }

    [DirectiveType(DirectiveLocation.Scalar)]
    public class CustomDirective;

    public class CustomDirectiveAttribute : ScalarTypeDescriptorAttribute
    {
        protected override void OnConfigure(
            IDescriptorContext context,
            IScalarTypeDescriptor descriptor,
            Type type)
            => descriptor.Directive<CustomDirective>();
    }
}
