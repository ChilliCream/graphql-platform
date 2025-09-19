namespace StrawberryShake.CodeGeneration;

public static class CodeGenerationErrorCodes
{
    /// <summary>
    /// A document contains a syntax error and therefore is no valid graphql document.
    /// </summary>
    public const string SyntaxError = "SS0001";

    /// <summary>
    /// The executable documents contain fields, that do not exist
    /// in the server schema.
    /// </summary>
    public const string SchemaValidationError = "SS0002";

    /// <summary>
    /// A union type must consist of only entity types or only data types.
    /// Ensure that your @key directive correctly captures your entities.
    /// </summary>
    public const string UnionTypeDataEntityMixed = "SS0013";
}
