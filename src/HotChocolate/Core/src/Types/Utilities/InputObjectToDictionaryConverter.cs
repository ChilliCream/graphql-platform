using System.Collections;
using HotChocolate.Types;

namespace HotChocolate.Utilities;

internal class InputObjectToDictionaryConverter
{
    private readonly ITypeConverter _converter;

    public InputObjectToDictionaryConverter(ITypeConverter converter)
    {
        _converter = converter
            ?? throw new ArgumentNullException(nameof(converter));
    }

    public Dictionary<string, object> Convert(
        InputObjectType type, object obj)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (obj is null)
        {
            throw new ArgumentNullException(nameof(obj));
        }

        Dictionary<string, object> dict = null;
        void SetValue(object value) => dict = (Dictionary<string, object>)value;
        VisitInputObject(type, obj, SetValue, new HashSet<object>());
        return dict;
    }

    private void VisitValue(
        IInputType type, object obj,
        Action<object> setValue,
        ISet<object> processed)
    {
        if (obj is null)
        {
            setValue(null);
        }
        else if (type.IsListType())
        {
            VisitList(type.ListType(), obj, setValue, processed);
        }
        else if (type.IsLeafType())
        {
            VisitLeaf((INamedInputType)type.NamedType(), obj, setValue);
        }
        else if (type.IsInputObjectType())
        {
            VisitInputObject((InputObjectType)type.NamedType(), obj, setValue, processed);
        }
        else
        {
            throw new NotSupportedException();
        }
    }

    private void VisitInputObject(
        InputObjectType type, object obj,
        Action<object> setValue, ISet<object> processed)
    {
        if (processed.Add(obj))
        {
            var dict = new Dictionary<string, object>();
            setValue(dict);

            var fieldValues = new object[type.Fields.Count];
            type.GetFieldValues(obj, fieldValues);

            for (var i = 0; i < type.Fields.Count; i++)
            {
                var field = type.Fields[i];
                void SetField(object value) => dict[field.Name] = value;
                VisitValue(field.Type, fieldValues[i], SetField, processed);
            }
        }
    }

    private void VisitList(
        ListType type, object obj,
        Action<object> setValue, ISet<object> processed)
    {
        if (obj is IEnumerable sourceList)
        {
            var list = new List<object>();
            setValue(list);

            var itemType = (IInputType)type.ElementType;
            void AddItem(object item) => list.Add(item);

            foreach (var item in sourceList)
            {
                VisitValue(itemType, item, AddItem, processed);
            }
        }
    }

    private void VisitLeaf(INamedInputType type, object obj, Action<object> setValue)
    {
        if (type is IHasRuntimeType hasClrType)
        {
            var currentType = obj.GetType();
            var normalized = currentType == hasClrType.RuntimeType
                ? obj
                : _converter.Convert(currentType, hasClrType.RuntimeType, obj);
            setValue(normalized);
        }
    }
}
