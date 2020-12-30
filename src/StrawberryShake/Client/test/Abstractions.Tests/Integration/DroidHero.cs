namespace StrawberryShake.Integration
{
    public partial class DroidHero : IHero
    {
        public DroidHero(string name, FriendsConnection friends)
        {
            Name = name;
            Friends = friends;
        }

        public string Name { get; }

        public FriendsConnection Friends { get; }
    }
}
