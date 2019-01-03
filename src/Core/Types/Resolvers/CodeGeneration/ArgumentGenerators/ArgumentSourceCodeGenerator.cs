using System.Text;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal abstract class ArgumentSourceCodeGenerator
        : SourceCodeGenerator<ArgumentDescriptor>
    {
        protected abstract ArgumentKind Kind { get; }

        protected sealed override string Generate(
            string delegateName,
            ArgumentDescriptor descriptor)
        {
            return Generate(descriptor);
        }

        protected abstract string Generate(ArgumentDescriptor descriptor);

        protected sealed override bool CanHandle(ArgumentDescriptor descriptor)
        {
            return descriptor.Kind == Kind;
        }

        protected static string WriteEscapeCharacters(string input)
        {
            var stringBuilder = new StringBuilder();

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                WriteEscapeCharacter(stringBuilder, in c);
            }

            return stringBuilder.ToString();
        }

        private static void WriteEscapeCharacter(
            StringBuilder stringBuilder, in char c)
        {
            switch (c)
            {
                case '"':
                    WriteEscapeCharacterHelper(stringBuilder, '"');
                    break;
                case '\\':
                    WriteEscapeCharacterHelper(stringBuilder, '\\');
                    break;
                case '/':
                    WriteEscapeCharacterHelper(stringBuilder, '/');
                    break;
                case '\a':
                    WriteEscapeCharacterHelper(stringBuilder, 'a');
                    break;
                case '\b':
                    WriteEscapeCharacterHelper(stringBuilder, 'b');
                    break;
                case '\f':
                    WriteEscapeCharacterHelper(stringBuilder, 'f');
                    break;
                case '\n':
                    WriteEscapeCharacterHelper(stringBuilder, 'n');
                    break;
                case '\r':
                    WriteEscapeCharacterHelper(stringBuilder, 'r');
                    break;
                case '\t':
                    WriteEscapeCharacterHelper(stringBuilder, 't');
                    break;
                case '\v':
                    WriteEscapeCharacterHelper(stringBuilder, 'v');
                    break;
                default:
                    stringBuilder.Append(c);
                    break;
            }
        }

        private static void WriteEscapeCharacterHelper(
           StringBuilder stringBuilder, in char c)
        {
            stringBuilder.Append('\\');
            stringBuilder.Append(c);
        }
    }
}
