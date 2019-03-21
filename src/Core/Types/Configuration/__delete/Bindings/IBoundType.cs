using System;
using System.Linq.Expressions;

namespace HotChocolate.Configuration
{
    public interface IBoundType<T>
       : IFluent
       where T : class
    {
        IBindField<T> Field<TPropertyType>(
            Expression<Func<T, TPropertyType>> field);
    }
}
