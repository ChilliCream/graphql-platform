namespace StrawberryShake.Remove
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
