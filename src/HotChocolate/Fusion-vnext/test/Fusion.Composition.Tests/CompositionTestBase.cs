using System.Reflection;
using HotChocolate.Fusion;
using HotChocolate.Fusion.Logging;
using HotChocolate.Skimmed.Serialization;
using Xunit.Sdk;
using Xunit.v3;

namespace HotChocolate.Composition;

public abstract class CompositionTestBase
{
    internal static CompositionContext CreateCompositionContext(string[] sdl)
    {
        return new CompositionContext(
            [
                .. sdl.Select((s, i) =>
                {
                    var schemaDefinition = SchemaParser.Parse(s);
                    schemaDefinition.Name = ((char)('A' + i)).ToString();

                    return schemaDefinition;
                })
            ],
            new CompositionLog());
    }

    public readonly record struct InvalidTestData(string[] Sdl, string[] ErrorMessages);

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    protected sealed class ValidInlineDataAttribute : DataAttribute
    {
        public required string[] Sdl { get; init; }

        public override bool SupportsDiscoveryEnumeration() => true;

        public override ValueTask<IReadOnlyCollection<ITheoryDataRow>> GetData(MethodInfo testMethod,
            DisposalTracker disposalTracker)
        {
            return ValueTask.FromResult<IReadOnlyCollection<ITheoryDataRow>>([
                new TheoryDataRow<string[]>(Sdl) { TestDisplayName = TestDisplayName }
            ]);
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    protected sealed class InvalidInlineDataAttribute : DataAttribute
    {
        public required string[] Sdl { get; init; }
        public required string[] ErrorMessages { get; init; }

        public override bool SupportsDiscoveryEnumeration() => true;

        public override ValueTask<IReadOnlyCollection<ITheoryDataRow>> GetData(MethodInfo testMethod,
            DisposalTracker disposalTracker)
        {
            return ValueTask.FromResult<IReadOnlyCollection<ITheoryDataRow>>([
                new TheoryDataRow(Sdl, ErrorMessages) { TestDisplayName = TestDisplayName }
            ]);
        }
    }
}
