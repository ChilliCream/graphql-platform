using System;

namespace HotChocolate.Client.Core
{
    public struct Arg<T> : IArg
    {
        bool isNullableVariable;

        public Arg(T value)
        {
            VariableName = null;
            Value = value;
            isNullableVariable = false;
        }

        public Arg(string variableName, bool isNullable)
        {
            VariableName = variableName;
            Value = default(T);
            isNullableVariable = isNullable;
        }

        public string VariableName { get; }
        public T Value { get; }
        bool IArg.IsNullableVariable => isNullableVariable;
        Type IArg.Type => typeof(T);
        object IArg.Value => Value;

        public static implicit operator Arg<T>(T value)
        {
            return new Arg<T>(value);
        }

        public static implicit operator Arg<T>(Variable variable)
        {
            return new Arg<T>(variable.Name, false);
        }

        public static implicit operator Arg<T>?(Variable variable)
        {
            return new Arg<T>(variable.Name, true);
        }
    }
}
