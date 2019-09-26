using System.Threading.Tasks;
using StrawberryShake.Generators.Descriptors;
using StrawberryShake.Generators.Utilities;

namespace StrawberryShake.Generators.CSharp
{
    public class DependencyInjectionGenerator
        : CodeGenerator<IClientDescriptor>
    {
        protected override string CreateFileName(IClientDescriptor descriptor)
        {
            return CreateName(descriptor) + ".cs";
        }

        protected override Task WriteAsync(
            CodeWriter writer,
            IClientDescriptor descriptor,
            ITypeLookup typeLookup) =>
            WriteStaticClassAsync(writer, CreateName(descriptor), async () =>
            {
                await WriteAddSerializersAsync(writer, descriptor, typeLookup);
            });

        private async Task WriteAddSerializersAsync(
            CodeWriter writer,
            IClientDescriptor descriptor,
            ITypeLookup typeLookup)
        {
            await writer.WriteIndentedLineAsync(
                "public static IServiceCollection AddDefaultValueSerializers(");

            using (writer.IncreaseIndent())
            {
                await writer.WriteIndentedLineAsync(
                    "this IServiceCollection serviceCollection)");
            }

            await writer.WriteIndentedLineAsync("{");

            using (writer.IncreaseIndent())
            {
                await writer.WriteIndentedLineAsync(
                    "if (serviceCollection is null)");

                await writer.WriteIndentedLineAsync("{");

                using (writer.IncreaseIndent())
                {
                    await writer.WriteIndentedLineAsync(
                        "throw new ArgumentNullException(nameof(serviceCollection));");
                }

                await writer.WriteIndentedLineAsync("}");
                await writer.WriteLineAsync();

                await writer.WriteIndentedLineAsync(
                    "foreach (IValueSerializer serializer in ValueSerializers.All)");
                await writer.WriteIndentedLineAsync("{");

                using (writer.IncreaseIndent())
                {
                    await writer.WriteIndentedLineAsync(
                        "serviceCollection.AddSingleton(serializer);");
                }

                await writer.WriteIndentedLineAsync("}");
                await writer.WriteLineAsync();

                await writer.WriteIndentedLineAsync(
                    "return serviceCollection;");
            }

            await writer.WriteIndentedLineAsync("}");
        }

        private static string CreateName(IClientDescriptor descriptor) =>
            descriptor.Name + "ServiceCollectionExtensions";
    }
}
