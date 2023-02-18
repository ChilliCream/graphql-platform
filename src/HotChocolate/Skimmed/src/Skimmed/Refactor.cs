namespace HotChocolate.Skimmed;

public static class Refactor
{
    public static bool RenameType(this Schema schema, string currentName, string newName)
    {
        if (schema is null)
        {
            throw new ArgumentNullException(nameof(schema));
        }

        if (string.IsNullOrEmpty(currentName))
        {
            throw new ArgumentException(
                "Value cannot be null or empty.",
                nameof(currentName));
        }

        if (string.IsNullOrEmpty(newName))
        {
            throw new ArgumentException(
                "Value cannot be null or empty.",
                nameof(newName));
        }

        if (schema.Types.TryGetType(currentName, out var type))
        {
            type.Name = newName;
            return true;
        }

        return false;
    }

    public static bool AddDirective(
        this Schema schema,
        string typeName,
        Directive directive)
    {
        if (schema is null)
        {
            throw new ArgumentNullException(nameof(schema));
        }

        if (directive is null)
        {
            throw new ArgumentNullException(nameof(directive));
        }

        if (string.IsNullOrEmpty(typeName))
        {
            throw new ArgumentException(
                "Value cannot be null or empty.",
                nameof(typeName));
        }

        if (schema.Types.TryGetType(typeName, out var type))
        {
            type.Directives.Add(directive);
            return true;
        }

        return false;
    }

    public static bool AddDirective(
        this Schema schema,
        SchemaCoordinate coordinate,
        Directive directive)
    {
        if (schema is null)
        {
            throw new ArgumentNullException(nameof(schema));
        }

        if (directive is null)
        {
            throw new ArgumentNullException(nameof(directive));
        }

        if (schema.TryGetMember<IHasDirectives>(coordinate, out var member))
        {
            member.Directives.Add(directive);
            return true;
        }

        return false;
    }
}
