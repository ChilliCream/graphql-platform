using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HotChocolate;
using StrawberryShake.Tools.Abstractions;

namespace StrawberryShake.Tools.Commands.Export
{
    public class ExportCommandHandler : ICommandHandler<Options.Export>
    {
        private readonly IFileSystem _fileSystem;
        private readonly IConsoleOutput _console;

        public ExportCommandHandler(IFileSystem fileSystem, IConsoleOutput console)
        {
            _fileSystem = fileSystem;
            _console = console;
        }

        public async ValueTask<int> ExecuteAsync(Options.Export export)
        {
            using var activity = _console.WriteActivity("Export Schema", export.Path);

            if (!_fileSystem.FileExists(export.Assembly))
            {
                activity.WriteError(HotChocolate.ErrorBuilder.New().SetMessage("Assembly file not found", export.Assembly).Build());
                return 1;
            }

            var assembly = Assembly.LoadFile(export.Assembly);
            MethodInfo schemaBuilderMethod;
            if (export.Type is {Length: > 0})
            {
                var type = assembly.GetType(export.Type);
                if (type == null)
                {
                    activity.WriteError(HotChocolate.ErrorBuilder.New().SetMessage($"Type {type} not found in Assembly {assembly}").Build());
                    return 1;
                }

                if (export.Method is {Length: > 0})
                {
                    var method = type.GetMethod(export.Method);
                    if (method == null)
                    {
                        activity.WriteError(
                            HotChocolate.ErrorBuilder.New()
                                .SetMessage($"Method {export.Method} not found in Assembly {assembly}")
                                .Build());
                        return 1;
                    }
                    schemaBuilderMethod = method;
                }
                else
                {
                    var method = type
                        .GetMethods(BindingFlags.Public | BindingFlags.Static)
                        .SingleOrDefault(method => method.GetCustomAttribute<SchemaBuilderMethodAttribute>() != null);

                    if (method == null)
                    {
                        _console.WriteActivity($"Could not discover a {nameof(SchemaBuilderMethodAttribute)} on type {type} in Assembly {assembly}");
                        return 1;
                    }

                    schemaBuilderMethod = method;
                }
            }
            else
            {
               var schemaBuilderMethods = assembly.GetExportedTypes()
                    .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static))
                    .Where(method => method.GetCustomAttribute<SchemaBuilderMethodAttribute>() != null)
                    .ToArray();

               if (schemaBuilderMethods.Length != 0)
               {
                   activity.WriteError(HotChocolate.ErrorBuilder.New().SetMessage(
                       $"Failed to discover a ${nameof(SchemaBuilderMethodAttribute)} in Assembly ${assembly}").Build());
                   return 1;
               }

               schemaBuilderMethod = schemaBuilderMethods.Single();
            }

            var schemaBuilderMethodParameters = schemaBuilderMethod.GetParameters();
            if (schemaBuilderMethodParameters.Length != 1 || !typeof(ISchemaBuilder).IsAssignableFrom(schemaBuilderMethodParameters.Single().ParameterType))
            {
                activity.WriteError(HotChocolate.ErrorBuilder.New().SetMessage(
                    $"Discovered SchemaBuilder Method {schemaBuilderMethod} " +
                    $"on type {schemaBuilderMethod.DeclaringType} " +
                    $"in Assembly {assembly} has invalid parameters. " +
                    $"Expected single parameter of type {nameof(ISchemaBuilder)}").Build());
                return 1;
            }

            if (!typeof(ISchemaBuilder).IsAssignableFrom(schemaBuilderMethod.ReturnType))
            {
                activity.WriteError(HotChocolate.ErrorBuilder.New().SetMessage(
                    $"Discovered SchemaBuilder Method {schemaBuilderMethod} " +
                    $"on type {schemaBuilderMethod.DeclaringType} " +
                    $"in Assembly {assembly} has invalid return type {schemaBuilderMethod.ReturnType}. " +
                    $"Expected return type ${nameof(ISchemaBuilder)}").Build());

                return 1;
            }

            var schemaBuilder = SchemaBuilder.New();
            var result = (ISchemaBuilder)schemaBuilderMethod.Invoke(null, new[] {schemaBuilder});
            var schema = result.Create();
            var text = schema.ToString();
            var path = export.Path ?? Options.Export.DefaultPath;

            await _fileSystem.WriteToAsync(path, async stream =>
            {
                await using var writer = new StreamWriter(stream, Encoding.UTF8, 1024, leaveOpen: true);
                await writer.WriteAsync(text).ConfigureAwait(false);
            }).ConfigureAwait(false);

            _console.WriteFileCreated(path);

            return 0;
        }



    }
}
