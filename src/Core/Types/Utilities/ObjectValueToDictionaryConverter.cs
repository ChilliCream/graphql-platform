using System;
using System.Collections.Generic;
using System.Globalization;
using HotChocolate.Language;

namespace HotChocolate.Utilities
{
    internal class ObjectValueToDictionaryConverter
        : SyntaxWalkerBase<IValueNode, Action<object>>
    {
        public Dictionary<string, object> Convert(ObjectValueNode objectValue)
        {
            if (objectValue == null)
            {
                throw new ArgumentNullException(nameof(objectValue));
            }

            Dictionary<string, object> dict = null;
            Action<object> setValue =
                value => dict = (Dictionary<string, object>)value;
            VisitObjectValue(objectValue, setValue);
            return dict;
        }

        protected override void VisitObjectValue(
            ObjectValueNode node,
            Action<object> setValue)
        {
            var obj = new Dictionary<string, object>();
            setValue(obj);

            foreach (ObjectFieldNode field in node.Fields)
            {
                Action<object> setField =
                    value => obj[field.Name.Value] = value;
                VisitValue(field.Value, setField);
            }
        }

        protected override void VisitListValue(
            ListValueNode node,
            Action<object> setValue)
        {
            var list = new List<object>();
            setValue(list);

            Action<object> addItem = item => list.Add(item);

            foreach (IValueNode value in node.Items)
            {
                VisitValue(value, addItem);
            }
        }

        protected override void VisitIntValue(
           IntValueNode node,
           Action<object> setValue)
        {
            if (int.TryParse(node.Value, NumberStyles.Integer,
                CultureInfo.InvariantCulture, out int i))
            {
                setValue(i);
            }
            else
            {
                setValue(node.Value);
            }
        }

        protected override void VisitFloatValue(
            FloatValueNode node,
            Action<object> setValue)
        {
            if (double.TryParse(node.Value, NumberStyles.Float,
                CultureInfo.InvariantCulture, out double d))
            {
                setValue(d);
            }
            else
            {
                setValue(node.Value);
            }
        }

        protected override void VisitStringValue(
            StringValueNode node,
            Action<object> setValue)
        {
            setValue(node.Value);
        }

        protected override void VisitBooleanValue(
            BooleanValueNode node,
            Action<object> setValue)
        {
            setValue(node.Value);
        }

        protected override void VisitEnumValue(
            EnumValueNode node,
            Action<object> setValue)
        {
            setValue(node.Value);
        }

        protected override void VisitNullValue(
            NullValueNode node,
            Action<object> setValue)
        {
            setValue(null);
        }
    }
}
