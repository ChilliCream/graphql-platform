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
        /*
        [Fact]
        public void D()
        {
            // arrange
            // act
            DirectiveType directiveType =
                CreateDirective<CustomDirectiveType>();

            // assert


        }
         */


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
            var schemaContext = new SchemaContext();
            schemaContext.Types.RegisterType(new StringType());
            schemaContext.Directives.RegisterDirectiveType<T>();

            var schemaConfiguration = new SchemaConfiguration(
                sp => { }, schemaContext.Types);

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
            }
        }

        public class CustomEnumType
            : EnumType
        {
            protected override void Configure(IEnumTypeDescriptor descriptor)
            {
                descriptor.Directive(new CustomDirective { Argument = "foo" });
            }
        }

        public class CustomMiddleware
        // : IDirectiveFieldResolver
        // , IDirectiveFieldResolverHandler
        {
            public void OnBeforeInvoke(
                IDirectiveContext directiveContext,
                IResolverContext context)
            {
                throw new NotImplementedException();
            }


            public Task<object> OnAfterInvokeAsync(
                IDirectiveContext directiveContext,
                IResolverContext context,
                object resolverResult,
                CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public Task<object> ResolveAsync(
                IDirectiveContext directiveContext,
                IResolverContext resolverContext,
                CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }


        }

        public class CustomDirective
        {
            public string Argument { get; set; }
        }
    }
}
