
using System;
using HotChocolate.Types;

namespace HotChocolate.Execution.Batching
{
    public sealed class ExportedVariable
    {
        public ExportedVariable(string name, IType type, object? value)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Value = value;

        }

        public string Name { get; }

        public IType Type { get; }

        public object? Value { get; }
    }
}
