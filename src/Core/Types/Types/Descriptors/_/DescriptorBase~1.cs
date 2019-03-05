using System;
using HotChocolate.Types.Descriptors.Definitions;
using System.Collections.Generic;

namespace HotChocolate.Types.Descriptors
{
    public abstract class DescriptorBase<T>
        : IDescriptor<T>
        , IDefinitionFactory<T>
        where T : DefinitionBase
    {
        private readonly List<Action<T>> _modifiers = new List<Action<T>>();

        protected abstract T Definition { get; }

        public void Configure(Action<T> definitionModifier)
        {
            if (definitionModifier == null)
            {
                throw new ArgumentNullException(nameof(definitionModifier));
            }
            _modifiers.Add(definitionModifier);
        }

        public T CreateDefinition()
        {
            OnCreateDefinition(Definition);

            foreach (Action<T> modifier in _modifiers)
            {
                modifier(Definition);
            }

            return Definition;
        }

        public virtual void OnCreateDefinition(T definition)
        {
        }

        DefinitionBase IDefinitionFactory.CreateDefinition() =>
            CreateDefinition();
    }
}
