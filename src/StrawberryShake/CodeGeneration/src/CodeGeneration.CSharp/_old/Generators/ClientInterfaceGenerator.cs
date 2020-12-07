// using System;
// using System.Threading.Tasks;
// using StrawberryShake.CodeGeneration.CSharp.Builders;
//
// namespace StrawberryShake.CodeGeneration.CSharp
// {
//     public class ClientInterfaceGenerator
//         : CodeGenerator<ClientClassDescriptor>
//     {
//         protected override Task WriteAsync(
//             CodeWriter writer,
//             ClientClassDescriptor descriptor)
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
//             InterfaceBuilder interfaceBuilder = InterfaceBuilder.New()
//                 .SetAccessModifier(AccessModifier.Public)
//                 .SetName(descriptor.Name);
//
//             foreach (ClientOperationMethodDescriptor operation in descriptor.Operations)
//             {
//                 string returnType = CreateReturnType(
//                     operation.ResponseModelName,
//                     operation.IsStreamExecutor);
//
//                 InterfaceMethodBuilder methodBuilder = InterfaceMethodBuilder.New()
//                     .SetName(operation.Name + "Async")
//                     .SetReturnType(returnType);
//
//                 foreach (ClientOperationMethodParameterDescriptor parameter in operation.Parameters)
//                 {
//                     ParameterBuilder parameterBuilder = ParameterBuilder.New()
//                         .SetName(parameter.Name)
//                         .SetType(parameter.TypeName);
//
//                     if (parameter.IsOptional && parameter.Default is null)
//                     {
//                         parameterBuilder.SetDefault();
//                     }
//
//                     if (parameter.Default is { })
//                     {
//                         parameterBuilder.SetDefault(parameter.Default);
//                     }
//
//                     methodBuilder.AddParameter(parameterBuilder);
//                 }
//
//                 methodBuilder.AddParameter(
//                     ParameterBuilder.New()
//                         .SetName("cancellationToken")
//                         .SetType("global::System.Threading.CancellationToken")
//                         .SetDefault());
//
//                 interfaceBuilder.AddMethod(methodBuilder);
//
//                 methodBuilder = InterfaceMethodBuilder.New()
//                     .SetName(operation.Name + "Async")
//                     .SetReturnType(returnType);
//
//                 methodBuilder.AddParameter(
//                     ParameterBuilder.New()
//                         .SetName("operation")
//                         .SetType(operation.OperationModelName));
//
//                 methodBuilder.AddParameter(
//                     ParameterBuilder.New()
//                         .SetName("cancellationToken")
//                         .SetType("global::System.Threading.CancellationToken")
//                         .SetDefault());
//
//                 interfaceBuilder.AddMethod(methodBuilder);
//             }
//
//             return CodeFileBuilder.New()
//                 .SetNamespace(descriptor.Namespace)
//                 .AddType(interfaceBuilder)
//                 .BuildAsync(writer);
//         }
//
//         private static string CreateReturnType(string responseModelName, bool isStream)
//         {
//             if (isStream)
//             {
//                 return "global::System.Threading.Tasks.Task<" +
//                     $"global::StrawberryShale.IResponseStream<{responseModelName}>>";
//             }
//             else
//             {
//                 return "global::System.Threading.Tasks.Task<" +
//                     $"global::StrawberryShale.IOperationResult<{responseModelName}>>";
//             }
//         }
//     }
// }
