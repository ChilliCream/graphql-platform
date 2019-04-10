using System.Collections.Generic;
using Xunit;

namespace HotChocolate.Types
{
    public class ObjectTypeExtensionTests
    {
        [Fact]
        public void TypeExtension_AddField()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<FooType>()
                .AddType<FooTypeExtension>()
                .Create();

            // assert
            ObjectType type = schema.GetType<ObjectType>("Foo");
            Assert.True(type.Fields.ContainsField("test"));
        }

        // TODO : ADD THE FOLLOWING TESTS:
        // Overwrite reslver
        // Add Middleware
        // Depricate Field
        // Depricate Argument
        // Add Context Data to Type
        // Add Context Data to Field
        // Add Context Data to Argument
        // Add Directive to Type
        // Add Directive to Field
        // Add Directive to Argument
        // Replace Directive to Type
        // Replace Directive to Field
        // Replace Directive to Argument
        // Add Repeatable Directive to Type
        // Add Repeatable Directive to Field
        // Add Repeatable Directive to Argument

        public class FooType
            : ObjectType<Foo>
        {
            protected override void Configure(IObjectTypeDescriptor<Foo> descriptor)
            {
                descriptor.Field(t => t.Description);
            }
        }

        public class FooTypeExtension
            : ObjectTypeExtension
        {
            protected override void Configure(IObjectTypeDescriptor descriptor)
            {
                descriptor.Name("Foo");
                descriptor.Field("test")
                    .Resolver(() => new List<string>())
                    .Type<ListType<StringType>>();
            }
        }

        public class Foo
        {
            public string Description { get; } = "hello";
        }
    }
}
