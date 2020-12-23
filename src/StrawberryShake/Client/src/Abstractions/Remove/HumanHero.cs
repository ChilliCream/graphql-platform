namespace StrawberryShake.Remove
{
    public partial class HumanHero : IHero
    {
        public HumanHero(string name, FriendsConnection friends)
        {
            Name = name;
            Friends = friends;
        }

        public string Name { get; }

        public FriendsConnection Friends { get; }
    }
}
