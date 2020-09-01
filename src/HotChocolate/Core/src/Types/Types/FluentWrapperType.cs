using System;
using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Types
{
    public class FluentWrapperType
        : IOutputType
        , IInputType
    {
        protected FluentWrapperType() { }

        Type IHasRuntimeType.RuntimeType => throw new NotSupportedException();

        TypeKind IType.Kind => throw new NotSupportedException();

        object? ISerializableType.Serialize(object? runtimeValue) =>
            throw new NotSupportedException();

        object? ISerializableType.Deserialize(object? resultValue) =>
            throw new NotSupportedException();

        bool ISerializableType.TryDeserialize(object? resultValue, out object? runtimeValue) =>
            throw new NotSupportedException();

        bool IParsableType.IsInstanceOfType(IValueNode literal) =>
            throw new NotSupportedException();

        bool IParsableType.IsInstanceOfType(object? value) =>
            throw new NotSupportedException();

        object IParsableType.ParseLiteral(IValueNode literal, bool withDefaults) =>
            throw new NotSupportedException();

        IValueNode IParsableType.ParseValue(object? value) =>
            throw new NotSupportedException();

        public IValueNode ParseResult(object? resultValue) =>
            throw new NotSupportedException();
    }
}
