namespace HotChocolate.Types.Mutable;

public static class BuiltIns
{
    public static class String
    {
        public const string Name = nameof(String);

        public static MutableScalarTypeDefinition Create() => new(Name) { IsSpecScalar = true };
    }

    public static class Boolean
    {
        public const string Name = nameof(Boolean);

        public static MutableScalarTypeDefinition Create() => new(Name) { IsSpecScalar = true };
    }

    public static class Float
    {
        public const string Name = nameof(Float);

        public static MutableScalarTypeDefinition Create() => new(Name) { IsSpecScalar = true };
    }

    public static class ID
    {
        public const string Name = nameof(ID);

        public static MutableScalarTypeDefinition Create() => new(Name) { IsSpecScalar = true };
    }

    public static class Int
    {
        public const string Name = nameof(Int);

        public static MutableScalarTypeDefinition Create() => new(Name) { IsSpecScalar = true };
    }

    public static class Include
    {
        public const string Name = "include";
        public const string If = "if";

        public static IncludeMutableDirectiveDefinition Create(MutableSchemaDefinition schema)
        {
            if (!schema.Types.TryGetType<MutableScalarTypeDefinition>(Boolean.Name, out var booleanTypeDef))
            {
                booleanTypeDef = Boolean.Create();
                schema.Types.Add(booleanTypeDef);
            }

            return new IncludeMutableDirectiveDefinition(booleanTypeDef);
        }
    }

    public static class Skip
    {
        public const string Name = "skip";
        public const string If = "if";

        public static SkipMutableDirectiveDefinition Create(MutableSchemaDefinition schema)
        {
            if (!schema.Types.TryGetType<MutableScalarTypeDefinition>(Boolean.Name, out var booleanTypeDef))
            {
                booleanTypeDef = Boolean.Create();
                schema.Types.Add(booleanTypeDef);
            }

            return new SkipMutableDirectiveDefinition(booleanTypeDef);
        }
    }

    public static class Deprecated
    {
        public const string Name = "deprecated";
        public const string Reason = "reason";

        public static DeprecatedMutableDirectiveDefinition Create(MutableSchemaDefinition schema)
        {
            if (!schema.Types.TryGetType<MutableScalarTypeDefinition>(String.Name, out var stringTypeDef))
            {
                stringTypeDef = String.Create();
                schema.Types.Add(stringTypeDef);
            }

            return new DeprecatedMutableDirectiveDefinition(stringTypeDef);
        }
    }

    public static class SpecifiedBy
    {
        public const string Name = "specifiedBy";
        public const string Url = "url";

        public static SpecifiedByMutableDirectiveDefinition Create(MutableSchemaDefinition schema)
        {
            if (!schema.Types.TryGetType<MutableScalarTypeDefinition>(String.Name, out var stringTypeDef))
            {
                stringTypeDef = String.Create();
                schema.Types.Add(stringTypeDef);
            }

            return new SpecifiedByMutableDirectiveDefinition(stringTypeDef);
        }
    }

    public static class OneOf
    {
        public const string Name = "oneOf";

        public static OneOfMutableDirectiveDefinition Create()
        {
            return new OneOfMutableDirectiveDefinition();
        }
    }

    public static bool IsBuiltInScalar(string name)
        => name switch
        {
            String.Name => true,
            Boolean.Name => true,
            Float.Name => true,
            ID.Name => true,
            Int.Name => true,
            _ => false
        };

    public static bool IsBuiltInDirective(string name)
        => name switch
        {
            Include.Name => true,
            Skip.Name => true,
            Deprecated.Name => true,
            SpecifiedBy.Name => true,
            OneOf.Name => true,
            _ => false
        };
}
