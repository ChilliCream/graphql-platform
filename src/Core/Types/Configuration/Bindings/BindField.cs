using System;

namespace HotChocolate.Configuration.Bindings
{
    internal class BindField<T>
        : IBindField<T>
        where T : class
    {
        private readonly IComplexTypeBindingBuilder _typeBuilder;
        private readonly IComplexTypeFieldBindingBuilder _fieldBuilder;

        public BindField(
            IComplexTypeBindingBuilder typeBuilder,
            IComplexTypeFieldBindingBuilder fieldBuilder)
        {
            _typeBuilder = typeBuilder
                ?? throw new ArgumentNullException(nameof(typeBuilder));
            _fieldBuilder = fieldBuilder
                ?? throw new ArgumentNullException(nameof(fieldBuilder));
        }

        public IBoundType<T> Name(NameString fieldName)
        {
            _fieldBuilder.SetName(fieldName);
            return new BoundType<T>(_typeBuilder);
        }
    }
}
