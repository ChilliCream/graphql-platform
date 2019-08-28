using System;
using System.Threading.Tasks;
using StrawberryShake.Generators.Descriptors;
using StrawberryShake.Generators.Utilities;

namespace StrawberryShake.Generators
{
    public abstract class CodeGenerator<T>
        : ICodeGenerator
        where T : ICodeDescriptor
    {
        public bool CanHandle(ICodeDescriptor descriptor)
        {
            return descriptor is T;
        }

        public string CreateFileName(ICodeDescriptor descriptor)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (descriptor is T t)
            {
                return CreateFileName(t);
            }

            throw new ArgumentException(
                "The code generator expected " +
                $"descriptor type `{typeof(T).FullName}`.");
        }

        public Task WriteAsync(
            CodeWriter writer,
            ICodeDescriptor descriptor,
            ITypeLookup typeLookup)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (typeLookup is null)
            {
                throw new ArgumentNullException(nameof(typeLookup));
            }

            if (descriptor is T t)
            {
                return WriteAsync(writer, t, typeLookup);
            }

            throw new ArgumentException(
                "The code generator expected " +
                $"descriptor type `{typeof(T).FullName}`.");
        }

        protected abstract Task WriteAsync(
            CodeWriter writer,
            T descriptor,
            ITypeLookup typeLookup);

        protected virtual string CreateFileName(T descriptor)
        {
            return descriptor.Name + ".cs";
        }
    }
}
