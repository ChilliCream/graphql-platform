using CookieCrumble;
using HotChocolate.Fusion.Shared;
using HotChocolate.Skimmed.Serialization;
using Xunit.Abstractions;

namespace HotChocolate.Fusion.Composition;

public class ResolverTests(ITestOutputHelper output)
{
    private readonly Func<ICompositionLog> _logFactory = () => new TestCompositionLog(output);

    [Fact]
    public async Task Variables_Are_Computed_Even_Without_Resolver()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var composer = new FusionGraphComposer(logFactory: _logFactory);

        var fusionConfig = await composer.ComposeAsync(
            new[]
            {
                demoProject.Appointment.ToConfiguration(),
                demoProject.Patient1.ToConfiguration(),
            });

        SchemaFormatter
            .FormatAsString(fusionConfig)
            .MatchSnapshot(extension: ".graphql");
    }
}
