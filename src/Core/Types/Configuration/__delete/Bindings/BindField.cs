using System;

namespace HotChocolate.Configuration
{
    internal class BindField<T>
        : IBindField<T>
        where T : class
    {
        private readonly TypeBindingInfo _bindingInfo;
        private readonly FieldBindingInfo _fieldBindingInfo;

        public BindField(
            TypeBindingInfo bindingInfo,
            FieldBindingInfo fieldBindingInfo)
        {
            _bindingInfo = bindingInfo
                ?? throw new ArgumentNullException(nameof(bindingInfo));
            _fieldBindingInfo = fieldBindingInfo
                ?? throw new ArgumentNullException(nameof(fieldBindingInfo));
        }

        public IBoundType<T> Name(NameString fieldName)
        {
            _fieldBindingInfo.Name = fieldName;
            return new BindType<T>(_bindingInfo);
        }
    }
}
