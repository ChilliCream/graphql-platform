namespace HotChocolate.Skimmed;

public static class BuiltIns
{
    public static class String
    {
        public const string Name = nameof(String);

        public static ScalarTypeDefinition Create() => new(Name) { IsSpecScalar = true };
    }

    public static class Boolean
    {
        public const string Name = nameof(Boolean);

        public static ScalarTypeDefinition Create() => new(Name) { IsSpecScalar = true };
    }

    public static class Float
    {
        public const string Name = nameof(Float);

        public static ScalarTypeDefinition Create() => new(Name) { IsSpecScalar = true };
    }

    public static class ID
    {
        public const string Name = nameof(ID);

        public static ScalarTypeDefinition Create() => new(Name) { IsSpecScalar = true };
    }

    public static class Int
    {
        public const string Name = nameof(Int);

        public static ScalarTypeDefinition Create() => new(Name) { IsSpecScalar = true };
    }

    public static class Include
    {
        public const string Name = "include";
        public const string If = "if";

        public static IncludeDirectiveDefinition Create(SchemaDefinition schema)
        {
            if (!schema.Types.TryGetType<ScalarTypeDefinition>(Boolean.Name, out var booleanTypeDef))
            {
                booleanTypeDef = Boolean.Create();
                schema.Types.Add(booleanTypeDef);
            }

            return new IncludeDirectiveDefinition(booleanTypeDef);
        }
    }

    public static class Skip
    {
        public const string Name = "skip";
        public const string If = "if";

        public static SkipDirectiveDefinition Create(SchemaDefinition schema)
        {
            if (!schema.Types.TryGetType<ScalarTypeDefinition>(Boolean.Name, out var booleanTypeDef))
            {
                booleanTypeDef = Boolean.Create();
                schema.Types.Add(booleanTypeDef);
            }

            return new SkipDirectiveDefinition(booleanTypeDef);
        }
    }

    public static class Deprecated
    {
        public const string Name = "deprecated";
        public const string Reason = "reason";

        public static DeprecatedDirectiveDefinition Create(SchemaDefinition schema)
        {
            if (!schema.Types.TryGetType<ScalarTypeDefinition>(String.Name, out var stringTypeDef))
            {
                stringTypeDef = String.Create();
                schema.Types.Add(stringTypeDef);
            }

            return new DeprecatedDirectiveDefinition(stringTypeDef);
        }
    }

    public static class SpecifiedBy
    {
        public const string Name = "specifiedBy";
        public const string Url = "url";

        public static SpecifiedByDirectiveDefinition Create(SchemaDefinition schema)
        {
            if (!schema.Types.TryGetType<ScalarTypeDefinition>(String.Name, out var stringTypeDef))
            {
                stringTypeDef = String.Create();
                schema.Types.Add(stringTypeDef);
            }

            return new SpecifiedByDirectiveDefinition(stringTypeDef);
        }
    }

    public static class SemanticNonNull
    {
        public const string Name = "semanticNonNull";
        public const string Levels = "levels";

        public static SemanticNonNullDirectiveDefinition Create(SchemaDefinition schema)
        {
            if (!schema.Types.TryGetType<ScalarTypeDefinition>(Int.Name, out var intTypeDef))
            {
                intTypeDef = Int.Create();
                schema.Types.Add(intTypeDef);
            }

            return new SemanticNonNullDirectiveDefinition(intTypeDef);
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
            // SemanticNonNull.Name => true,
            _ => false
        };
}
