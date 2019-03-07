using System.Reflection.Metadata;
using System;
using System.Collections.Generic;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public class TypeSystemObjectBase<TDefinition>
        : TypeSystemObjectBase
        where TDefinition : DefinitionBase
    {
        private TDefinition _definition;

        protected TypeSystemObjectBase() { }

        protected TDefinition Definition
        {
            get => _definition;
            set
            {
                if (IsInitialized)
                {
                    // TODO : exception type
                    // TODO : resources
                    throw new InvalidOperationException(
                        "The type is initialize bla bla ...");
                }

                if (_definition != null)
                {
                    // TODO : exception type
                    // TODO : resources
                    throw new NotSupportedException(
                        "It is not allowed to change the type definition " +
                        "once it is set.");
                }

                _definition = value
                    ?? throw new ArgumentNullException(nameof(value));
            }
        }

        protected override void OnCompleteName(ICompletionContext context)
        {
            if (Definition == null)
            {
                // TODO : exception type
                // TODO : resources
                throw new InvalidOperationException(
                    "The type is initialize bla bla ...");
            }

            if (Definition.Name.IsEmpty)
            {
                // TODO : exception type
                // TODO : resources
                throw new InvalidOperationException(
                    "The type is initialize bla bla ...");
            }

            Name = Definition.Name;
        }
    }
}
