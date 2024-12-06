using System.Collections.Immutable;
using HotChocolate.Fusion.Logging.Contracts;
using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.PreMergeValidation;

internal sealed class PreMergeValidationContext(CompositionContext context)
{
    public SchemaDefinition[] SchemaDefinitions => context.SchemaDefinitions;
    public ICompositionLog Log => context.Log;
    public ImmutableArray<OutputTypeInfo> OutputTypeInfo = [];

    public void Initialize()
    {
        InitializeOutputTypeInfo();
    }

    /// <summary>
    /// Initializes a structure that makes it easier to access combined output types, fields, and
    /// arguments for validation purposes.
    /// </summary>
    private void InitializeOutputTypeInfo()
    {
        OutputTypeInfo =
        [
            .. SchemaDefinitions
                .SelectMany(s => s.Types)
                .Where(t => t.IsOutputType())
                .OfType<ComplexTypeDefinition>()
                .GroupBy(
                    t => t.Name,
                    (typeName, types) =>
                    {
                        types = types.ToImmutableArray();

                        var fieldInfo = types
                            .SelectMany(t => t.Fields)
                            .GroupBy(
                                f => f.Name,
                                (fieldName, fields) =>
                                {
                                    fields = fields.ToImmutableArray();

                                    var argumentInfo = fields
                                        .SelectMany(f => f.Arguments)
                                        .GroupBy(
                                            a => a.Name,
                                            (argumentName, arguments) =>
                                                new OutputArgumentInfo(
                                                    argumentName,
                                                    [.. arguments]));

                                    return new OutputFieldInfo(
                                        fieldName,
                                        [.. fields],
                                        [.. argumentInfo]);
                                });

                        return new OutputTypeInfo(typeName, [.. types], [.. fieldInfo]);
                    })
        ];
    }
}

internal record OutputTypeInfo(
    string TypeName,
    ImmutableArray<ComplexTypeDefinition> Types,
    ImmutableArray<OutputFieldInfo> FieldInfo);

internal record OutputFieldInfo(
    string FieldName,
    ImmutableArray<OutputFieldDefinition> Fields,
    ImmutableArray<OutputArgumentInfo> Arguments);

internal record OutputArgumentInfo(
    string ArgumentName,
    ImmutableArray<InputFieldDefinition> Arguments);
