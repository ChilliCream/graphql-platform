// using System;
// using System.Collections.Generic;
// using System.Text;
// using System.Threading.Tasks;
// using StrawberryShake.CodeGeneration.CSharp.Builders;
//
// namespace StrawberryShake.CodeGeneration.CSharp
// {
//     public class InputModelSerializerGenerator
//         : CSharpCodeGenerator<InputModelSerializerDescriptor>
//     {
//         protected override Task WriteAsync(
//             CodeWriter writer,
//             InputModelSerializerDescriptor descriptor)
//         {
//             if (writer is null)
//             {
//                 throw new ArgumentNullException(nameof(writer));
//             }
//
//             if (descriptor is null)
//             {
//                 throw new ArgumentNullException(nameof(descriptor));
//             }
//
//             ClassBuilder classBuilder = ClassBuilder.New()
//                 .SetAccessModifier(AccessModifier.Public)
//                 .SetName(descriptor.Name)
//                 .AddImplements("global::StrawberryShake.IInputSerializer");
//
//             AddFields(descriptor.ValueSerializers, classBuilder);
//             AddProperties(descriptor, classBuilder);
//             AddInitializeMethod(descriptor.ValueSerializers, classBuilder);
//             AddSerializeMethod(descriptor, classBuilder);
//             AddDeserializeMethod(classBuilder);
//             AddTypeSerializerMethods(descriptor.TypeSerializerMethods, classBuilder);
//
//             return CodeFileBuilder.New()
//                 .SetNamespace(descriptor.Namespace)
//                 .AddType(classBuilder)
//                 .BuildAsync(writer);
//         }
//
//         private void AddFields(
//             IReadOnlyList<ValueSerializerDescriptor> serializerDescriptors,
//             ClassBuilder builder)
//         {
//             builder
//                 .AddField(FieldBuilder.New()
//                     .SetName("_needsInitialization")
//                     .SetType("bool")
//                     .SetValue("true"));
//
//             foreach (ValueSerializerDescriptor serializer in serializerDescriptors)
//             {
//                 builder
//                     .AddField(FieldBuilder.New()
//                         .SetName(serializer.FieldName)
//                         .SetType($"{Types.IValueSerializer}?", NullableRefTypes)
//                         .SetType(Types.IValueSerializer, !NullableRefTypes));
//             }
//         }
//
//         private static void AddProperties(
//             InputModelSerializerDescriptor descriptor,
//             ClassBuilder builder)
//         {
//             builder
//                 .AddProperty(PropertyBuilder.New()
//                     .SetAccessModifier(AccessModifier.Public)
//                     .SetType("string")
//                     .SetName("Name")
//                     .SetGetter(CodeLineBuilder.New()
//                         .SetLine($"return \"{descriptor.InputGraphQLTypeName}\";")))
//                 .AddProperty(PropertyBuilder.New()
//                     .SetAccessModifier(AccessModifier.Public)
//                     .SetType("global::StrawberryShake.ValueKind")
//                     .SetName("Kind")
//                     .SetGetter(CodeLineBuilder.New()
//                         .SetLine($"return global::StrawberryShake.ValueKind.InputObject;")))
//                 .AddProperty(PropertyBuilder.New()
//                     .SetAccessModifier(AccessModifier.Public)
//                     .SetType("global::System.Type")
//                     .SetName("ClrType")
//                     .SetGetter(CodeLineBuilder.New()
//                         .SetLine($"return typeof({descriptor.InputTypeName});")))
//                 .AddProperty(PropertyBuilder.New()
//                     .SetAccessModifier(AccessModifier.Public)
//                     .SetType("global::System.Type")
//                     .SetName("SerializationType")
//                     .SetGetter(CodeLineBuilder.New()
//                         .SetLine(
//                             "return typeof(global::System.Collections.Generic." +
//                             "IReadOnlyDictionary<string, object>);")));
//         }
//
//         private void AddInitializeMethod(
//             IReadOnlyList<ValueSerializerDescriptor> serializerDescriptors,
//             ClassBuilder builder)
//         {
//             builder
//                 .AddMethod(MethodBuilder.New()
//                     .SetAccessModifier(AccessModifier.Public)
//                     .SetName("Initialize")
//                     .AddParameter(ParameterBuilder.New()
//                         .SetType(Types.IValueSerializerCollection)
//                         .SetName("serializerResolver"))
//                 .AddCode(CreateInitializeBody(serializerDescriptors, CodeWriter.Indent)));
//         }
//
//         private static CodeBlockBuilder CreateInitializeBody(
//             IReadOnlyList<ValueSerializerDescriptor> serializerDescriptors,
//             string indent)
//         {
//             var body = new StringBuilder();
//
//             body.AppendLine("if (serializerResolver is null)");
//             body.AppendLine("{");
//             body.AppendLine(
//                 $"{indent}throw new global::System.ArgumentNullException(nameof(serializerResolver));");
//             body.AppendLine("}");
//             body.AppendLine();
//
//             foreach (ValueSerializerDescriptor serializer in serializerDescriptors)
//             {
//                 body.Append($"{serializer.FieldName} = ");
//                 body.AppendLine($"serializerResolver.Get(\"{serializer.Name}\")");
//             }
//
//             body.Append("_needsInitialization = false;");
//
//             return CodeBlockBuilder.FromStringBuilder(body);
//         }
//
//         private void AddSerializeMethod(
//             InputModelSerializerDescriptor descriptor,
//             ClassBuilder builder)
//         {
//             builder
//                 .AddMethod(MethodBuilder.New()
//                     .SetAccessModifier(AccessModifier.Public)
//                     .SetReturnType("object?", NullableRefTypes)
//                     .SetReturnType("object", !NullableRefTypes)
//                     .SetName("Serialize")
//                     .AddParameter(ParameterBuilder.New()
//                         .SetType("object?", NullableRefTypes)
//                         .SetType("object", !NullableRefTypes)
//                         .SetName("value"))
//                 .AddCode(CreateSerializeBody(descriptor, CodeWriter.Indent)));
//         }
//
//         private CodeBlockBuilder CreateSerializeBody(
//             InputModelSerializerDescriptor descriptor,
//             string indent)
//         {
//             var body = new StringBuilder();
//
//             AppendInitializationCheck(body, indent);
//
//             body.AppendLine("if (value is null)");
//             body.AppendLine("{");
//             body.AppendLine($"{indent}return null;");
//             body.AppendLine("}");
//             body.AppendLine();
//
//             body.AppendLine($"var input = ({descriptor.InputTypeName})value;");
//             body.Append(NullableRefTypes
//                 ? "var map = new global::System.Collections.Generic.Dictionary<string, object?>();"
//                 : "var map = new global::System.Collections.Generic.Dictionary<string, object>();");
//
//             foreach (InputFieldSerializerDescriptor field in descriptor.FieldSerializers)
//             {
//                 body.AppendLine();
//                 body.AppendLine();
//                 AppendSerializeField(field, body, indent);
//             }
//
//             body.AppendLine();
//             body.AppendLine();
//             body.AppendLine("return map;");
//
//             return CodeBlockBuilder.FromStringBuilder(body);
//         }
//
//         private static void AppendSerializeField(
//             InputFieldSerializerDescriptor descriptor,
//             StringBuilder body,
//             string indent)
//         {
//             body.AppendLine($"if (input.{descriptor.Name}.HasValue)");
//             body.AppendLine("{");
//             body.Append($"{indent}map.Add(\"{descriptor.GraphQLFieldName}\", ");
//             body.AppendLine($"{descriptor.SerializerMethodName}(input.{descriptor.Name}.Value));");
//             body.Append("}");
//         }
//
//         private void AddDeserializeMethod(ClassBuilder builder)
//         {
//             builder
//                 .AddMethod(MethodBuilder.New()
//                     .SetAccessModifier(AccessModifier.Public)
//                     .SetReturnType("object?", NullableRefTypes)
//                     .SetReturnType("object", !NullableRefTypes)
//                     .SetName("Deserialize")
//                     .AddParameter(ParameterBuilder.New()
//                         .SetType("object?", NullableRefTypes)
//                         .SetType("object", !NullableRefTypes)
//                         .SetName("serialized"))
//                 .AddCode(CreateDeserializeBody(CodeWriter.Indent)));
//         }
//
//         private static CodeBlockBuilder CreateDeserializeBody(string indent)
//         {
//             var body = new StringBuilder();
//
//             body.AppendLine("throw new NotSupportedException(");
//             body.Append($"{indent}\"Deserializing input values is not supported.\");");
//
//             return CodeBlockBuilder.FromStringBuilder(body);
//         }
//
//         private void AddTypeSerializerMethods(
//             IReadOnlyList<InputTypeSerializerMethodDescriptor> descriptors,
//             ClassBuilder builder)
//         {
//             foreach (InputTypeSerializerMethodDescriptor descriptor in descriptors)
//             {
//                 builder
//                     .AddMethod(MethodBuilder.New()
//                         .SetAccessModifier(AccessModifier.Private)
//                         .SetName(descriptor.Name)
//                         .SetReturnType("object?", NullableRefTypes)
//                         .SetReturnType("object", !NullableRefTypes)
//                         .AddParameter(ParameterBuilder.New()
//                             .SetName("value")
//                             .SetType("object?", NullableRefTypes)
//                             .SetType("object", !NullableRefTypes))
//                             .AddCode(CreateTypeSerializeBody(descriptor, CodeWriter.Indent)));
//             }
//         }
//
//         private CodeBlockBuilder CreateTypeSerializeBody(
//             InputTypeSerializerMethodDescriptor descriptor,
//             string indent)
//         {
//             var body = new StringBuilder();
//
//             if (descriptor.IsNullableType)
//             {
//                 body.AppendLine("if (value is null)");
//                 body.AppendLine("{");
//                 body.AppendLine($"{indent}return null;");
//                 body.AppendLine("}");
//                 body.AppendLine();
//             }
//
//             if (descriptor.IsListSerializer)
//             {
//                 body.AppendLine("var source = (global::System.Collections.IList)value;");
//                 body.AppendLine(NullableRefTypes
//                     ? "object?[] serialized = new object?[source.Count];"
//                     : "object[] serialized = new object[source.Count];");
//                 body.AppendLine("for(int i = 0; i < source.Count; i++)");
//                 body.AppendLine("{");
//                 body.AppendLine(
//                     $"{indent}serialized[i] = {descriptor.SerializerMethodName}(source[i]);");
//                 body.AppendLine("}");
//                 body.AppendLine();
//                 body.AppendLine("return result;");
//             }
//             else
//             {
//                 body.Append($"{descriptor.ValueSerializerFieldName}!.Serialize(value);");
//             }
//
//             return CodeBlockBuilder.FromStringBuilder(body);
//         }
//
//         private static void AppendInitializationCheck(StringBuilder body, string indent)
//         {
//             body.AppendLine("if (_needsInitialization)");
//             body.AppendLine("{");
//             body.AppendLine($"{indent}throw new global::System.InvalidOperationException(");
//             body.Append($"{indent}{indent}$\"The serializer for type `{{Name}}` ");
//             body.AppendLine("has not been initialized.\");");
//             body.AppendLine("}");
//             body.AppendLine();
//         }
//     }
// }
