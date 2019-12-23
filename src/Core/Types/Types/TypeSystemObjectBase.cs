using System;
using System.Collections.Generic;
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
        private string? _description;

        protected TypeSystemObjectBase() { }

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

        protected bool IsCompleted =>
            _status == TypeStatus.Completed;

        protected bool IsNamed =>
            _status == TypeStatus.Named
            || _status == TypeStatus.Completed;

        protected bool IsInitialized =>
            _status == TypeStatus.Initialized
            || _status == TypeStatus.Named
            || _status == TypeStatus.Completed;

        internal virtual void Initialize(IInitializationContext context)
        {
            _status = TypeStatus.Initialized;
        }

        internal virtual void CompleteName(ICompletionContext context)
        {
            _status = TypeStatus.Named;
        }

        internal virtual void CompleteType(ICompletionContext context)
        {
            _status = TypeStatus.Completed;
        }
    }
}
