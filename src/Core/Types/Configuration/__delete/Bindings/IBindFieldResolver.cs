using System;
using System.Linq.Expressions;

namespace HotChocolate.Configuration
{
    public interface IBindFieldResolver<TResolver>
        : IFluent
        where TResolver : class
    {
        IBoundResolver<TResolver> With<TPropertyType>(
            Expression<Func<TResolver, TPropertyType>> resolver);
    }

    public interface IBindFieldResolver<TResolver, TObjectType>
       : IFluent
       where TResolver : class
       where TObjectType : class
    {
        IBoundResolver<TResolver, TObjectType> With<TPropertyType>(
            Expression<Func<TResolver, TPropertyType>> resolver);
    }
}
