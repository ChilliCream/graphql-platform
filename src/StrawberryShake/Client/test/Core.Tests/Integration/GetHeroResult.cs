namespace StrawberryShake.Integration
{
    public class GetHeroResult
    {
        public GetHeroResult(
            IHero hero,
            string version)
        {
            Hero = hero;
            Version = version;
        }

        public IHero Hero { get; }

        public string Version { get; }
    }
}
