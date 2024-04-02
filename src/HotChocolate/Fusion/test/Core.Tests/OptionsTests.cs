using HotChocolate.Fusion.Composition;
using HotChocolate.Fusion.Composition.Features;
using HotChocolate.Fusion.Metadata;
using HotChocolate.Fusion.Shared;
using HotChocolate.Skimmed.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public class OptionsTests
{
    [Fact]
    public async Task Options_Are_Scoped_To_Particular_Gateway_Builder()
    {
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
            new[]
            {
                demoProject.Appointment.ToConfiguration(),
            },
            new FusionFeatureCollection(FusionFeatures.NodeField));

        var services = new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory);

        var executor1 = await services
            .AddFusionGatewayServer("graph1")
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
            .ModifyFusionOptions(options => options.AllowFusionQueryPlan = !options.AllowFusionQueryPlan)
            .BuildRequestExecutorAsync("graph1");

        var executor2 = await services.AddFusionGatewayServer("graph2")
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
            .BuildRequestExecutorAsync("graph2");

        var options1 = executor1.Services.GetRequiredService<IFusionOptionsAccessor>();
        var options2 = executor2.Services.GetRequiredService<IFusionOptionsAccessor>();
        var defaultOptions = new FusionOptions();

        Assert.Equal(options1.AllowFusionQueryPlan, !defaultOptions.AllowQueryPlan);
        Assert.Equal(options2.AllowFusionQueryPlan, defaultOptions.AllowQueryPlan);
    }

    [Fact]
    public async Task Multiple_Option_Modifications_Are_Applied()
    {
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
            new[]
            {
                demoProject.Appointment.ToConfiguration(),
            },
            new FusionFeatureCollection(FusionFeatures.NodeField));

        var services = new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory);

        var executor = await services
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
            .ModifyFusionOptions(options => options.AllowFusionQueryPlan = !options.AllowFusionQueryPlan)
            .ModifyFusionOptions(options => options.IncludeFusionDebugInfo = !options.IncludeFusionDebugInfo)
            .BuildRequestExecutorAsync();

        var options = executor.Services.GetRequiredService<IFusionOptionsAccessor>();
        var defaultOptions = new FusionOptions();

        Assert.Equal(options.AllowFusionQueryPlan, !defaultOptions.AllowQueryPlan);
        Assert.Equal(options.IncludeFusionDebugInfo, !defaultOptions.IncludeDebugInfo);
    }
}
