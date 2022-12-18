namespace HotChocolate.CodeGeneration;

public static class TypeNames
{
    public const string Types = "HotChocolate." + nameof(Types);
    public const string Data = "HotChocolate." + nameof(Data);
    public const string Configuration = "HotChocolate.Execution.Configuration";
    public const string DependencyInjection = "Microsoft.Extensions.DependencyInjection";
    public const string SystemCollections = "System.Collections.Generic";
    public const string UsePagingAttribute = Types + "." + nameof(UsePagingAttribute);
    public const string UseOffsetPagingAttribute = Types + "." + 
        nameof(UseOffsetPagingAttribute);
    public const string UseFilteringAttribute = Data + "." + nameof(UseFilteringAttribute);
    public const string UseSortingAttribute = Data + "." + nameof(UseSortingAttribute);
    public const string UseProjectionAttribute = Data + "." + nameof(UseProjectionAttribute);
    public const string List = SystemCollections + "." + nameof(List);
    public const string IRequestExecutorBuilder = Configuration + "." + 
        nameof(IRequestExecutorBuilder); 
    public const string SchemaRequestExecutorBuilderExtensions = DependencyInjection + "." +
        nameof(SchemaRequestExecutorBuilderExtensions);

    public static string Global(string s)
    {
        return "global::" + s;
    }

    public static string Generics(string s, params string[] args)
    {
        return $"{s}<{string.Join(", ", args)}>";
    }

    public static string Nullable(string s)
    {
        return $"{s}?";
    }
}