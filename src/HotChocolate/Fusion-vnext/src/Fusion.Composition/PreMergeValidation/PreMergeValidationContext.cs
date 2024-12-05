using HotChocolate.Fusion.Logging.Contracts;
using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.PreMergeValidation;

internal sealed class PreMergeValidationContext(CompositionContext context)
{
    public SchemaDefinition[] SchemaDefinitions => context.SchemaDefinitions;
    public ICompositionLog Log => context.Log;
    public IEnumerable<OutputTypeInfo> OutputTypeInfo = [];

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
        OutputTypeInfo = SchemaDefinitions
            .SelectMany(s => s.Types)
            .Where(t => t.IsOutputType())
            .OfType<ComplexTypeDefinition>()
            .GroupBy(t => t.Name, (typeName, types) =>
            {
                types = types.ToArray();

                var fieldInfo = types
                    .SelectMany(t => t.Fields)
                    .GroupBy(
                        f => f.Name,
                        (fieldName, fields) =>
                        {
                            fields = fields.ToArray();

                            var argumentInfo = fields
                                .SelectMany(f => f.Arguments)
                                .GroupBy(
                                    a => a.Name,
                                    (argumentName, arguments) =>
                                        new OutputArgumentInfo(argumentName, arguments.ToArray()));

                            return new OutputFieldInfo(
                                fieldName,
                                fields.ToArray(),
                                argumentInfo.ToArray());
                        });

                return new OutputTypeInfo(typeName, types.ToArray(), fieldInfo.ToArray());
            });
    }
}

internal record OutputTypeInfo(
    string TypeName,
    ComplexTypeDefinition[] Types,
    OutputFieldInfo[] FieldInfo);

internal record OutputFieldInfo(
    string FieldName,
    OutputFieldDefinition[] Fields,
    OutputArgumentInfo[] Arguments);

internal record OutputArgumentInfo(
    string ArgumentName,
    InputFieldDefinition[] Arguments);
