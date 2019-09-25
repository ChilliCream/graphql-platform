using HotChocolate.Language;

namespace HotChocolate.Execution
{
    internal sealed class FieldVisibility
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

        public bool IsVisible(IVariableValueCollection variables)
        {
            if (Parent != null && !Parent.IsVisible(variables))
            {
                return false;
            }

            if (Skip != null && IsTrue(variables, Skip))
            {
                return false;
            }
            return Include == null || IsTrue(variables, Include);
        }

        private static bool IsTrue(
            IVariableValueCollection variables,
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

            // TODO: Resources
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
