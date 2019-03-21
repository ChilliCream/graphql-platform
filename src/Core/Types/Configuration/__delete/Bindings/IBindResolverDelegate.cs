using System;
using System.Linq.Expressions;

namespace HotChocolate.Configuration
{
    public interface IBindResolverDelegate
        : IFluent
    {
        void To(NameString typeName, NameString fieldName);
        void To<TObjectType>(Expression<Func<TObjectType, object>> resolver)
            where TObjectType : class;
    }
}
