using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    public interface INamedType
        : IType
        , INullableType
    {
        string Name { get; }
        string Description { get; }
    }
    public interface IComplexOutputType
        : INamedOutputType
    {
        IFieldCollection<IOutputField> Fields { get; }
    }

    public interface IFieldCollection<T>
        : IEnumerable<T>
        where T : IField
    {
        T this[string fieldName] { get; }

        bool ContainsField(string fieldName);

        bool TryGetField(string fieldName, out T field);
    }

    public class FieldCollection<T>
        : IFieldCollection<T>
        where T : IField
    {
        private readonly Dictionary<string, T> _fields;

        public FieldCollection(IEnumerable<T> fields)
        {
            if (fields == null)
            {
                throw new ArgumentNullException(nameof(fields));
            }

            _fields = fields.ToDictionary(t => t.Name);
        }

        public T this[string fieldName] => _fields[fieldName];

        public bool ContainsField(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName))
            {
                throw new ArgumentException(
                    "A field name must at least consist of one letter.",
                    nameof(fieldName));
            }

            return _fields.ContainsKey(fieldName);
        }

        public bool TryGetField(string fieldName, out T field)
        {
            if (string.IsNullOrEmpty(fieldName))
            {
                throw new ArgumentException(
                    "A field name must at least consist of one letter.",
                    nameof(fieldName));
            }

            return _fields.TryGetValue(fieldName, out field);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _fields.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public interface IOutputField
       : IField
    {
        bool IsDeprecated { get; }

        string DeprecationReason { get; }

        IOutputType Type { get; }

        IFieldCollection<IInputField> Arguments { get; }
    }

    public interface IObjectTypeField
       : IOutputField
    {
        FieldResolverDelegate Resolver { get; }
    }


    /// <summary>
    /// Represents an input field. Input fields can be arguments of fields
    /// or fields of an input objects.
    /// </summary>
    public interface IInputField
        : IField
    {
        /// <summary>
        /// Gets the type of this input field.
        /// </summary>
        IInputType Type { get; }

        /// <summary>
        /// Gets the default value literal of this field.
        /// </summary>
        IValueNode DefaultValue { get; }
    }
}
