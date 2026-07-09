namespace HotChocolate.Types.Composite;

internal sealed class ScalarSerializationTypeType : EnumType<ScalarSerializationType>
{
    protected override void Configure(IEnumTypeDescriptor<ScalarSerializationType> descriptor)
    {
        descriptor.Name("ScalarSerializationType");
        descriptor.BindValuesExplicitly();
        descriptor.Value(ScalarSerializationType.String).Name("STRING");
        descriptor.Value(ScalarSerializationType.Boolean).Name("BOOLEAN");
        descriptor.Value(ScalarSerializationType.Int).Name("INT");
        descriptor.Value(ScalarSerializationType.Float).Name("FLOAT");
        descriptor.Value(ScalarSerializationType.Object).Name("OBJECT");
        descriptor.Value(ScalarSerializationType.List).Name("LIST");
    }
}
