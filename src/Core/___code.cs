using HotChocolate;
using HotChocolate.Types;

namespace Foo
{
    public class QueryType
        : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Description("Bla bla bla bla");
            descriptor.Field("a").Type<PersonType>();
        }
    }

    public class PersonType
        : ObjectType<Person>
    {
        protected override void Configure(
            IObjectTypeDescriptor<Person> descriptor)
        {
            descriptor.Field(t => t.Name);
        }
    }

    public class Person
    {
        public string Name { get; set; }
    }

    public class Setup
    {
        public void Do()
        {
            var schema = Schema.Create(c => c.RegisterQuery<QueryType>());
            schema.ExecuteAsync("{ a { name } }");
        }


    }

}
