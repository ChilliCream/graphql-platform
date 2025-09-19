namespace HotChocolate.Execution.Integration.InputOutputObjectAreTheSame;

public class Query
{
    public Person GetPerson(Person person)
    {
        return person;
    }
}
