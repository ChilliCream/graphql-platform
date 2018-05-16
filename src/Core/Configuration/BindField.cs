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
            if (bindingInfo == null)
            {
                throw new ArgumentNullException(nameof(bindingInfo));
            }

            if (fieldBindingInfo == null)
            {
                throw new ArgumentNullException(nameof(fieldBindingInfo));
            }

            _bindingInfo = bindingInfo;
            _fieldBindingInfo = fieldBindingInfo;
        }

        public IBoundType<T> Name(string fieldName)
        {
            _fieldBindingInfo.Name = fieldName;
            return new BindType<T>(_bindingInfo);
        }
    }
}
