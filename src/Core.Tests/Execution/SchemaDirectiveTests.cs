using HotChocolate.Types;
using Xunit;

namespace HotChocolate.Execution
{
    public class SchemaDirectiveTests
    {
        [Fact]
        public void Foo()
        {
            // arrange
            ISchema schema = CreateSchema();

            // act
            IExecutionResult result =
                schema.Execute("{ person(name: \"Foo\") { name } }");

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
            });
        }

        public class Query
        {
            public Person GetPerson(string name) => new Person { Name = name };
        }

        public class Person
        {
            public string Name { get; set; }
        }

        public class PersonType
            : ObjectType<Person>
        {
            protected override void Configure(
                IObjectTypeDescriptor<Person> descriptor)
            {
                descriptor.Directive(new AppendStringDirective { S = "Bar" });
            }
        }

        public class AppendStringDirectiveType
            : DirectiveType
        {
            protected override void Configure(
                IDirectiveTypeDescriptor descriptor)
            {
                descriptor.Name("AppendString");
                descriptor.Location(DirectiveLocation.Object);
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
