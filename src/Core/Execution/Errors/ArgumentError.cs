using HotChocolate.Language;

namespace HotChocolate.Execution
{
    public class ArgumentError
        : FieldError
    {
        public ArgumentError(string message, string argumentName,
            FieldNode fieldSelection)
            : base(message, fieldSelection)
        {
            ArgumentName = argumentName;
        }

        public string ArgumentName { get; }
    }
}
