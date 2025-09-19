namespace HotChocolate.Utilities;

public class DictionaryVisitor<TContext>
{
    protected DictionaryVisitor()
    {
    }

    protected virtual void Visit(object value, TContext context)
    {
        switch (value)
        {
            case IReadOnlyDictionary<string, object> dictionary:
                VisitObject(dictionary, context);
                break;
            case IReadOnlyList<object> list:
                VisitList(list, context);
                break;
            default:
                VisitValue(value, context);
                break;
        }
    }

    protected virtual void VisitObject(
        IReadOnlyDictionary<string, object> dictionary,
        TContext context)
    {
        foreach (var field in dictionary)
        {
            VisitField(field, context);
        }
    }

    protected virtual void VisitField(
        KeyValuePair<string, object> field,
        TContext context)
    {
        Visit(field.Value, context);
    }

    protected virtual void VisitList(IReadOnlyList<object> list, TContext context)
    {
        for (var i = 0; i < list.Count; i++)
        {
            Visit(list[i], context);
        }
    }

    protected virtual void VisitValue(object value, TContext context)
    {
    }
}
