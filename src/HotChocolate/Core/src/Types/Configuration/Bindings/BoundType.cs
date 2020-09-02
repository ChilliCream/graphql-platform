using System;
using System.Linq.Expressions;
using HotChocolate.Utilities;

namespace HotChocolate.Configuration.Bindings
{
    internal class BoundType<T>
        : IBoundType<T>
        where T : class
    {
        private readonly IComplexTypeBindingBuilder _typeBuilder;

        public BoundType(IComplexTypeBindingBuilder typeBuilder)
        {
            _typeBuilder = typeBuilder
                ?? throw new ArgumentNullException(nameof(typeBuilder));
        }

        public IBindField<T> Field<TPropertyType>(
            Expression<Func<T, TPropertyType>> field)
        {
            if (field is null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            IComplexTypeFieldBindingBuilder fieldBuilder =
                ComplexTypeFieldBindingBuilder.New()
                    .SetMember(field.ExtractMember());
            _typeBuilder.AddField(fieldBuilder);
            return new BindField<T>(_typeBuilder, fieldBuilder);
        }
    }
}
