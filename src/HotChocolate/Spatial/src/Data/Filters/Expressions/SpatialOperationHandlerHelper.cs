using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters.Spatial
{
    public static class SpatialOperationHandlerHelper
    {
        public static bool TryGetParameter<T>(
            IFilterField parentField,
            IValueNode node,
            string fieldName,
            [NotNullWhen(true)] out T fieldNode)
        {
            if (parentField.Type is InputObjectType inputType &&
                node is ObjectValueNode objectValueNode)
            {
                for (var i = 0; i < objectValueNode.Fields.Count; i++)
                {
                    if (objectValueNode.Fields[i].Name.Value == fieldName)
                    {
                        ObjectFieldNode field = objectValueNode.Fields[i];
                        if (inputType.Fields[fieldName].Type.ParseLiteral(field.Value) is T val)
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
}
