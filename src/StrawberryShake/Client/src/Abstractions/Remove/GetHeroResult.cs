namespace StrawberryShake.Remove
{
    public class GetHeroResult : IOperationResultData
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
