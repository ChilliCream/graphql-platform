using System;
using System.Linq.Expressions;

namespace HotChocolate.Configuration.Bindings
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
        IBindFieldResolver<TResolver, TObjectType> Resolve<TPropertyType>(
            Expression<Func<TObjectType, TPropertyType>> field);
    }
}
