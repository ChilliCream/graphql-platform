namespace StrawberryShake.Integration
{
    public interface IHero : ICharacter
    {
        FriendsConnection Friends { get; }
    }
}
