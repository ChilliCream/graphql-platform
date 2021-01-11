namespace logging
{
    public class Query
    {
        public Person GetPerson(bool upperCase = false)
        {
            return upperCase ? new Person("Luke Skywalker".ToUpper(), 101) : new Person("Luke Skywalker", 102);
        }
    }

    public class Person
    {
        public Person(string name, int id)
        {
            Name = name;
            Id = id;
        }

        public string Name { get; }
        public int Id { get; }
    }
}