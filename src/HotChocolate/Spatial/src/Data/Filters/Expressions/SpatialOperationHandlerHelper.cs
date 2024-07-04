using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters.Spatial;

public static class SpatialOperationHandlerHelper
{
    public static bool TryGetParameter<T>(
        IFilterField parentField,
        IValueNode node,
        string fieldName,
        InputParser inputParser,
        [NotNullWhen(true)] out T fieldNode)
    {
        if (parentField.Type is InputObjectType inputType &&
            node is ObjectValueNode objectValueNode)
        {
            for (var i = 0; i < objectValueNode.Fields.Count; i++)
            {
                if (objectValueNode.Fields[i].Name.Value == fieldName)
                {
                    var fieldValue = objectValueNode.Fields[i];
                    IInputField field = inputType.Fields[fieldName];

                    if (inputParser.ParseLiteral(fieldValue.Value, field) is T val)
                    {
                        fieldNode = val;
                        return true;
                    }

                    fieldNode = default!;
                    return false;
                }
            }
        }

        fieldNode = default!;
        return false;
    }
}
