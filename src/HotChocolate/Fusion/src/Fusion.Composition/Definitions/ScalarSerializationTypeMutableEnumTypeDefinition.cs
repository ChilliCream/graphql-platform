using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Definitions;

internal sealed class ScalarSerializationTypeMutableEnumTypeDefinition : MutableEnumTypeDefinition
{
    public ScalarSerializationTypeMutableEnumTypeDefinition()
        : base(WellKnownTypeNames.ScalarSerializationType)
    {
        Values.Add(new MutableEnumValue("STRING"));
        Values.Add(new MutableEnumValue("BOOLEAN"));
        Values.Add(new MutableEnumValue("INT"));
        Values.Add(new MutableEnumValue("FLOAT"));
        Values.Add(new MutableEnumValue("OBJECT"));
        Values.Add(new MutableEnumValue("LIST"));
    }

    public static ScalarSerializationTypeMutableEnumTypeDefinition Create()
    {
        return new ScalarSerializationTypeMutableEnumTypeDefinition();
    }
}
