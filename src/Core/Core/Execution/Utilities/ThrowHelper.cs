using HotChocolate.Language;
using HotChocolate.Properties;

namespace HotChocolate.Execution.Utilities
{
    internal static class ThrowHelper
    {
        public static QueryException FieldDoesNotExistOnType(
            FieldNode selection, string typeName)
        {
            return new QueryException(ErrorBuilder.New()
                .SetMessage(
                    "Field `{0}` does not exist on type `{1}`.",
                    selection.Name.Value,
                    typeName)
                .AddLocation(selection)
                .SetExtension("fieldName", selection.Name.Value)
                .SetExtension("typeName", typeName)
                .Build());
        }

        public static QueryException FieldVisibility_ValueNotSupported(IValueNode value) =>
            new QueryException(
                ErrorBuilder.New()
                    .SetMessage("The skip/include if-argument value has to be a 'Boolean'.")
                    .AddLocation(value)
                    .Build());

        public static QueryException VariableNotFound(
            VariableNode variable) =>
            new QueryException(ErrorBuilder.New()
                .SetMessage(
                    "The variable with the name `{0}` does not exist.",
                    variable.Name.Value)
                .AddLocation(variable)
                .Build());
    }
}
