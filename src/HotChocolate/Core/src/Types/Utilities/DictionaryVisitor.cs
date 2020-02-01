using System.Collections.Generic;

namespace HotChocolate.Utilities
{
    internal class DictionaryVisitor<TContext>
    {
        protected DictionaryVisitor()
        {
        }

        public virtual void Visit(object value, TContext context)
        {
            if (value is IDictionary<string, object> dictionary)
            {
                VisitObject(dictionary, context);
            }
            else if (value is IList<object> list)
            {
                VisitList(list, context);
            }
            else
            {
                VisitValue(value, context);
            }
        }

        protected virtual void VisitObject(
            IDictionary<string, object> dictionary,
            TContext context)
        {
            foreach (KeyValuePair<string, object> field in dictionary)
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

        protected virtual void VisitList(IList<object> list, TContext context)
        {
            for (int i = 0; i < list.Count; i++)
            {
                Visit(list[i], context);
            }
        }

        protected virtual void VisitValue(object value, TContext context)
        {
        }
    }
}
