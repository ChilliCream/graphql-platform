namespace HotChocolate.Template.AzureFunctions;

public class Query
{
    public Person GetPerson() => new Person("Luke Skywalker");
}

public class Person
{
    public Person(string name)
    {
        Name = name;
    }

    public string Name { get; }
}
