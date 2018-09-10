using System;
using System.Linq;
using HotChocolate.Configuration;
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
            schemaContext.Directives.RegisterDirective<T>();

            var schemaConfiguration = new SchemaConfiguration(
                sp => { }, schemaContext.Types);

            var typeFinalizer = new TypeFinalizer(schemaConfiguration);
            typeFinalizer.FinalizeTypes(schemaContext, null);

            return schemaContext.Directives.GetDirectives().Single();
        }

        public class CustomDirectiveType
            : DirectiveType<CustomDirective>
        {
            protected override void Configure(
                IDirectiveTypeDescriptor<CustomDirective> descriptor)
            {
                descriptor.Name("Custom");
            }
        }

        public class CustomDirective
        {
            public string Argument { get; set; }
        }
    }
}
