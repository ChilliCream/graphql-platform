using HotChocolate.Skimmed;
using HotChocolate.Skimmed.Serialization;

namespace HotChocolate.Fusion.Composition;

public interface ITypeMergeHandler
{
    void Merge(IReadOnlyList<INamedType> types);
}

public sealed class SubGraphConfiguration
{
    public SubGraphConfiguration(string name, string schema)
    {
        Name = name;
        Schema = schema;
    }

    public string Name { get; }

    public string Schema { get; }
}

public sealed class CompositionContext
{
    public CompositionContext(IReadOnlyList<SubGraphConfiguration> configurations)
    {
        Configurations = configurations;
    }

    public IReadOnlyList<SubGraphConfiguration> Configurations { get; }

    public List<Schema> SubGraphs { get; } = new();

    public List<EntityGroup> Entities { get; } = new();

    public Schema FusionGraph { get; } = new();

    public CancellationToken Abort { get; set; }

    public ICompositionLog Log { get; }
}

public interface ICompositionLog
{
    bool HasErrors { get; }

    void Info(string message, string code, ITypeSystemMember? member,  Exception? exception = null);
    void Warning(string message, string code, ITypeSystemMember? member,  Exception? exception = null);
    void Error(string message, string code, ITypeSystemMember? member,  Exception? exception = null);
}

public sealed class PreProcess
{
    private readonly Func<CompositionContext, ValueTask> _next;

    public PreProcess(Func<CompositionContext, ValueTask> next)
    {
        _next = next;
    }

    public async Task InvokeAsync(CompositionContext context)
    {
        foreach (var config in context.Configurations)
        {
            var schema = SchemaParser.Parse(config.Schema);
            schema.Name = config.Name;


            // cleanup
            context.SubGraphs.Add(schema);
        }

        await _next(context).ConfigureAwait(false);
    }
}
