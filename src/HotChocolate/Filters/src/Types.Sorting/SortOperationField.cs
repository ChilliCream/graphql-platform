namespace HotChocolate.Types.Sorting
{
    internal sealed class SortOperationField : InputField
    {
        public SortOperationField(SortOperationDefintion definition)
            : base(definition, default)
        {
            Operation = definition.Operation;
        }

        public SortOperation? Operation { get; }
    }
}
