using System.Globalization;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Utilities;

public class ObjectValueToDictionaryConverter : SyntaxWalker<Action<object>>
{
    public Dictionary<string, object> Convert(ObjectValueNode objectValue)
    {
        if (objectValue is null)
        {
            throw new ArgumentNullException(nameof(objectValue));
        }

        Dictionary<string, object> dictionary = null;

        void SetValue(object value) => dictionary = (Dictionary<string, object>)value;

        Enter(objectValue, SetValue);

        return dictionary;
    }

    public List<object> Convert(ListValueNode listValue)
    {
        if (listValue is null)
        {
            throw new ArgumentNullException(nameof(listValue));
        }

        List<object> list = null;

        void SetValue(object value) => list = (List<object>)value;

        Enter(listValue, SetValue);

        return list;
    }

    protected override ISyntaxVisitorAction Enter(ObjectValueNode node, Action<object> setValue)
    {
        var dictionary = new Dictionary<string, object>();
        setValue(dictionary);

        foreach (var field in node.Fields)
        {
            void SetField(object value) => dictionary[field.Name.Value] = value;

            Enter(field.Value, SetField);
        }

        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(ListValueNode node, Action<object> setValue)
    {
        var list = new List<object>();
        setValue(list);

        void AddItem(object item) => list.Add(item);

        foreach (var value in node.Items)
        {
            Enter(value, AddItem);
        }

        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(IValueNode node, Action<object> setValue)
    {
        switch (node)
        {
            case BooleanValueNode booleanValueNode:
                setValue(booleanValueNode.Value);
                break;

            case EnumValueNode enumValueNode:
                setValue(enumValueNode.Value);
                break;

            case FloatValueNode floatValueNode:
                if (double.TryParse(floatValueNode.Value, NumberStyles.Float,
                        CultureInfo.InvariantCulture, out var d))
                {
                    setValue(d);
                }
                else
                {
                    setValue(floatValueNode.Value);
                }

                break;

            case IntValueNode intValueNode:
                if (int.TryParse(intValueNode.Value, NumberStyles.Integer,
                        CultureInfo.InvariantCulture, out var i))
                {
                    setValue(i);
                }
                else
                {
                    setValue(intValueNode.Value);
                }

                break;

            case ListValueNode listValueNode:
                Enter(listValueNode, setValue);
                break;

            case NullValueNode:
                setValue(null);
                break;

            case ObjectValueNode objectValueNode:
                Enter(objectValueNode, setValue);
                break;

            case StringValueNode stringValueNode:
                setValue(stringValueNode.Value);
                break;
        }

        return Continue;
    }
}
