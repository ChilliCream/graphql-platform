using HotChocolate.Skimmed;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Composition.Types;

public class LookupDirectiveDefinition : DirectiveDefinition
{
    internal LookupDirectiveDefinition() : base(BuiltIns.Lookup.Name)
    {
        Locations = DirectiveLocation.FieldDefinition;
    }
}

public sealed class InternalDirectiveDefinition : DirectiveDefinition
{
    internal InternalDirectiveDefinition() : base(BuiltIns.Internal.Name)
    {
        Locations = DirectiveLocation.FieldDefinition;
    }
}

public sealed class IsDirectiveDefinition : DirectiveDefinition
{
    internal IsDirectiveDefinition(FieldSelectionMapType fieldSelectionMapType)
        : base(BuiltIns.Is.Name)
    {
        Locations = DirectiveLocation.ArgumentDefinition;
        Arguments.Add(new InputFieldDefinition(BuiltIns.Is.Field, fieldSelectionMapType));
    }

    public InputFieldDefinition Field => Arguments[BuiltIns.Is.Field];

    public Directive Create(FieldSelectionMap fieldSelectionMap)
    {
        return new(this, new ArgumentAssignment(BuiltIns.Is.Field, fieldSelectionMap));
    }
}

public sealed class FieldSelectionMapType : ScalarTypeDefinition
{
    internal FieldSelectionMapType()
        : base(BuiltIns.FieldSelectionMap.Name)
    {

    }
}

public static class BuiltIns
{
    public static class Lookup
    {
        public const string Name = "lookup";

        public static LookupDirectiveDefinition Create() => new();
    }

    public static class Internal
    {
        public const string Name = "internal";

        public static InternalDirectiveDefinition Create() => new();
    }

    public static class Is
    {
        public const string Name = "is";
        public const string Field = "field";

        public static IsDirectiveDefinition Create(SchemaDefinition schema)
        {
            if(!schema.Types.TryGetType<FieldSelectionMapType>(FieldSelectionMap.Name, out var type))
            {
                type = FieldSelectionMap.Create();
                schema.Types.Add(type);
            }

            return new IsDirectiveDefinition(type);
        }
    }

    public static class FieldSelectionMap
    {
        public const string Name = "FieldSelectionMap";

        public static FieldSelectionMapType Create() => new();
    }
}
