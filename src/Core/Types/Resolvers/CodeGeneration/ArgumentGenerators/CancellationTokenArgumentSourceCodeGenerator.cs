namespace HotChocolate.Resolvers.CodeGeneration
{
    internal sealed class CancellationTokenArgumentSourceCodeGenerator
        : ArgumentSourceCodeGenerator
    {
        protected override ArgumentKind Kind => ArgumentKind.CancellationToken;

        protected override string Generate(
            ArgumentDescriptor descriptor)
        {
            return "ct";
        }
    }
}
