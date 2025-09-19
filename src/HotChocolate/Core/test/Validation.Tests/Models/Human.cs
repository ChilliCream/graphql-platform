namespace HotChocolate.Validation;

public class Human(string name, string address) : ISentient
{
    public string Name { get; set; } = name;

    public string Address { get; set; } = address;
}
