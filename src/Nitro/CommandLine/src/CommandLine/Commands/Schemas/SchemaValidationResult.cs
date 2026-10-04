using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine.Commands.Schemas;

internal abstract record SchemaValidationResult
{
    public sealed record Success : SchemaValidationResult
    {
        public static readonly Success Instance = new();

        private Success()
        {
        }
    }

    public sealed record Failed(IRenderable Details) : SchemaValidationResult;
}
