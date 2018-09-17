using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;
using Xunit;

namespace HotChocolate.Types
{
    public class DirectiveTypeTests
    {
        [Fact]
        public void ConfigureTypedDirectiveWithResolver()
        {
            // arrange
            // act
            DirectiveType directiveType =
                CreateDirective<CustomDirectiveType>();

            // assert
            Assert.True(directiveType.IsExecutable);
            Assert.NotNull(directiveType.OnInvokeResolver);
            Assert.Equal(typeof(CustomDirective), directiveType.ClrType);
        }

        [Fact]
        public void ConfigureDirectiveWithResolver()
        {
            // arrange
            DirectiveType directiveType = new DirectiveType(
                t => t.Name("Foo")
                    .Location(DirectiveLocation.Field)
                    .Resolver((dctx, rctx, ct) => "foo"));

            // act
            directiveType = CreateDirective(directiveType);

            // assert
            Assert.True(directiveType.IsExecutable);
            Assert.NotNull(directiveType.OnInvokeResolver);
            Assert.Null(directiveType.ClrType);
        }

        [Fact]
        public void ConfigureIsNull()
        {
            // act
            Action a = () => new DirectiveType(null);

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        public void NoName()
        {
            // act
            Action a = () => new DirectiveType(d => { });

            // assert
            Assert.Throws<InvalidOperationException>(a);
        }

        private DirectiveType CreateDirective<T>()
            where T : DirectiveType, new()
        {
            return CreateDirective(new T());
        }

        private DirectiveType CreateDirective<T>(T directiveType)
            where T : DirectiveType
        {
            var schemaContext = new SchemaContext();
            schemaContext.Types.RegisterType(new StringType());
            schemaContext.Directives.RegisterDirectiveType(directiveType);

            var schemaConfiguration = new SchemaConfiguration(
                sp => { },
                schemaContext.Types,
                schemaContext.Directives);

            var typeFinalizer = new TypeFinalizer(schemaConfiguration);
            typeFinalizer.FinalizeTypes(schemaContext, null);

            return schemaContext.Directives.GetDirectiveTypes().Single();
        }

        public class CustomDirectiveType
            : DirectiveType<CustomDirective>
        {
            protected override void Configure(
                IDirectiveTypeDescriptor<CustomDirective> descriptor)
            {
                descriptor.Name("Custom");
                descriptor.Location(DirectiveLocation.Enum);
                descriptor.Location(DirectiveLocation.Field);
                descriptor.Resolver((dctx, rctx, ct) => "foo");
            }
        }

        public class CustomDirective
        {
            public string Argument { get; set; }
        }
    }
}
