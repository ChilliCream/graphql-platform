namespace StrawberryShake.Remove
{
    public class Human : ICharacter
    {
        public Human(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
