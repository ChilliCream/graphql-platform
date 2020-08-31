using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Language;
using HotChocolate.Tests;
using Snapshooter.Xunit;
using Xunit;
using static HotChocolate.Tests.TestHelper;

namespace HotChocolate.Types.Filters
{
    public class FilterInputTypeTest
    {

        [Fact]
        public async Task FilterInputType_DynamicName()
        {
            // arrange
            // act
            var schema = await CreateSchemaAsync(s => s
                .AddType(new FilterInputType<Foo>(d => d
                    .Name(dep => dep.Name + "Foo")
                    .DependsOn<StringType>()
                    .Filter(x => x.Bar)
                    .BindFiltersExplicitly()
                    .AllowEquals())));


            // assert
            schema.ToString().MatchSnapshot();
        }


        [Fact]
        public async Task FilterInputType_DynamicName_NonGeneric()
        {
            // arrange
            // act
            var schema = await CreateSchemaAsync(s => s
                .AddType(new FilterInputType<Foo>(d => d
                    .Name(dep => dep.Name + "Foo")
                    .DependsOn(typeof(StringType))
                    .Filter(x => x.Bar)
                    .BindFiltersExplicitly()
                    .AllowEquals())));


            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task FilterInput_AddDirectives_NameArgs()
        {
            // arrange
            // act
            var schema = await CreateSchemaAsync(s => s
                .AddDirectiveType<FooDirectiveType>()
                .AddType(new FilterInputType<Foo>(d => d
                    .Directive("foo")
                    .Filter(x => x.Bar)
                    .BindFiltersExplicitly()
                    .AllowEquals())));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task FilterInput_AddDirectives_NameArgs2()
        {
            // arrange
            // act
            var schema = await CreateSchemaAsync(s => s
                .AddDirectiveType<FooDirectiveType>()
                .AddType(new FilterInputType<Foo>(d => d
                    .Directive(new NameString("foo"))
                    .Filter(x => x.Bar)
                    .BindFiltersExplicitly()
                    .AllowEquals())));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task FilterInput_AddDirectives_DirectiveNode()
        {
            // arrange
            // act
            var schema = await CreateSchemaAsync(s => s
                .AddDirectiveType<FooDirectiveType>()
                .AddType(new FilterInputType<Foo>(d => d
                    .Directive(new DirectiveNode("foo"))
                    .Filter(x => x.Bar)
                    .BindFiltersExplicitly()
                    .AllowEquals())));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task FilterInput_AddDirectives_DirectiveClassInstance()
        {
            // arrange
            // act
            var schema = await CreateSchemaAsync(s => s
                .AddDirectiveType<FooDirectiveType>()
                .AddType(new FilterInputType<Foo>(d => d
                    .Directive(new FooDirective())
                    .Filter(x => x.Bar)
                    .BindFiltersExplicitly()
                    .AllowEquals())));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task FilterInput_AddDirectives_DirectiveType()
        {
            // arrange
            // act
            var schema = await CreateSchemaAsync(s => s
                .AddDirectiveType<FooDirectiveType>()
                .AddType(new FilterInputType<Foo>(d => d
                    .Directive<FooDirective>()
                    .Filter(x => x.Bar)
                    .BindFiltersExplicitly()
                    .AllowEquals())));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task FilterInput_AddDescription()
        {
            // arrange
            // act
            var schema = await CreateSchemaAsync(s => s
                .AddType(new FilterInputType<Foo>(d => d
                    .Description("Test")
                    .Filter(x => x.Bar)
                    .BindFiltersExplicitly()
                    .AllowEquals())));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task FilterInput_AddName()
        {
            // arrange
            // act
            var schema = await CreateSchemaAsync(s => s
                .AddType(new FilterInputType<Foo>(d => d
                    .Name("Test")
                    .Filter(x => x.Bar)
                    .BindFiltersExplicitly()
                    .AllowEquals())));

            // assert
            schema.ToString().MatchSnapshot();
        }

        private class FooDirectiveType
            : DirectiveType<FooDirective>
        {
            protected override void Configure(
                IDirectiveTypeDescriptor<FooDirective> descriptor)
            {
                descriptor
                    .Name("foo")
                    .Location(DirectiveLocation.InputObject)
                    .Location(DirectiveLocation.InputFieldDefinition);
            }
        }

        private class FooDirective { }

        private class Foo
        {
            public string Bar { get; set; }
        }
    }
}
