using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public class UnionType<T>
        : UnionType
    {
        public UnionType()
        {
        }

        public UnionType(Action<IUnionTypeDescriptor> configure)
            : base(configure)
        {
        }

        protected override void OnCompleteTypeSet(
            ICompletionContext context,
            UnionTypeDefinition definition,
            ISet<ObjectType> typeSet)
        {
            base.OnCompleteTypeSet(context, definition, typeSet);

            Type markerType = definition.ClrType;

            if (markerType != typeof(object))
            {
                foreach (IType type in context.GetTypes())
                {
                    if (type is ObjectType objectType
                        && objectType.ClrType != typeof(object)
                        && markerType.IsAssignableFrom(objectType.ClrType))
                    {
                        typeSet.Add(objectType);
                    }
                }
            }
        }
    }
}
