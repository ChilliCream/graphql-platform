using System;

namespace HotChocolate.Types.Descriptors
{
    internal class InputUnionTypeNameDependencyDescriptor
        : IInputUnionTypeNameDependencyDescriptor
    {
        private readonly IInputUnionTypeDescriptor _descriptor;
        private readonly Func<INamedType, NameString> _createName;

        public InputUnionTypeNameDependencyDescriptor(
            IInputUnionTypeDescriptor descriptor,
            Func<INamedType, NameString> createName)
        {
            _descriptor = descriptor
                ?? throw new ArgumentNullException(nameof(descriptor));
            _createName = createName
                ?? throw new ArgumentNullException(nameof(createName));
        }

        public IInputUnionTypeDescriptor DependsOn<TDependency>()
            where TDependency : IType
        {
            TypeNameHelper.AddNameFunction(
                _descriptor, _createName, typeof(TDependency));
            return _descriptor;
        }

        public IInputUnionTypeDescriptor DependsOn(Type schemaType)
        {
            TypeNameHelper.AddNameFunction(
                _descriptor, _createName, schemaType);
            return _descriptor;
        }
    }
}
