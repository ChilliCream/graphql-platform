using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition.Types;

public static class CompositeSchemaSpec
{
    /// <summary>
    /// Provides metadata and factory methods for the lookup directive.
    /// </summary>
    public static class Lookup
    {
        /// <summary>
        /// The name of the @lookup directive.
        /// </summary>
        public const string Name = "lookup";

        /// <summary>
        /// Creates a new @lookup directive definition.
        /// </summary>
        /// <returns>
        /// Returns a new @lookup directive definition.
        /// </returns>
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

    public static class Require
    {
        public const string Name = "require";
        public const string Field = "field";

        public static RequireDirectiveDefinition Create(SchemaDefinition schema)
        {
            if(!schema.Types.TryGetType<FieldSelectionMapType>(FieldSelectionMap.Name, out var type))
            {
                type = FieldSelectionMap.Create();
                schema.Types.Add(type);
            }

            return new RequireDirectiveDefinition(type);
        }
    }

    public static class FieldSelectionMap
    {
        public const string Name = "FieldSelectionMap";

        public static FieldSelectionMapType Create() => new();
    }
}
