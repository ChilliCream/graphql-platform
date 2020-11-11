namespace HotChocolate.Server.Template
{
    public class Query
    {
        public Person GetPerson() => new Person("Luke Skywalker");
    }

    public record Person(string Name);
}
