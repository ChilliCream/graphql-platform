// using System;
// using System.Text;
// using System.Threading.Tasks;
// using StrawberryShake.CodeGeneration.CSharp.Builders;
//
// namespace StrawberryShake.CodeGeneration.CSharp
// {
//     public class DocumentGenerator
//         : CodeGenerator<DocumentDescriptor>
//     {
//         protected override Task WriteAsync(
//             CodeWriter writer,
//             DocumentDescriptor descriptor)
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
//             ClassBuilder classBuilder =
//                 ClassBuilder.New()
//                     .SetAccessModifier(AccessModifier.Public)
//                     .SetName(descriptor.Name)
//                     .AddImplements("global::StrawberryShake.IDocument");
//
//             AddArrayProperty(
//                 classBuilder,
//                 "HashName",
//                 "_hashName",
//                 descriptor.HashAlgorithm,
//                 CodeWriter.Indent);
//
//             AddArrayProperty(
//                 classBuilder,
//                 "Hash",
//                 "_hash",
//                 descriptor.Hash,
//                 CodeWriter.Indent);
//
//             AddArrayProperty(
//                 classBuilder,
//                 "Content",
//                 "_content",
//                 descriptor.Document,
//                 CodeWriter.Indent);
//
//             AddToStringMethod(
//                 classBuilder,
//                 descriptor.OriginalDocument);
//
//             return CodeFileBuilder.New()
//                 .SetNamespace(descriptor.Namespace)
//                 .AddType(classBuilder)
//                 .BuildAsync(writer);
//         }
//
//         private static void AddArrayProperty(
//             ClassBuilder classBuilder,
//             string name,
//             string fieldName,
//             ReadOnlySpan<byte> bytes,
//             string indent)
//         {
//             classBuilder
//                 .AddField(FieldBuilder.New()
//                     .SetType("byte[]")
//                     .SetReadOnly()
//                     .SetName(fieldName)
//                     .SetValue(CreateNewByteArray(bytes, indent)))
//                 .AddProperty(PropertyBuilder.New()
//                     .SetAccessModifier(AccessModifier.Public)
//                     .SetType("global::System.ReadOnlySpan<byte>")
//                     .SetBackingField(fieldName)
//                     .SetName(name));
//         }
//
//         private static string CreateNewByteArray(
//             ReadOnlySpan<byte> bytes,
//             string indent)
//         {
//             var body = new StringBuilder();
//
//             body.AppendLine("new byte[]");
//             body.AppendLine("{");
//             for (int i = 0; i < bytes.Length; i++)
//             {
//                 if (i > 0)
//                 {
//                     body.AppendLine(",");
//                 }
//                 body.Append($"{indent}{bytes[i]}");
//             }
//             body.AppendLine();
//             body.Append("}");
//
//             return body.ToString();
//         }
//
//         private static void AddDefaultProperty(
//             ClassBuilder classBuilder,
//             string name)
//         {
//             classBuilder
//                 .AddField(FieldBuilder.New()
//                     .SetStatic()
//                     .SetType(name)
//                     .SetReadOnly()
//                     .SetName("_default")
//                     .SetValue($"new {name}();"))
//                 .AddProperty(PropertyBuilder.New()
//                     .SetAccessModifier(AccessModifier.Public)
//                     .SetType(name)
//                     .SetBackingField("_default")
//                     .SetName("Default"));
//         }
//
//         private static void AddToStringMethod(
//             ClassBuilder classBuilder,
//             string originalDocument)
//         {
//             classBuilder
//                 .AddMethod(MethodBuilder.New()
//                     .SetAccessModifier(AccessModifier.Public)
//                     .SetInheritance(Inheritance.Override)
//                     .SetReturnType("string")
//                     .SetName("ToString")
//                     .AddCode(CreateToStringBody(originalDocument)));
//         }
//
//         private static CodeBlockBuilder CreateToStringBody(
//             string originalDocument)
//         {
//             var body = new StringBuilder();
//
//             body.Append("return @\"");
// #pragma warning disable CA1307
//             body.Append(originalDocument
//                 .Replace("\"", "\"\"")
//                 .Replace("\r\n", "\n")
//                 .Replace("\n\r", "\n")
//                 .Replace("\r", "\n")
//                 .Replace("\n", "\n"));
// #pragma warning restore CA1307
//             body.Append("\";");
//
//             return CodeBlockBuilder.FromStringBuilder(body);
//         }
//     }
// }
