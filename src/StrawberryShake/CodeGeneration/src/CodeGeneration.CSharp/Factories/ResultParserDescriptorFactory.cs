using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HotChocolate;
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
            var deserializerMethods = new List<ResultParserDeserializerMethod>();
            var valueSerializers = new List<ValueSerializerDescriptor>();
            var parserMethods = new List<ResultParserMethodDescriptor>();

            foreach (FieldParserModel fieldParser in model.FieldParsers)
            {
                foreach (ComplexOutputTypeModel possibleType in fieldParser.PossibleTypes)
                {
                    var possibleTypes = new List<ResultTypeDescriptor>();
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

                    DecomposeType(context, possibleType.)
                }


            }

            new ResultParserDescriptor(
                model.Name,
                context.Namespace,
                context.GetFullTypeName(model.ReturnType),
                new[] {
                    new ResultParserMethodDescriptor(
                        "ParseFooBar",
                        "IBar",
                        new [] {
                            new ResultTypeDescriptor("Abc", true, true, true),
                            new ResultTypeDescriptor("Def", true, false, true)
                        },
                        false,
                        new [] {
                            new ResultFieldDescriptor("FieldA", "ParseThisAndThat")
                        }
                    ) },
                Array.Empty<ResultParserDeserializerMethod>(),
                Array.Empty<ValueSerializerDescriptor>());

            string CreateMethodName(Path path)
            {
                Path current = path;
                temp.Clear();

                while (current is { })
                {
                    temp.Insert(0, path.Name);
                }

                temp.Insert(0, "Parse");
                return temp.ToString();
            }
        }

        private static void RegisterDeserializationMethod(
            ICSharpClientBuilderContext context,
            HashSet<string> methodNames,
            List<ResultParserDeserializerMethod> deserializerMethods,
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

                var runtimeTypeComponents = new List<ResultTypeDescriptor>();
                DecomposeType(context, type, runtimeTypeComponents);

                deserializerMethods.Add(new ResultParserDeserializerMethod(
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
            ICollection<ResultTypeDescriptor> components)
        {
            if (type.IsListType())
            {
                components.Add(new ResultTypeDescriptor(
                    context.GetFullTypeName((IOutputType)type, null),
                    type.IsNullableType(),
                    type.IsListType(),
                    context.IsReferenceType((IOutputType)type, null),
                    Array.Empty<ResultFieldDescriptor>()));

                DecomposeType(
                    context,
                    type.ElementType(),
                    components);
            }
            else
            {
                components.Add(new ResultTypeDescriptor(
                    context.GetFullTypeName((IOutputType)type, null),
                    true,
                    type.IsListType(),
                    context.IsReferenceType((IOutputType)type, null),
                    Array.Empty<ResultFieldDescriptor>()));
            }
        }
    }
}
