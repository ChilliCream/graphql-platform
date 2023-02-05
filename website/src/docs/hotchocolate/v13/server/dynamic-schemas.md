---
title: Dynamic schemas
---

<WorkInProgress />

Type modules allow you to make parts of your GraphQL schema dynamic. You could for example expose a dynamic JSON file or a database schema as well-typed data in your GraphQL schema.

<Video videoId="EjON8pU-CVs" />

To get started create a class, your type module, inheriting from `ITypeModule`.

<!-- todo: explain more and give some examples for type and field generation -->

```csharp
public class CustomTypeModule : ITypeModule
{
    private readonly string _file;
    private readonly FileSystemWatcher _watcher;

    public event EventHandler<EventArgs>? TypesChanged;

    public CustomTypeModule(string file)
    {
        _file = file;
        _watcher = new FileSystemWatcher(Path.GetDirectoryName(_file)!);

        _watcher.NotifyFilter = NotifyFilters.Attributes
            | NotifyFilters.CreationTime
            | NotifyFilters.FileName
            | NotifyFilters.LastAccess
            | NotifyFilters.LastWrite
            | NotifyFilters.Size;

        _watcher.EnableRaisingEvents = true;
        _watcher.Changed += (sender, args) => TypesChanged?.Invoke(this, EventArgs.Empty);
    }

    public async ValueTask<IReadOnlyCollection<ITypeSystemMember>> CreateTypesAsync(
        IDescriptorContext context,
        CancellationToken cancellationToken)
    {
        var types = new List<ITypeSystemMember>();

        await using var file = File.OpenRead(_file);
        using var json = await JsonDocument.ParseAsync(file, cancellationToken: cancellationToken);

        foreach (var type in json.RootElement.EnumerateArray())
        {
            var typeDefinition = new ObjectTypeDefinition(type.GetProperty("name").GetString()!);

            foreach (var field in type.GetProperty("fields").EnumerateArray())
            {
                typeDefinition.Fields.Add(
                    new ObjectFieldDefinition(
                        field.GetString()!,
                        type: TypeReference.Parse("String!"),
                        pureResolver: ctx => "foo"));
            }

            types.Add(
                type.GetProperty("extension").GetBoolean()
                    ? ObjectTypeExtension.CreateUnsafe(typeDefinition)
                    : ObjectType.CreateUnsafe(typeDefinition));
        }

        return types;
    }
}
```
