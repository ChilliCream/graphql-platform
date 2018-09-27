using HotChocolate.Types;
using Xunit;

namespace HotChocolate.Execution
{
    public class SchemaDirectiveTests
    {
        [Fact]
        public void InheritExecutableDirectiveFromObjectType()
        {
            // arrange
            ISchema schema = CreateSchema();

            // act
            IExecutionResult result =
                schema.Execute("{ person(name: \"Foo\") { name } }");

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        [Fact]
        public void InheritExecutableDirectiveFromField()
        {
            // arrange
            ISchema schema = CreateSchema();

            // act
            IExecutionResult result =
                schema.Execute("{ person(name: \"Foo\") { phone } }");

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        [Fact]
        public void InheritExecutableDirectiveFromInterface()
        {
            // arrange
            ISchema schema = CreateSchema();

            // act
            IExecutionResult result =
                schema.Execute("{ person(name: \"Foo\") { zipCode } }");

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        [Fact]
        public void InheritExecutableDirectiveFromInterfaceField()
        {
            // arrange
            ISchema schema = CreateSchema();

            // act
            IExecutionResult result =
                schema.Execute("{ person(name: \"Foo\") { country } }");

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        public static ISchema CreateSchema()
        {
            return Schema.Create(c =>
            {
                c.RegisterDirective<AppendStringDirectiveType>();
                c.RegisterQueryType<Query>();
                c.RegisterType<PersonType>();
                c.RegisterType<HasCountryType>();
            });
        }

        public class Query
        {
            public Person GetPerson(string name) => new Person { Name = name };
        }

        public class Person
        {
            public string Name { get; set; }
            public string Phone { get; set; }
            public string ZipCode { get; set; }
            public string Country { get; set; }
        }

        public class PersonType
            : ObjectType<Person>
        {
            protected override void Configure(
                IObjectTypeDescriptor<Person> descriptor)
            {
                descriptor.Directive(new AppendStringDirective { S = "Bar" });
                descriptor.Interface<HasCountryType>();
                descriptor.Field(t => t.Phone)
                    .Directive(new AppendStringDirective { S = "Phone" });
            }
        }

        public class HasCountryType
           : InterfaceType
        {
            protected override void Configure(
                IInterfaceTypeDescriptor descriptor)
            {
                descriptor.Directive(new AppendStringDirective { S = "HasCountry" });
                descriptor.Name("HasCountry");
                descriptor.Field("zipCode").Type<StringType>();
                descriptor.Field("country").Type<StringType>()
                    .Directive(new AppendStringDirective { S = "Country" });
            }
        }

        public class AppendStringDirectiveType
            : DirectiveType<AppendStringDirective>
        {
            protected override void Configure(
                IDirectiveTypeDescriptor<AppendStringDirective> descriptor)
            {
                descriptor.Name("AppendString");
                descriptor.Location(DirectiveLocation.Object);
                descriptor.Location(DirectiveLocation.Interface);
                descriptor.Location(DirectiveLocation.FieldDefinition);
                descriptor.OnInvokeResolver(async (ctx, dir, exec, ct) =>
                {
                    return ((string)await exec())
                        + dir.ToObject<AppendStringDirective>().S;
                });
            }
        }

        public class AppendStringDirective
        {
            public string S { get; set; }
        }
    }
}
