namespace StrawberryShake.CodeGeneration.CSharp.Analyzers
{
    public class StrawberryShakeSettings
    {
        public string Name { get; set; } = default!;

        public string? Namespace { get; set; } = default!;

        public string? Url { get; set; }

        public bool DependencyInjection { get; set; } = true;
    }
}
