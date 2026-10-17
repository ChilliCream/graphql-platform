using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text.Json;

var enginePath = Path.GetFullPath(args[0]);
AssemblyLoadContext.Default.Resolving += (_, name) =>
{
    var path = Path.Combine(Path.GetDirectoryName(enginePath)!, name.Name + ".dll");
    return File.Exists(path) ? AssemblyLoadContext.Default.LoadFromAssemblyPath(path) : null;
};
var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(enginePath);
var methodName = args.Length > 1 ? args[1] : "ExecuteInternalAsync";
var sizeMethod = typeof(Unsafe).GetMethod(nameof(Unsafe.SizeOf))!;
var rows = new List<object>();
foreach (var typeName in new[] { "DefaultGraphQLHttpClient" })
{
    var type = assembly.GetType("HotChocolate.Fusion.Transport.Http." + typeName)!;
    var method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!;
    var state = method.GetCustomAttribute<AsyncStateMachineAttribute>()!.StateMachineType;
    rows.Add(new
    {
        typeName,
        methodName,
        stateMachine = state.FullName,
        stateSize = (int)sizeMethod.MakeGenericMethod(state).Invoke(null, null)!,
        fields = state.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Select(f => new { f.Name, type = f.FieldType.FullName }).ToArray()
    });
}
Console.WriteLine(JsonSerializer.Serialize(new
{
    enginePath,
    engineSha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(enginePath))),
    runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
    rows,
    limitation = "Unsafe.SizeOf reports the generated state struct layout, not boxed object allocation. This audit performs no timing or allocation measurement."
}, new JsonSerializerOptions { WriteIndented = true }));
