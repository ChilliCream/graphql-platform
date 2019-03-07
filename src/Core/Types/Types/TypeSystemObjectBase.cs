using System.Reflection.Metadata;
using System;
using System.Collections.Generic;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public class TypeSystemObjectBase
        : ITypeSystemObject
    {
        private TypeStatus _status;
        private NameString _name;
        private string _description;

        protected TypeSystemObjectBase() { }

        public NameString Name
        {
            get => _name;
            protected set
            {
                if (IsNamed)
                {
                    // TODO : exception type
                    // TODO : resources
                    throw new InvalidOperationException(
                        "The name cannot be changed after the " +
                        "name was completed.");
                }

                _name = value.EnsureNotEmpty(nameof(value));
            }
        }

        public string Description
        {
            get => _description;
            protected set
            {
                if (IsCompleted)
                {
                    // TODO : exception type
                    // TODO : resources
                    throw new InvalidOperationException(
                        "The type is completed and cannot changed.");
                }
                _description = value;
            }
        }

        protected bool IsCompleted =>
            _status == TypeStatus.Completed;

        protected bool IsNamed =>
            _status == TypeStatus.Named
            || _status == TypeStatus.Completed;

        protected bool IsInitialized =>
            _status == TypeStatus.Initialized
            || _status == TypeStatus.Named
            || _status == TypeStatus.Completed;


        internal void Initialize(IInitializationContext context)
        {
            OnInitialize(context);
            _status = TypeStatus.Initialized;
        }

        protected virtual void OnInitialize(IInitializationContext context)
        {
        }

        internal void CompleteName(ICompletionContext context)
        {
            OnCompleteName(context);
            _status = TypeStatus.Named;
        }

        protected virtual void OnCompleteName(ICompletionContext context)
        {
        }

        internal void CompleteObject(ICompletionContext context)
        {
            OnCompleteObject(context);
            _status = TypeStatus.Completed;
        }

        protected virtual void OnCompleteObject(ICompletionContext context)
        {
        }

        private enum TypeStatus
        {
            Initializing,
            Initialized,
            Named,
            Completed
        }
    }
}
