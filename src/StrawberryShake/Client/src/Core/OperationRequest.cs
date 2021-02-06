using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace StrawberryShake
{
    public sealed class OperationRequest : IEquatable<OperationRequest>
    {
        private readonly IReadOnlyDictionary<string, object?> _variables;
        private Dictionary<string, object?>? _extensions;
        private Dictionary<string, object?>? _contextData;

        public OperationRequest(
            string name,
            IDocument document,
            IReadOnlyDictionary<string, object?>? variables = null)
            : this(null, name, document, variables)
        {
        }

        public OperationRequest(
            string? id,
            string name,
            IDocument document,
            IReadOnlyDictionary<string, object?>? variables = null)
        {
            Id = id;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Document = document ?? throw new ArgumentNullException(nameof(document));
            _variables = variables ?? ImmutableDictionary<string, object?>.Empty;
        }

        public void Deconstruct(
            out string? id,
            out string name,
            out IDocument document,
            out IReadOnlyDictionary<string, object?> variables,
            out IReadOnlyDictionary<string, object?>? extensions,
            out IReadOnlyDictionary<string, object?>? contextData)
        {
            id = Id;
            name = Name;
            document = Document;
            variables = _variables;
            extensions = _extensions;
            contextData = _contextData;
        }

        /// <summary>
        /// Gets the optional request id.
        /// </summary>
        public string? Id { get; }

        /// <summary>
        /// Gets the operation name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the GraphQL query document containing this operation.
        /// </summary>
        public IDocument Document { get; }

        /// <summary>
        /// Gets the variable values.
        /// </summary>
        public IReadOnlyDictionary<string, object?> Variables => _variables;

        /// <summary>
        /// Gets the request extension values.
        /// </summary>
        public IDictionary<string, object?> Extensions
        {
            get
            {
                return _extensions ??= new();
            }
        }

        /// <summary>
        /// Gets the local context data.
        /// </summary>
        public IDictionary<string, object?> ContextData
        {
            get
            {
                return _contextData ??= new();
            }
        }

        public bool Equals(OperationRequest? other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Id == other.Id &&
               Name == other.Name &&
               Document.Equals(other.Document) &&
               EqualsVariables(other._variables);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((OperationRequest)obj);
        }

        private bool EqualsVariables(IReadOnlyDictionary<string, object?> others)
        {
            // the variables dictionary is the same or both are null.
            if (ReferenceEquals(_variables, others))
            {
                return true;
            }

            if (_variables.Count != others.Count)
            {
                return false;
            }

            foreach (var key in _variables.Keys)
            {
                if(!_variables.TryGetValue(key, out object? a) ||
                   !others.TryGetValue(key, out object? b))
                {
                    return false;
                }

                if (!Equals(a, b))
                {
                    return false;
                }
            }

            return true;
        }

         public override int GetHashCode()
         {
             unchecked
             {
                 var hash = (Id?.GetHashCode() ?? 0) * 397 ^
                    Name.GetHashCode() * 397 ^
                    Document.GetHashCode() * 397;

                 foreach (KeyValuePair<string, object?> variable in _variables)
                 {
                     hash ^= variable.GetHashCode();
                 }

                 return hash;
             }
         }
    }
}
