using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

var results = new List<object>();
var bodies = new List<Dictionary<string, string>>();
var normalizedBodies = new List<Dictionary<string, string>>();
var opcodes = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
    .Where(f => f.FieldType == typeof(OpCode)).Select(f => (OpCode)f.GetValue(null)!)
    .ToDictionary(o => unchecked((ushort)o.Value));
foreach (var path in args)
{
    using var file = File.OpenRead(path);
    using var pe = new PEReader(file);
    var metadata = pe.GetMetadataReader();
    var assembly = metadata.GetAssemblyDefinition();
    var methods = new Dictionary<string, string>();
    var normalized = new Dictionary<string, string>();
    var pathStrings = new HashSet<string>();
    foreach (var handle in metadata.MethodDefinitions)
    {
        var method = metadata.GetMethodDefinition(handle);
        var type = metadata.GetTypeDefinition(method.GetDeclaringType());
        var key = $"{MetadataTokens.GetToken(handle):x8} {metadata.GetString(type.Namespace)}.{metadata.GetString(type.Name)}.{metadata.GetString(method.Name)}";
        key = Regex.Replace(key, @">F[0-9A-F]{64}__", ">F<file-local-hash>__");
        var il = method.RelativeVirtualAddress == 0 ? [] : pe.GetMethodBody(method.RelativeVirtualAddress).GetILBytes()!;
        methods[key] = Convert.ToHexStringLower(SHA256.HashData(il));
        var decoded = new List<string>();
        for (var offset = 0; offset < il.Length;)
        {
            ushort code = il[offset++];
            if (code == 0xfe)
            {
                code = (ushort)(0xfe00 | il[offset++]);
            }
            var opcode = opcodes[code];
            var length = opcode.OperandType switch
            {
                OperandType.InlineNone => 0,
                OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
                OperandType.InlineVar => 2,
                OperandType.InlineI8 or OperandType.InlineR => 8,
                OperandType.InlineSwitch => 4 + 4 * BitConverter.ToInt32(il, offset),
                _ => 4
            };
            string operand;
            if (opcode.OperandType is OperandType.InlineString)
            {
                operand = metadata.GetUserString(MetadataTokens.UserStringHandle(BitConverter.ToInt32(il, offset) & 0xffffff));
                if (operand.Contains("/workspaces/platform-1/.worktrees/"))
                {
                    pathStrings.Add(operand);
                }
                operand = Regex.Replace(operand, @"/workspaces/platform-1/\.worktrees/[^/]+", "<worktree>");
            }
            else
            {
                operand = Convert.ToHexStringLower(il.AsSpan(offset, length));
            }
            decoded.Add($"{opcode.Name} {operand}");
            offset += length;
        }
        normalized[key] = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(decoded))));
    }
    bodies.Add(methods);
    normalizedBodies.Add(normalized);
    var codeView = pe.ReadDebugDirectory().Where(e => e.Type == DebugDirectoryEntryType.CodeView)
        .Select(e => pe.ReadCodeViewDebugDirectoryData(e).Path).ToArray();
    var attributes = assembly.GetCustomAttributes().Select(handle =>
    {
        var attribute = metadata.GetCustomAttribute(handle);
        return Convert.ToHexStringLower(metadata.GetBlobBytes(attribute.Value));
    }).ToArray();
    results.Add(new
    {
        path, name = metadata.GetString(assembly.Name), version = assembly.Version.ToString(),
        mvid = metadata.GetGuid(metadata.GetModuleDefinition().Mvid), codeView, attributes,
        methods = methods.Count, pathStrings,
        normalizedDigest = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(normalized)))),
        methodBodyDigest = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(methods))))
    });
}
var differences = bodies[0].Keys.Union(bodies[1].Keys)
    .Where(k => bodies[0].GetValueOrDefault(k) != bodies[1].GetValueOrDefault(k)).ToArray();
var normalizedDifferences = normalizedBodies[0].Keys.Union(normalizedBodies[1].Keys)
    .Where(k => normalizedBodies[0].GetValueOrDefault(k) != normalizedBodies[1].GetValueOrDefault(k)).ToArray();
Console.WriteLine(JsonSerializer.Serialize(new { results, methodBodyDifferences = differences, normalizedDifferences }, new JsonSerializerOptions { WriteIndented = true }));
