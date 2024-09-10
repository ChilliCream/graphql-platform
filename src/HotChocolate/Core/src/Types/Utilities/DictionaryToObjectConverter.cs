using System.Collections;
using HotChocolate.Internal;

namespace HotChocolate.Utilities;

public sealed class DictionaryToObjectConverter(ITypeConverter converter)
    : DictionaryVisitor<ConverterContext>
{
    private readonly ITypeConverter _converter = converter
        ?? throw new ArgumentNullException(nameof(converter));

    public object Convert(object from, Type to)
    {
        if (from is null)
        {
            throw new ArgumentNullException(nameof(from));
        }

        if (to is null)
        {
            throw new ArgumentNullException(nameof(to));
        }

        var context = new ConverterContext { ClrType = to, };
        Visit(from, context);
        return context.Object;
    }

    protected override void VisitObject(
        IReadOnlyDictionary<string, object> dictionary,
        ConverterContext context)
    {
        if (!context.ClrType.IsValueType &&
            context.ClrType != typeof(string))
        {
            var properties =
                context.ClrType.CreatePropertyLookup();

            context.Fields = properties;
            context.Object = Activator.CreateInstance(context.ClrType);

            foreach (var field in dictionary)
            {
                VisitField(field, context);
            }
        }
    }

    protected override void VisitField(
        KeyValuePair<string, object> field,
        ConverterContext context)
    {
        var property = context.Fields[field.Key].FirstOrDefault();
        if (property != null)
        {
            var valueContext = new ConverterContext();
            valueContext.ClrType = property.PropertyType;
            Visit(field.Value, valueContext);
            property.SetValue(context.Object, valueContext.Object);
        }
    }

    protected override void VisitList(
        IReadOnlyList<object> list,
        ConverterContext context)
    {
        var elementType = ExtendedType.Tools.GetElementType(context.ClrType);

        if (elementType is not null)
        {
            var listType = typeof(List<>).MakeGenericType(elementType);
            var temp = (IList)Activator.CreateInstance(listType);

            for (var i = 0; i < list.Count; i++)
            {
                var valueContext = new ConverterContext { ClrType = elementType, };
                Visit(list[i], valueContext);
                temp!.Add(valueContext.Object);
            }

            context.Object = context.ClrType.IsAssignableFrom(listType)
                ? temp
                : _converter.Convert(listType, context.ClrType, temp);
        }
    }

    protected override void VisitValue(
        object value,
        ConverterContext context)
    {
        context.Object = _converter.Convert(typeof(object), context.ClrType, value);
    }
}
