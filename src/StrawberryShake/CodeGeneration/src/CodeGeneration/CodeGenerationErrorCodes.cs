namespace StrawberryShake.CodeGeneration
{
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
        /// The generator has not found any type documents and can therefore
        /// not generate anything.
        /// </summary>
        public const string NoTypeDocumentsFound = "SS0003";

        /// <summary>
        /// The generator has not found any executable documents and can therefore
        /// not generate anything.
        /// </summary>
        public const string NoExecutableDocumentsFound = "SS0004";
    }
}
