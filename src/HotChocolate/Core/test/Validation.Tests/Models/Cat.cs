namespace HotChocolate.Validation
{
    public class Cat
        : IPet
    {
        public string Name { get; set; }

        public string Nickname { get; set; }

        public int? MeowVolume { get; set; }

        public bool DoesKnowCommand(CatCommand catCommand)
        {
            return true;
        }
    }
}
