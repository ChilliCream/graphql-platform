using System;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal interface ISourceCodeGenerator
    {
        string Generate(string delegateName, IDelegateDescriptor descriptor);

        bool CanHandle(IDelegateDescriptor descriptor);
    }

    internal abstract class SourceCodeGenerator<TDescriptor>
        : ISourceCodeGenerator
        where TDescriptor : IDelegateDescriptor
    {
        public string Generate(
            string delegateName,
            IDelegateDescriptor descriptor)
        {
            if (descriptor is TDescriptor d)
            {
                return Generate(delegateName, d);
            }

            throw new NotSupportedException("Descriptor not supported.");
        }

        public bool CanHandle(IDelegateDescriptor descriptor)
        {
            if (descriptor is TDescriptor d)
            {
                return CanHandle(d);
            }

            return false;
        }

        protected abstract string Generate(
            string delegateName, TDescriptor descriptor);

        protected virtual bool CanHandle(TDescriptor descriptor) => true;
    }

}
