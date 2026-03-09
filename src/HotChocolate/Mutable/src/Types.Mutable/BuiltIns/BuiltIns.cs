namespace HotChocolate.Types.Mutable;

public static class BuiltIns
{
    public static class String
    {
        public static MutableScalarTypeDefinition Create() => new(SpecScalarNames.String.Name) { IsSpecScalar = true };
    }

    public static class Boolean
    {
        public static MutableScalarTypeDefinition Create() => new(SpecScalarNames.Boolean.Name) { IsSpecScalar = true };
    }

    public static class Float
    {
        public static MutableScalarTypeDefinition Create() => new(SpecScalarNames.Float.Name) { IsSpecScalar = true };
    }

    public static class ID
    {
        public static MutableScalarTypeDefinition Create() => new(SpecScalarNames.ID.Name) { IsSpecScalar = true };
    }

    public static class Int
    {
        public static MutableScalarTypeDefinition Create() => new(SpecScalarNames.Int.Name) { IsSpecScalar = true };
    }

    public static class Include
    {
        public static IncludeMutableDirectiveDefinition Create(MutableSchemaDefinition schema)
        {
            if (!schema.Types.TryGetType<MutableScalarTypeDefinition>(SpecScalarNames.Boolean.Name, out var booleanTypeDef))
            {
                booleanTypeDef = Boolean.Create();
                schema.Types.Add(booleanTypeDef);
            }

            return new IncludeMutableDirectiveDefinition(booleanTypeDef);
        }
    }

    public static class Skip
    {
        public static SkipMutableDirectiveDefinition Create(MutableSchemaDefinition schema)
        {
            if (!schema.Types.TryGetType<MutableScalarTypeDefinition>(SpecScalarNames.Boolean.Name, out var booleanTypeDef))
            {
                booleanTypeDef = Boolean.Create();
                schema.Types.Add(booleanTypeDef);
            }

            return new SkipMutableDirectiveDefinition(booleanTypeDef);
        }
    }

    public static class Deprecated
    {
        public static DeprecatedMutableDirectiveDefinition Create(MutableSchemaDefinition schema)
        {
            if (!schema.Types.TryGetType<MutableScalarTypeDefinition>(SpecScalarNames.String.Name, out var stringTypeDef))
            {
                stringTypeDef = String.Create();
                schema.Types.Add(stringTypeDef);
            }

            return new DeprecatedMutableDirectiveDefinition(stringTypeDef);
        }
    }

    public static class SpecifiedBy
    {
        public static SpecifiedByMutableDirectiveDefinition Create(MutableSchemaDefinition schema)
        {
            if (!schema.Types.TryGetType<MutableScalarTypeDefinition>(SpecScalarNames.String.Name, out var stringTypeDef))
            {
                stringTypeDef = String.Create();
                schema.Types.Add(stringTypeDef);
            }

            return new SpecifiedByMutableDirectiveDefinition(stringTypeDef);
        }
    }

    public static class OneOf
    {
        public static OneOfMutableDirectiveDefinition Create()
        {
            return new OneOfMutableDirectiveDefinition();
        }
    }

    public static class SerializeAs
    {
        public const string Name = "serializeAs";
        public const string Type = "type";
        public const string Pattern = "pattern";
    }
}
