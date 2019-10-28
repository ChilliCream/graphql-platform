using System;
using System.Collections.Generic;
using System.Globalization;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Execution
{
    internal class DictionaryToObjectValueConverter
        : DictionaryVisitor<ConverterContext>
    {
        public IValueNode Convert(object from, IInputType type, VariableDefinitionNode variable)
        {
            if (from == null)
            {
                throw new ArgumentNullException(nameof(from));
            }

            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (variable is null)
            {
                throw new ArgumentNullException(nameof(variable));
            }

            var context = new ConverterContext
            {
                InputType = type,
                Node = variable,
                Name = "$" + variable.Variable.Name.Value
            };
            Visit(from, context);
            return (IValueNode)context.Object;
        }

        protected override void VisitObject(
            IDictionary<string, object> dictionary,
            ConverterContext context)
        {
            if (!context.InputType.IsInputObjectType())
            {
                throw new QueryException(ErrorBuilder.New()
                    .SetMessage($"The value of {context.Name} has a wrong structure.")
                    .SetCode(ErrorCodes.Execution.InvalidType)
                    .AddLocation(context.Node)
                    .Build());
            }

            IInputType originalType = context.InputType;
            InputObjectType type = (InputObjectType)context.InputType.NamedType();

            var fields = new ObjectFieldNode[dictionary.Count];
            int i = 0;

            foreach (KeyValuePair<string, object> field in dictionary)
            {
                if (!type.Fields.TryGetField(field.Key, out InputField f))
                {
                    throw new QueryException(ErrorBuilder.New()
                        .SetMessage($"The value of {context.Name} has a wrong structure.")
                        .SetCode(ErrorCodes.Execution.InvalidType)
                        .AddLocation(context.Node)
                        .Build());
                }

                context.InputType = f.Type;
                context.Object = null;

                VisitField(field, context);

                fields[i++] = new ObjectFieldNode(field.Key, (IValueNode)context.Object);
            }

            context.InputType = originalType;
            context.Object = new ObjectValueNode(fields);
        }

        protected override void VisitField(
            KeyValuePair<string, object> field,
            ConverterContext context)
        {
            Visit(field.Value, context);
        }

        protected override void VisitList(
            IList<object> list,
            ConverterContext context)
        {
            if (!context.InputType.IsListType())
            {
                throw new QueryException(ErrorBuilder.New()
                    .SetMessage($"The value of {context.Name} has a wrong structure.")
                    .SetCode(ErrorCodes.Execution.InvalidType)
                    .AddLocation(context.Node)
                    .Build());
            }

            IInputType originalType = context.InputType;
            ListType listType = context.InputType.ListType();
            context.InputType = (IInputType)listType.ElementType;

            var items = new IValueNode[list.Count];

            for (int i = 0; i < list.Count; i++)
            {
                context.Object = null;
                Visit(list[i], context);
                items[i] = (IValueNode)context.Object;
            }

            context.Object = new ListValueNode(items);
            context.InputType = originalType;
        }

        protected override void VisitValue(
            object value,
            ConverterContext context)
        {
            try
            {
                context.Object = context.InputType.ParseValue(
                    context.InputType.Deserialize(value));
            }
            catch
            {
                throw new QueryException(ErrorBuilder.New()
                    .SetMessage(string.Format(
                        CultureInfo.InvariantCulture,
                        TypeResources.VariableValueBuilder_InvalidValue,
                        context.Name))
                    .SetCode(ErrorCodes.Execution.InvalidType)
                    .AddLocation(context.Node)
                    .Build());
            }
        }

        public static DictionaryToObjectValueConverter Default { get; } =
            new DictionaryToObjectValueConverter();
    }
}
