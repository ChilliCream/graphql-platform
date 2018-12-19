using System;
using System.Linq.Expressions;
using HotChocolate.Utilities;

namespace HotChocolate.Configuration
{
    internal class BindType<T>
        : IBindType<T>
        where T : class
    {
        private readonly TypeBindingInfo _bindingInfo;

        public BindType(TypeBindingInfo bindingInfo)
        {
            if (bindingInfo == null)
            {
                throw new ArgumentNullException(nameof(bindingInfo));
            }

            _bindingInfo = bindingInfo;
        }

        public IBindField<T> Field<TPropertyType>(
            Expression<Func<T, TPropertyType>> field)
        {
            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            FieldBindingInfo fieldBindingInfo = new FieldBindingInfo
            {
                Member = field.ExtractMember()
            };
            _bindingInfo.Fields.Add(fieldBindingInfo);
            return new BindField<T>(_bindingInfo, fieldBindingInfo);
        }

        public IBoundType<T> To(NameString typeName)
        {

            _bindingInfo.Name = typeName.EnsureNotEmpty(nameof(typeName));
            return this;
        }
    }
}
