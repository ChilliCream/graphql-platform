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

        if(schema.Types.TryGetType(currentName, out var type))
        {
            type.Name = newName;
            return true;
        }

        return false;
    }
}
