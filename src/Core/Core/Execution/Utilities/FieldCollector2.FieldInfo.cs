using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal sealed partial class FieldCollector2
    {
        private class FieldInfo
        {
            public string ResponseName { get; set; }

            public ObjectField Field { get; set; }

            public FieldNode Selection { get; set; }

            public List<FieldNode> Nodes { get; set; }

            public FieldDelegate Middleware { get; set; }

            public Dictionary<NameString, object> Arguments { get; set; }

            public Dictionary<NameString, object> VarArguments { get; set; }

            public List<FieldVisibility> Visibilities { get; set; }
        }

        public class FieldVisibility
        {
            public FieldVisibility(
                IValueNode skip,
                IValueNode include,
                FieldVisibility parent)
            {
                Skip = skip;
                Include = include;
                Parent = parent;
            }

            public IValueNode Skip { get; }

            public IValueNode Include { get; }

            public FieldVisibility Parent { get; }

            public bool IsVisible(IVariableCollection variables)
            {
                if (Skip != null && IsTrue(variables, Skip))
                {
                    return false;
                }
                return Include == null || IsTrue(variables, Include);
            }

            private bool IsTrue(
                IVariableCollection variables,
                IValueNode value)
            {
                if (value is BooleanValueNode b)
                {
                    return b.Value;
                }

                if (value is VariableNode v)
                {
                    return variables.GetVariable<bool>(v.Name.Value);
                }

                throw new QueryException(
                    ErrorBuilder.New()
                        .SetMessage(
                            "The skip/include if-argument " +
                            "value has to be a 'Boolean'.")
                        .AddLocation(value)
                        .Build());
            }
        }
    }
}
