using System;
using System.Linq.Expressions;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Configuration.Bindings
{
    internal class BindType<T>
        : IBindType<T>
        where T : class
    {
        private readonly IComplexTypeBindingBuilder _typeBuilder;

        public BindType(IComplexTypeBindingBuilder typeBuilder)
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

        public IBoundType<T> To(NameString typeName)
        {
            _typeBuilder.SetName(typeName);
            return new BoundType<T>(_typeBuilder);
        }
    }
}
