using System;
using System.Linq.Expressions;

namespace HotChocolate.Configuration
{
    public interface IBoundResolver<TResolver>
        : IFluent
        where TResolver : class
    {
        IBindFieldResolver<TResolver> Resolve(NameString fieldName);

    }

    public interface IBoundResolver<TResolver, TObjectType>
        : IBoundResolver<TResolver>
        where TResolver : class
        where TObjectType : class
    {
        IBindFieldResolver<TResolver> Resolve<TPropertyType>(
            Expression<Func<TObjectType, TPropertyType>> field);
    }
}
