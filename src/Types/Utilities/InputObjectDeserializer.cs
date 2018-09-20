using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Utilities
{
    internal static class InputObjectDeserializer
    {
        public static object ParseLiteral(
            InputObjectType inputObjectType,
            ObjectValueNode literal)
        {
            var fieldValues = literal.Fields
                .ToDictionary(t => t.Name.Value, t => t.Value);

            object obj = Activator.CreateInstance(inputObjectType.ClrType);

            foreach (InputField field in inputObjectType.Fields)
            {
                ValueDeserializer.SetProperty(
                    field, fieldValues, obj, field.Property,
                    t => field.Type.ParseLiteral(literal));
            }

            return obj;
        }
    }
}
