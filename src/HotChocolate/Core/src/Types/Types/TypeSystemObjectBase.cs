using System;
using System.Collections.Generic;
using System.Diagnostics;
using HotChocolate.Configuration;
using HotChocolate.Properties;

#nullable enable

namespace HotChocolate.Types
{
    public abstract class TypeSystemObjectBase
        : ITypeSystemObject
    {
        private TypeStatus _status;
        private NameString _name;
        private string? _scope;
        private string? _description;

        /// <summary>
        /// Gets a scope name that was provided by an extension.
        /// </summary>
        public string? Scope
        {
            get => _scope;
            protected set
            {
                if (IsInitialized)
                {
                    throw new InvalidOperationException(
                        "The type scope is immutable.");
                }
                _scope = value;
            }
        }

        /// <summary>
        /// Gets the GraphQL type name.
        /// </summary>
        public NameString Name
        {
            get => _name;
            protected set
            {
                if (IsNamed)
                {
                    throw new InvalidOperationException(
                        TypeResources.TypeSystemObject_NameImmutable);
                }
                _name = value.EnsureNotEmpty(nameof(value));
            }
        }

        /// <summary>
        /// Gets the optional description of this scalar type.
        /// </summary>
        public string? Description
        {
            get => _description;
            protected set
            {
                if (IsCompleted)
                {
                    throw new InvalidOperationException(
                        TypeResources.TypeSystemObject_DescriptionImmutable);
                }
                _description = value;
            }
        }

        public abstract IReadOnlyDictionary<string, object?> ContextData { get; }

        protected internal bool IsInitialized =>
            _status == TypeStatus.Initialized
            || _status == TypeStatus.Named
            || _status == TypeStatus.Completed;

        protected internal bool IsNamed =>
            _status == TypeStatus.Named
            || _status == TypeStatus.Completed;

        protected internal bool IsCompleted =>
            _status == TypeStatus.Completed;

        internal virtual void Initialize(ITypeDiscoveryContext context)
        {
            MarkInitialized();
        }

        internal virtual void CompleteName(ITypeCompletionContext context)
        {
            MarkNamed();
        }

        internal virtual void CompleteType(ITypeCompletionContext context)
        {
            MarkCompleted();
        }

        protected void MarkInitialized()
        {
            Debug.Assert(_status == TypeStatus.Uninitialized);

            if (_status != TypeStatus.Uninitialized)
            {
                throw new InvalidOperationException();
            }

            _status = TypeStatus.Initialized;
        }

        protected void MarkNamed()
        {
            Debug.Assert(_status == TypeStatus.Initialized);

            if (_status != TypeStatus.Initialized)
            {
                throw new InvalidOperationException();
            }

            _status = TypeStatus.Named;
        }

        protected void MarkCompleted()
        {
            Debug.Assert(_status == TypeStatus.Named);

            if (_status != TypeStatus.Named)
            {
                throw new InvalidOperationException();
            }

            _status = TypeStatus.Completed;
        }
    }
}
