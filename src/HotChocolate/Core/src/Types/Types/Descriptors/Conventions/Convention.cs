using System;
using System.Diagnostics;

#nullable enable

namespace HotChocolate.Types.Descriptors
{
    public abstract class Convention : IConvention
    {
        private string? _scope;

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
                        "The convention scope is immutable.");
                }
                _scope = value;
            }
        }

        protected bool IsInitialized { get; private set; }

        protected internal virtual void Initialize(IConventionContext context)
        {
            MarkInitialized();
        }

        protected void MarkInitialized()
        {
            Debug.Assert(!IsInitialized);

            if (IsInitialized)
            {
                throw new InvalidOperationException();
            }

            IsInitialized = true;
        }
    }
}
