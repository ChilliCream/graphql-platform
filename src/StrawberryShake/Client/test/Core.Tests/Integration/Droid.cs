namespace StrawberryShake.Integration
{
    public class Droid : ICharacter
    {
        public Droid(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
