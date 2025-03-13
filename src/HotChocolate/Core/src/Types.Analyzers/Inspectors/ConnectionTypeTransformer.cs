using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types.Analyzers.Helpers;
using HotChocolate.Types.Analyzers.Models;
using Microsoft.CodeAnalysis;

namespace HotChocolate.Types.Analyzers.Inspectors;

public class ConnectionTypeTransformer : IPostCollectSyntaxTransformer
{
    public ImmutableArray<SyntaxInfo> Transform(
        Compilation compilation,
        ImmutableArray<SyntaxInfo> syntaxInfos)
    {
        Dictionary<Resolver, IOutputTypeInfo>? connectionResolvers = null;
        Dictionary<string, ConnectionClassInfo>? connectionClassLookup = null;

        foreach (var syntaxInfo in syntaxInfos)
        {
            if (syntaxInfo is ConnectionClassInfo connectionClass)
            {
                connectionClassLookup ??= [];
                connectionClassLookup[connectionClass.RuntimeType.ToFullyQualified()] = connectionClass;
                continue;
            }

            if (syntaxInfo is IOutputTypeInfo typeInfo
                && typeInfo.Resolvers.Any(t => t.Kind is ResolverKind.ConnectionResolver))
            {
                foreach (var resolver in typeInfo.Resolvers)
                {
                    if (resolver.Kind is ResolverKind.ConnectionResolver)
                    {
                        connectionResolvers ??= [];
                        connectionResolvers.Add(resolver, typeInfo);
                    }
                }
            }
        }

        if (connectionResolvers is not null)
        {
            connectionClassLookup ??= [];
            var connectionTypeLookup = new Dictionary<string, IOutputTypeInfo>();
            var connectionNameLookup = new Dictionary<string, string>();
            List<SyntaxInfo>? connectionTypeInfos = null;

            foreach (var syntaxInfo in syntaxInfos)
            {
                if (syntaxInfo is IOutputTypeInfo { HasRuntimeType: true } typeInfo)
                {
#if NET8_0_OR_GREATER
                    connectionTypeLookup[typeInfo.RuntimeTypeFullName] = typeInfo;
#else
                    connectionTypeLookup[typeInfo.RuntimeTypeFullName!] = typeInfo;
#endif
                }
            }

#if NET8_0_OR_GREATER
            foreach (var (connectionResolver, owner) in connectionResolvers)
            {
#else
            foreach (var item in connectionResolvers.ToImmutableArray())
            {
                var connectionResolver = item.Key;
                var owner = item.Value;
#endif
                var connectionType = GetConnectionType(compilation, connectionResolver.Member.GetReturnType());
                ConnectionTypeInfo connectionTypeInfo;
                ConnectionClassInfo? connectionClass;
                EdgeTypeInfo? edgeTypeInfo;
                ConnectionClassInfo? edgeClass;

                if (connectionType.IsGenericType)
                {
                    var diagnostics = ImmutableArray<Diagnostic>.Empty;

                    var connection = CreateGenericTypeInfo(
                        connectionType,
                        connectionResolver.Member,
                        compilation,
                        isConnection: true,
                        connectionNameLookup,
                        ref diagnostics);

                    if (connection is null)
                    {
                        connectionTypeInfo = ConnectionTypeInfo.CreateConnection(
                            compilation,
                            connectionType,
                            null,
                            connectionType.Name);
                        connectionTypeInfo.AddDiagnosticRange(diagnostics);
                        connectionTypeInfos ??= [];
                        connectionTypeInfos.Add(connectionTypeInfo);
                        continue;
                    }

                    var edgeType = GetEdgeType(connection.Type);
                    if (edgeType is null)
                    {
                        continue;
                    }

                    var edge = CreateGenericTypeInfo(
                        edgeType,
                        connectionResolver.Member,
                        compilation,
                        isConnection: false,
                        connectionNameLookup,
                        ref diagnostics);

                    if (edge is null)
                    {
                        edgeTypeInfo = EdgeTypeInfo.CreateEdge(
                            compilation,
                            connectionType,
                            null);
                        edgeTypeInfo.AddDiagnosticRange(diagnostics);
                        connectionTypeInfos ??= [];
                        connectionTypeInfos.Add(edgeTypeInfo);
                        continue;
                    }

                    edgeTypeInfo =
                        connectionClassLookup.TryGetValue(edge.TypeDefinitionName, out edgeClass)
                            ? EdgeTypeInfo.CreateEdge(
                                compilation,
                                edge.Type,
                                edgeClass.ClassDeclarations,
                                edge.Name,
                                edge.NameFormat)
                            : EdgeTypeInfo.CreateEdge(
                                compilation,
                                edge.Type,
                                null,
                                edge.Name,
                                edge.NameFormat);

                    connectionTypeInfo =
                        connectionClassLookup.TryGetValue(connection.TypeDefinitionName, out connectionClass)
                            ? ConnectionTypeInfo.CreateConnection(
                                compilation,
                                connection.Type,
                                connectionClass.ClassDeclarations,
                                edgeTypeInfo.Name,
                                connection.Name,
                                connection.NameFormat)
                            : ConnectionTypeInfo.CreateConnection(
                                compilation,
                                connection.Type,
                                null,
                                edgeTypeInfo.Name,
                                connection.Name,
                                connection.NameFormat);

                    var connectionTypeName = "global::" + connectionTypeInfo.Namespace + "." + connectionTypeInfo.Name;
                    var edgeTypeName = "global::" + edgeTypeInfo.Namespace + "." + edgeTypeInfo.Name;

                    if (!connectionTypeLookup.ContainsKey(connectionTypeName))
                    {
                        connectionTypeInfos ??= [];
                        connectionTypeInfos.Add(connectionTypeInfo);
                        connectionTypeLookup.Add(connectionTypeName, connectionTypeInfo);
                    }

                    if (!connectionTypeLookup.ContainsKey(edgeTypeName))
                    {
                        connectionTypeInfos ??= [];
                        connectionTypeInfos.Add(edgeTypeInfo);
                        connectionTypeLookup.Add(edgeTypeName, edgeTypeInfo);
                    }
                }
                else
                {
                    var edgeType = GetEdgeType(connectionType);
                    if (edgeType is null)
                    {
                        continue;
                    }

                    string? connectionName = null;
                    string? edgeName = null;

                    if (compilation.TryGetConnectionNameFromResolver(connectionResolver.Member, out var name))
                    {
                        connectionName = $"{name}Connection";
                        edgeName = $"{name}Edge";
                    }

                    edgeTypeInfo =
                        connectionClassLookup.TryGetValue(edgeType.ToFullyQualified(), out edgeClass)
                            ? EdgeTypeInfo.CreateEdgeFrom(edgeClass, edgeName, edgeName)
                            : EdgeTypeInfo.CreateEdge(compilation, edgeType, null, edgeName, edgeName);

                    connectionTypeInfo =
                        connectionClassLookup.TryGetValue(connectionType.ToFullyQualified(), out connectionClass)
                            ? ConnectionTypeInfo.CreateConnectionFrom(
                                connectionClass,
                                edgeTypeInfo.Name,
                                connectionName,
                                connectionName)
                            : ConnectionTypeInfo.CreateConnection(
                                compilation,
                                connectionType,
                                null,
                                edgeType.Name,
                                connectionName,
                                connectionName);

                    var connectionTypeName = "global::" + connectionTypeInfo.Namespace + "." + connectionTypeInfo.Name;
                    var edgeTypeName = "global::" + edgeTypeInfo.Namespace + "." + edgeTypeInfo.Name;

                    if (!connectionTypeLookup.ContainsKey(connectionTypeName))
                    {
                        connectionTypeInfos ??= [];
                        connectionTypeInfos.Add(connectionTypeInfo);
                        connectionTypeLookup.Add(connectionTypeName, connectionTypeInfo);
                    }

                    if (!connectionTypeLookup.ContainsKey(edgeTypeName))
                    {
                        connectionTypeInfos ??= [];
                        connectionTypeInfos.Add(edgeTypeInfo);
                        connectionTypeLookup.Add(edgeTypeName, edgeTypeInfo);
                    }
                }

                owner.ReplaceResolver(
                    connectionResolver,
                    connectionResolver.WithSchemaTypeName(
                        $"{connectionTypeInfo.Namespace}.{connectionTypeInfo.Name}"));
            }

            if (connectionTypeInfos is not null)
            {
                return syntaxInfos.AddRange(connectionTypeInfos);
            }
        }

        return syntaxInfos;
    }

    private static INamedTypeSymbol GetConnectionType(Compilation compilation, ITypeSymbol? possibleConnectionType)
    {
        if (possibleConnectionType is null)
        {
            Throw();
        }

        if (compilation.IsTaskOrValueTask(possibleConnectionType, out var type))
        {
            if (type is INamedTypeSymbol namedType1)
            {
                return namedType1;
            }

            Throw();
        }

        if (possibleConnectionType is not INamedTypeSymbol namedType2)
        {
            Throw();
        }

        return namedType2;

        [DoesNotReturn]
        static void Throw() => throw new InvalidOperationException("Could not resolve connection base type.");
    }

    private static GenericTypeInfo? CreateGenericTypeInfo(
        INamedTypeSymbol genericType,
        ISymbol resolver,
        Compilation compilation,
        bool isConnection,
        Dictionary<string, string> connectionNameLookup,
        ref ImmutableArray<Diagnostic> diagnostics)
    {
        if (genericType.TypeArguments.Length > 1)
        {
            // we can only handle connections/edges with a single type argument.
            // the generic type argument must represent the entity.
            diagnostics = diagnostics.Add(
                Diagnostic.Create(
                    Errors.ConnectionSingleGenericTypeArgument,
                    resolver.Locations[0]));
            return null;
        }

        // first we check if there is a name template defined for the specified connection.
        if (compilation.TryGetGraphQLTypeName(genericType, out var nameFormat)
            && (string.IsNullOrEmpty(nameFormat) || !nameFormat.Contains("{0}")))
        {
            diagnostics = diagnostics.Add(
                Diagnostic.Create(
                    Errors.ConnectionNameFormatIsInvalid,
                    resolver.Locations[0]));
            return null;
        }

        // next we get the generic type definition for the connection type.
        var typeDefinition = genericType.OriginalDefinition;
        var typeDefinitionName = typeDefinition.ToFullyQualified();

        // if we do not have a name format we will create one from the generic type definition.
        nameFormat ??= $"{{0}}{typeDefinition.Name}";

        if (compilation.TryGetConnectionNameFromResolver(resolver, out var name))
        {
            // if the user specified that for this resolver there should be a specific connection name,
            // then we will take the defined connection name and discard the name format.
            name = isConnection ? $"{name}Connection" : $"{name}Edge";
            nameFormat = null;
        }
        else
        {
            // if there was no connection name specified we will construct it with the name format.
            name = string.Format(nameFormat, genericType.TypeArguments[0].Name);
        }

        // we will now create the full connection type name.
        var typeName = $"{genericType.ContainingNamespace.ToDisplayString()}.{name}";
        var runtimeTypeName = genericType.ToDisplayString();

        // we need to make sure that not different .NET types represent the same connection name.
        if (connectionNameLookup.TryGetValue(typeName, out var expectedRuntimeTypeName)
            && !string.Equals(expectedRuntimeTypeName, runtimeTypeName, StringComparison.Ordinal))
        {
            diagnostics = diagnostics.Add(
                Diagnostic.Create(
                    Errors.ConnectionNameDuplicate,
                    resolver.Locations[0],
                    messageArgs: [runtimeTypeName, typeName, expectedRuntimeTypeName]));
            return null;
        }

        // we will store the connection name and the runtime type name for later reference.
        connectionNameLookup[typeName] = runtimeTypeName;
        return new GenericTypeInfo(typeDefinitionName, genericType, name, nameFormat);
    }

    private sealed class GenericTypeInfo(
        string typeDefinitionName,
        INamedTypeSymbol type,
        string name,
        string? nameFormat)
    {
        public string TypeDefinitionName { get; } = typeDefinitionName;
        public INamedTypeSymbol Type { get; } = type;
        public string Name { get; } = name;
        public string? NameFormat { get; } = nameFormat;
    }

    private static INamedTypeSymbol? GetEdgeType(INamedTypeSymbol connectionType)
    {
        var property = connectionType.GetMembers()
            .OfType<IPropertySymbol>()
            .FirstOrDefault(p => p.Name == "Edges");

        if (property is null)
        {
            return null;
        }

        var returnType = property.GetReturnType();
        if (returnType is not INamedTypeSymbol namedType
            || !namedType.IsGenericType
            || namedType.TypeArguments.Length != 1
            || namedType.Name != "IReadOnlyList")
        {
            return null;
        }

        return (INamedTypeSymbol)namedType.TypeArguments[0];
    }
}
