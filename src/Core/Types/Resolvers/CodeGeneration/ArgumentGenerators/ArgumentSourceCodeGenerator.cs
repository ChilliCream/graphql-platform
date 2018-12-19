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
    }
}
