namespace HotChocolate.Skimmed;

public static class BuiltIns
{
    public static class String
    {
        public const string Name = nameof(String);

        public static StringTypeDefinition Create()
            => new();
    }

    public static class Boolean
    {
        public const string Name = nameof(Boolean);

        public static BooleanTypeDefinition Create()
            => new();
    }

    public static class Float
    {
        public const string Name = nameof(Float);

        public static FloatTypeDefinition Create()
            => new();
    }

    public static class ID
    {
        public const string Name = nameof(ID);

        public static IDTypeDefinition Create()
            => new();
    }

    public static class Int
    {
        public const string Name = nameof(Int);

        public static IntTypeDefinition Create()
            => new();
    }

    public static class Include
    {
        public const string Name = "include";
        public const string If = "if";

        public static IncludeDirectiveDefinition Create(SchemaDefinition schema)
        {
            if (!schema.Types.TryGetType<BooleanTypeDefinition>(Boolean.Name, out var typeDef))
            {
                typeDef = new BooleanTypeDefinition();
                schema.Types.Add(typeDef);
            }

            return new IncludeDirectiveDefinition(typeDef);
        }
    }

    public static class Skip
    {
        public const string Name = "skip";
        public const string If = "if";

        public static SkipDirectiveDefinition Create(SchemaDefinition schema)
        {
            if (!schema.Types.TryGetType<BooleanTypeDefinition>(Boolean.Name, out var typeDef))
            {
                typeDef = new BooleanTypeDefinition();
                schema.Types.Add(typeDef);
            }

            return new SkipDirectiveDefinition(typeDef);
        }
    }

    public static class Deprecated
    {
        public const string Name = "deprecated";
        public const string Reason = "reason";

        public static DeprecatedDirectiveDefinition Create(SchemaDefinition schema)
        {
            if (!schema.Types.TryGetType<StringTypeDefinition>(String.Name, out var typeDef))
            {
                typeDef = new StringTypeDefinition();
                schema.Types.Add(typeDef);
            }

            return new DeprecatedDirectiveDefinition(typeDef);
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
            _ => false
        };
}
