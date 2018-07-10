using System;
using System.Linq;
using System.Reflection;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal static class InputObjectDeserializerFactory
    {
        public static Func<ObjectValueNode, object> Create(
           ITypeInitializationContext context,
           InputObjectType inputObjectType,
           Type nativeType)
        {
            Func<ObjectValueNode, object> _deserialize;
            if (!TryCreateNativeTypeParserDeserializer(
                    context, inputObjectType, nativeType, out _deserialize)
                && !TryCreateNativeConstructorDeserializer(
                    nativeType, out _deserialize)
                && !TryCreateNativeReflectionDeserializer(
                    inputObjectType, nativeType, out _deserialize))
            {
                context.ReportError(new SchemaError(
                    "Could not create a literal parser for input " +
                    $"object type `{inputObjectType.Name}`", inputObjectType));
            }
            return _deserialize;
        }

        private static bool TryCreateNativeTypeParserDeserializer(
            ITypeInitializationContext context,
            InputObjectType inputObjectType,
            Type nativeType,
            out Func<ObjectValueNode, object> deserializer)
        {
            if (nativeType.IsDefined(typeof(GraphQLLiteralParserAttribute)))
            {
                Type parserType = nativeType
                    .GetCustomAttribute<GraphQLLiteralParserAttribute>().Type;
                if (typeof(ILiteralParser).IsAssignableFrom(parserType))
                {
                    var parser = (ILiteralParser)Activator
                        .CreateInstance(parserType);
                    deserializer = parser.ParseLiteral;
                    return true;
                }
                else
                {
                    context.ReportError(new SchemaError(
                        "A literal parser has to implement `ILiteralParser`.",
                        inputObjectType));
                }
            }

            deserializer = null;
            return false;
        }

        private static bool TryCreateNativeConstructorDeserializer(
            Type nativeType,
            out Func<ObjectValueNode, object> deserializer)
        {
            ConstructorInfo nativeTypeConstructor =
                nativeType.GetConstructors(
                    BindingFlags.Public | BindingFlags.NonPublic)
                .Where(t => t.GetParameters().Length == 1)
                .FirstOrDefault(t => t.GetParameters()
                    .First().ParameterType == nativeType);

            if (nativeTypeConstructor != null)
            {
                deserializer = literal => nativeTypeConstructor
                    .Invoke(new object[] { literal });
                return true;
            }

            deserializer = null;
            return false;
        }

        private static bool TryCreateNativeReflectionDeserializer(
            InputObjectType inputObjectType,
            Type nativeType,
            out Func<ObjectValueNode, object> deserializer)
        {
            ConstructorInfo nativeTypeConstructor =
                nativeType.GetConstructors()
                .FirstOrDefault(t => t.GetParameters().Length == 0);
            if (nativeTypeConstructor != null)
            {
                deserializer = literal => InputObjectDefaultDeserializer
                    .ParseLiteral(inputObjectType, literal);
                return true;
            }

            deserializer = null;
            return false;
        }
    }
}
