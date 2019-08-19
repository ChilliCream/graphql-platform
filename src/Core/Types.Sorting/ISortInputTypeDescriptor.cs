using System;
using System.Linq.Expressions;

namespace HotChocolate.Types.Sorting
{
    public interface ISortInputTypeDescriptor<T>
        : IDescriptor<SortInputTypeDefinition>
        , IFluent
    {
        ISortInputTypeDescriptor<T> BindFields(
            BindingBehavior behavior);

        ISortInputTypeDescriptor<T> BindFieldsImplicitly();

        ISortInputTypeDescriptor<T> BindFieldsExplicitly();

        ISortFieldDescriptor SortField(
            Expression<Func<T, IComparable>> property);
    }
}
