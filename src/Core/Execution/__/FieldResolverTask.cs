using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal class FieldResolverTask
    {
        public FieldResolverTask(ImmutableStack<object> source,
            ObjectType objectType, FieldSelection fieldSelection,
            Path path, Dictionary<string, object> result)
        {
            Source = source;
            ObjectType = objectType;
            FieldSelection = fieldSelection;
            FieldType = fieldSelection.Field.Type;
            Path = path;
            Result = result;
        }

        public FieldResolverTask(FieldResolverTask fieldResolverTask, IType fieldType)
        {
            Source = field;
            ObjectType = objectType;
            FieldSelection = fieldSelection;
            FieldType = fieldType;
            Path = path;
            Result = result;
        }

        public ImmutableStack<object> Source { get; }
        public ObjectType ObjectType { get; }
        public FieldSelection FieldSelection { get; }
        public IType FieldType { get; }
        public Path Path { get; }
        public Dictionary<string, object> Result { get; }
        public void SetValue(object value)
        {
            Result[FieldSelection.ResponseName] = value;
        }

        public FieldResolverTask WithFieldType(IType fieldType)
        {
            if (fieldType == null)
            {
                throw new ArgumentNullException(nameof(fieldType));
            }

            return new FieldResolverTask(fieldType);
        }
    }
}
