namespace HotChocolate.Validation
{
    public interface IDocumentValidatorFactory
    {
        IDocumentValidator CreateValidator(NameString schemaName = default);
    }
}
