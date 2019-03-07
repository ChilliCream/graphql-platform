using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Introspection;

namespace HotChocolate.Types
{
    public class ObjectType<T>
        : ObjectType
    {
        public ObjectType()
        {
            ClrType = typeof(T);
        }

        public ObjectType(Action<IObjectTypeDescriptor<T>> configure)
            : base(d => configure((IObjectTypeDescriptor<T>)d))
        {
            ClrType = typeof(T);
        }

        #region Configuration

        internal sealed override ObjectTypeDescriptor CreateDescriptor() =>
            new ObjectTypeDescriptor<T>();

        protected sealed override void Configure(
            IObjectTypeDescriptor descriptor)
        {
            Configure((IObjectTypeDescriptor<T>)descriptor);
        }

        protected virtual void Configure(IObjectTypeDescriptor<T> descriptor)
        {

        }

        #endregion
    }
}
