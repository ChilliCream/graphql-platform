using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HotChocolate.Language;
using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;
using static StrawberryShake.CodeGeneration.Utilities.SerializerNameUtils;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class ResultParserDescriptorFactory
        : IDescriptorFactory<ParserModel, ResultParserDescriptor>
    {
        public ResultParserDescriptor Create(
            ICSharpClientBuilderContext context,
            ParserModel model)
        {
            var temp = new StringBuilder();
            var typeNames = new HashSet<string>();
            var methodNames = new HashSet<string>();
            var parserMethods = new List<ResultParserMethodDescriptor>();
            var deserializerMethods = new List<ResultParserDeserializerMethodDescriptor>();
            var valueSerializers = new List<ValueSerializerDescriptor>();

            foreach (FieldParserModel fieldParser in model.FieldParsers)
            {
                var possibleTypes = new List<ResultTypeDescriptor>();

                foreach (OutputTypeModel possibleType in fieldParser.PossibleTypes)
                {
                    var components = new List<ResultTypeComponentDescriptor>();
                    var fields = new List<ResultFieldDescriptor>();

                    foreach (OutputFieldModel field in possibleType.Fields)
                    {
                        string? methodName;

                        if (field.Type.IsLeafType())
                        {
                            methodName = CreateDeserializerName(fieldParser.FieldType);
                            RegisterDeserializationMethod(
                                context,
                                methodNames,
                                deserializerMethods,
                                valueSerializers,
                                CreateDeserializerName(fieldParser.FieldType),
                                fieldParser.FieldType);
                        }
                        else
                        {
                            methodName = $"Parse{GetPathName(field.Path)}";
                        }

                        fields.Add(new ResultFieldDescriptor(field.Name, methodName));
                    }

                    IType possibleFieldType = RewriteType(fieldParser.FieldType, possibleType.Type);
                    DecomposeType(context, possibleFieldType, components);
                    possibleTypes.Add(new ResultTypeDescriptor(
                        context.GetFullTypeName(
                            (IOutputType)possibleFieldType,
                            fieldParser.Selection.SelectionSet),
                        possibleFieldType.NamedType().Name,
                        components,
                        fields));
                }

                var returnTypeComponents = new List<ResultTypeComponentDescriptor>();
                DecomposeType(context, fieldParser.FieldType, returnTypeComponents);

                parserMethods.Add(new ResultParserMethodDescriptor(
                    $"Parse{GetPathName(fieldParser.Path)}",
                    new ResultTypeDescriptor(
                        context.GetFullTypeName(
                        (IOutputType)fieldParser.FieldType,
                        fieldParser.Selection.SelectionSet),
                        fieldParser.FieldType.NamedType().Name,
                        returnTypeComponents,
                        Array.Empty<ResultFieldDescriptor>()),
                    possibleTypes,
                    false));
            }

            return new ResultParserDescriptor(
                model.Name,
                context.Namespace,
                context.GetFullTypeName(model.ReturnType),
                parserMethods,
                deserializerMethods,
                valueSerializers);
        }

        private static void RegisterDeserializationMethod(
            ICSharpClientBuilderContext context,
            HashSet<string> methodNames,
            List<ResultParserDeserializerMethodDescriptor> deserializerMethods,
            List<ValueSerializerDescriptor> valueSerializers,
            string deserializerName,
            IType type)
        {
            if (methodNames.Add(deserializerName))
            {
                string leafTypeName = type.NamedType().Print();
                ValueSerializerDescriptor? serializer =
                    valueSerializers.FirstOrDefault(t => t.Name == leafTypeName);
                if (serializer is null)
                {
                    serializer = new ValueSerializerDescriptor(
                        leafTypeName,
                        GetFieldName(leafTypeName, "Serializer"));
                }

                var runtimeTypeComponents = new List<ResultTypeComponentDescriptor>();
                DecomposeType(context, type, runtimeTypeComponents);

                deserializerMethods.Add(new ResultParserDeserializerMethodDescriptor(
                    deserializerName,
                    context.GetSerializationTypeName(type),
                    context.GetFullTypeName((IOutputType)type, null),
                    runtimeTypeComponents,
                    serializer));
            }
        }

        private static void DecomposeType(
            ICSharpClientBuilderContext context,
            IType type,
            ICollection<ResultTypeComponentDescriptor> components)
        {
            if (type.IsListType())
            {
                components.Add(new ResultTypeComponentDescriptor(
                    context.GetFullTypeName((IOutputType)type, null),
                    type.IsNullableType(),
                    type.IsListType(),
                    context.IsReferenceType((IOutputType)type, null)));

                DecomposeType(
                    context,
                    type.ElementType(),
                    components);
            }
            else
            {
                components.Add(new ResultTypeComponentDescriptor(
                    context.GetFullTypeName((IOutputType)type, null),
                    true,
                    type.IsListType(),
                    context.IsReferenceType((IOutputType)type, null)));
            }
        }

        private IType RewriteType(IType type, INamedType namedType)
        {
            if (type is NonNullType nnt)
            {
                return new NonNullType(RewriteType(nnt.Type, namedType));
            }
            else if (type is ListType lt)
            {
                return new ListType(RewriteType(lt.ElementType, namedType));
            }
            else
            {
                return namedType;
            }
        }
    }
}
