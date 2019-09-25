using System;
using System.Collections;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Utilities;

namespace HotChocolate.Types
{
    public class ListType
        : NonNamedType
        , INullableType
    {
        public ListType(IType elementType)
            : base(elementType)
        {
            if (elementType.IsListType())
            {
                // TODO : resources
                throw new ArgumentException(
                    "It is not possible to put a list type into list type.",
                    nameof(elementType));
            }
        }

        public override TypeKind Kind => TypeKind.List;

        public IType ElementType => InnerType;

        protected sealed override bool IsInstanceOfType(IValueNode literal)
        {
            if (literal is NullValueNode)
            {
                return true;
            }

            if (InnerInputType.IsInstanceOfType(literal))
            {
                return true;
            }

            if (literal is ListValueNode listValueLiteral)
            {
                foreach (IValueNode element in listValueLiteral.Items)
                {
                    if (!InnerInputType.IsInstanceOfType(element))
                    {
                        return false;
                    }
                }

                return true;
            }
            return false;
        }

        protected sealed override object ParseLiteral(IValueNode literal)
        {
            if (literal is NullValueNode)
            {
                return null;
            }

            if (InnerInputType.IsInstanceOfType(literal))
            {
                return CreateList(new ListValueNode(literal));
            }

            if (literal is ListValueNode listValueLiteral)
            {
                return CreateList(listValueLiteral);
            }

            // TODO : resources
            throw new ArgumentException(
                "The specified literal cannot be handled by this list type.");
        }

        protected sealed override bool IsInstanceOfType(object value)
        {
            if (value is null)
            {
                return true;
            }

            if (ClrType.IsInstanceOfType(value))
            {
                return true;
            }

            Type elementType = DotNetTypeInfoFactory
                .GetInnerListType(value.GetType());

            if (elementType is null)
            {
                return false;
            }

            Type clrType = InnerClrType;

            if (elementType == typeof(object))
            {
                return value is IList l
                    && (l.Count == 0 || clrType == l[0]?.GetType());
            }

            return elementType == clrType;
        }

        protected sealed override IValueNode ParseValue(object value)
        {
            if (value == null)
            {
                return NullValueNode.Default;
            }

            if (value is IList l)
            {
                var items = new List<IValueNode>();

                for (int i = 0; i < l.Count; i++)
                {
                    items.Add(InnerInputType.ParseValue(l[i]));
                }

                return new ListValueNode(null, items);
            }

            // TODO : resources
            throw new ScalarSerializationException(
                TypeResourceHelper.Scalar_Cannot_ParseValue(
                    this.Visualize(), value.GetType()));
        }

        protected sealed override bool TrySerialize(
            object value, out object serialized)
        {
            if (value is null)
            {
                serialized = null;
                return true;
            }

            if (value is IList l)
            {
                var list = new List<object>();

                for (int i = 0; i < l.Count; i++)
                {
                    list.Add(InnerInputType.Serialize(l[i]));
                }

                serialized = list;
                return true;
            }

            serialized = null;
            return false;
        }

        protected sealed override bool TryDeserialize(
            object serialized, out object value)
        {
            if (serialized is null)
            {
                value = null;
                return true;
            }

            if (serialized is IList l)
            {
                var list = (IList)Activator.CreateInstance(ClrType);

                for (int i = 0; i < l.Count; i++)
                {
                    if (InnerInputType.TryDeserialize(l[i], out var v))
                    {
                        list.Add(v);
                    }
                    else
                    {
                        value = null;
                        return false;
                    }
                }

                value = list;
                return true;
            }

            value = null;
            return false;
        }

        private object CreateList(ListValueNode listLiteral)
        {
            var list = (IList)Activator.CreateInstance(ClrType);

            for (var i = 0; i < listLiteral.Items.Count; i++)
            {
                object element = InnerInputType.ParseLiteral(
                    listLiteral.Items[i]);
                list.Add(element);
            }

            return list;
        }
    }
}
