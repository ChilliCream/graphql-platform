using System.Text;
using HotChocolate.Language;
using HotChocolate.Stitching.Properties;
using HotChocolate.Utilities;

namespace HotChocolate.Stitching.Merge;

public static class MergeSyntaxNodeExtensions
{
    public static T Rename<T>(
        this T enumTypeDefinition,
        string newName,
        params string[] schemaNames)
        where T : ITypeDefinitionNode
        => Rename(enumTypeDefinition, newName, (IEnumerable<string>)schemaNames);

    public static T Rename<T>(
        this T typeDefinitionNode,
        string newName,
        IEnumerable<string> schemaNames)
        where T : ITypeDefinitionNode
    {
        ITypeDefinitionNode node = typeDefinitionNode;

        node = node switch
        {
            ObjectTypeDefinitionNode otd => Rename(otd, newName, schemaNames),
            InterfaceTypeDefinitionNode itd => Rename(itd, newName, schemaNames),
            UnionTypeDefinitionNode utd => Rename(utd, newName, schemaNames),
            InputObjectTypeDefinitionNode iotd => Rename(iotd, newName, schemaNames),
            EnumTypeDefinitionNode etd => Rename(etd, newName, schemaNames),
            ScalarTypeDefinitionNode std => Rename(std, newName, schemaNames),
            _ => throw new NotSupportedException(),
        };

        return (T)node;
    }

    public static FieldDefinitionNode Rename(
        this FieldDefinitionNode enumTypeDefinition,
        string newName,
        params string[] schemaNames)
        => Rename(
            enumTypeDefinition,
            newName,
            (IEnumerable<string>)schemaNames);

    public static FieldDefinitionNode Rename(
        this FieldDefinitionNode enumTypeDefinition,
        string newName,
        IEnumerable<string> schemaNames)
        => AddSource(
            enumTypeDefinition,
            newName,
            schemaNames,
            (n, d) => enumTypeDefinition.WithName(n).WithDirectives(d));

    public static InputValueDefinitionNode Rename(
        this InputValueDefinitionNode enumTypeDefinition,
        string newName,
        params string[] schemaNames)
    {
        return Rename(
            enumTypeDefinition,
            newName,
            (IEnumerable<string>)schemaNames);
    }

    public static InputValueDefinitionNode Rename(
        this InputValueDefinitionNode enumTypeDefinition,
        string newName,
        IEnumerable<string> schemaNames)
    {
        return AddSource(enumTypeDefinition, newName, schemaNames,
            (n, d) => enumTypeDefinition
                .WithName(n).WithDirectives(d));
    }

    public static ScalarTypeDefinitionNode Rename(
        this ScalarTypeDefinitionNode enumTypeDefinition,
        string newName,
        params string[] schemaNames)
    {
        return Rename(
            enumTypeDefinition,
            newName,
            (IEnumerable<string>)schemaNames);
    }

    public static ScalarTypeDefinitionNode Rename(
        this ScalarTypeDefinitionNode enumTypeDefinition,
        string newName,
        IEnumerable<string> schemaNames)
    {
        return AddSource(enumTypeDefinition, newName, schemaNames,
            (n, d) => enumTypeDefinition
                .WithName(n).WithDirectives(d));
    }

    public static DirectiveDefinitionNode Rename(
        this DirectiveDefinitionNode directiveDefinition,
        string newName,
        params string[] schemaNames)
    {
        return Rename(
            directiveDefinition,
            newName,
            (IEnumerable<string>)schemaNames);
    }

    public static DirectiveDefinitionNode Rename(
        this DirectiveDefinitionNode directiveDefinition,
        string newName,
        IEnumerable<string> schemaNames)
    {
        return directiveDefinition.WithName(new NameNode(newName));
    }

    public static EnumTypeDefinitionNode Rename(
        this EnumTypeDefinitionNode enumTypeDefinition,
        string newName,
        params string[] schemaNames)
    {
        return Rename(
            enumTypeDefinition,
            newName,
            (IEnumerable<string>)schemaNames);
    }

    public static EnumTypeDefinitionNode Rename(
        this EnumTypeDefinitionNode enumTypeDefinition,
        string newName,
        IEnumerable<string> schemaNames)
    {
        return AddSource(enumTypeDefinition, newName, schemaNames,
            (n, d) => enumTypeDefinition
                .WithName(n).WithDirectives(d));
    }

    public static InputObjectTypeDefinitionNode Rename(
        this InputObjectTypeDefinitionNode enumTypeDefinition,
        string newName,
        params string[] schemaNames)
    {
        return Rename(
            enumTypeDefinition,
            newName,
            (IEnumerable<string>)schemaNames);
    }

    public static InputObjectTypeDefinitionNode Rename(
        this InputObjectTypeDefinitionNode enumTypeDefinition,
        string newName,
        IEnumerable<string> schemaNames)
    {
        return AddSource(enumTypeDefinition, newName, schemaNames,
            (n, d) => enumTypeDefinition
                .WithName(n).WithDirectives(d));
    }

    public static UnionTypeDefinitionNode Rename(
        this UnionTypeDefinitionNode unionTypeDefinition,
        string newName,
        params string[] schemaNames)
    {
        return Rename(
            unionTypeDefinition,
            newName,
            (IEnumerable<string>)schemaNames);
    }

    public static UnionTypeDefinitionNode Rename(
        this UnionTypeDefinitionNode unionTypeDefinition,
        string newName,
        IEnumerable<string> schemaNames)
    {
        return AddSource(unionTypeDefinition, newName, schemaNames,
            (n, d) => unionTypeDefinition
                .WithName(n).WithDirectives(d));
    }

    public static ObjectTypeDefinitionNode Rename(
        this ObjectTypeDefinitionNode objectTypeDefinition,
        string newName,
        params string[] schemaNames)
    {
        return Rename(
            objectTypeDefinition,
            newName,
            (IEnumerable<string>)schemaNames);
    }

    public static ObjectTypeDefinitionNode Rename(
        this ObjectTypeDefinitionNode objectTypeDefinition,
        string newName,
        IEnumerable<string> schemaNames)
    {
        return AddSource(objectTypeDefinition, newName, schemaNames,
            (n, d) => objectTypeDefinition
                .WithName(n).WithDirectives(d));
    }

    public static InterfaceTypeDefinitionNode Rename(
        this InterfaceTypeDefinitionNode interfaceTypeDefinition,
        string newName,
        params string[] schemaNames)
    {
        return Rename(
            interfaceTypeDefinition,
            newName,
            (IEnumerable<string>)schemaNames);
    }

    public static InterfaceTypeDefinitionNode Rename(
        this InterfaceTypeDefinitionNode interfaceTypeDefinition,
        string newName,
        IEnumerable<string> schemaNames)
    {
        return AddSource(interfaceTypeDefinition, newName, schemaNames,
            (n, d) => interfaceTypeDefinition
                .WithName(n).WithDirectives(d));
    }

    private static T AddSource<T>(
        T interfaceTypeDefinition,
        string newName,
        IEnumerable<string> schemaNames,
        Func<NameNode, IReadOnlyList<DirectiveNode>, T> rewrite)
        where T : NamedSyntaxNode
    {
        if (interfaceTypeDefinition == null)
        {
            throw new ArgumentNullException(
                nameof(interfaceTypeDefinition));
        }

        if (schemaNames == null)
        {
            throw new ArgumentNullException(nameof(schemaNames));
        }

        newName.EnsureGraphQLName(nameof(newName));

        var originalName = interfaceTypeDefinition.Name.Value;

        var directives =
            AddRenamedDirective(
                interfaceTypeDefinition.Directives,
                originalName,
                schemaNames);

        return rewrite(new NameNode(newName), directives);
    }

    private static IReadOnlyList<DirectiveNode> AddRenamedDirective(
        IEnumerable<DirectiveNode> directives,
        string originalName,
        IEnumerable<string> schemaNames)
    {
        var list = new List<DirectiveNode>(directives);
        var hasSchemas = false;

        foreach (var schemaName in schemaNames)
        {
            hasSchemas = true;
            if (!list.Any(t => HasSourceDirective(t, schemaName)))
            {
                list.Add(new DirectiveNode
                (
                    DirectiveNames.Source,
                    new ArgumentNode(
                        DirectiveFieldNames.Source_Name,
                        originalName),
                    new ArgumentNode(
                        DirectiveFieldNames.Source_Schema,
                        schemaName)
                ));
            }
        }

        if (!hasSchemas)
        {
            throw new ArgumentException(
                StitchingResources.MergeSyntaxNodeExtensions_NoSchema,
                nameof(schemaNames));
        }

        return list;
    }

    private static bool HasSourceDirective(
        DirectiveNode directive,
        string schemaName)
    {
        if (DirectiveNames.Source.Equals(directive.Name.Value))
        {
            var argument = directive.Arguments.FirstOrDefault(t =>
                DirectiveFieldNames.Source_Schema.Equals(t.Name.Value));
            return argument?.Value is StringValueNode sv
                && schemaName.Equals(sv.Value);
        }
        return false;
    }

    public static FieldDefinitionNode AddDelegationPath(
        this FieldDefinitionNode field,
        string schemaName)
        => AddDelegationPath(field, schemaName, false);

    public static FieldDefinitionNode AddDelegationPath(
        this FieldDefinitionNode field,
        string schemaName,
        bool overwrite)
        => AddDelegationPath(field, schemaName, (string?)null, overwrite);

    public static FieldDefinitionNode AddDelegationPath(
        this FieldDefinitionNode field,
        string schemaName,
        SelectionPathComponent selectionPath) =>
        AddDelegationPath(field, schemaName, selectionPath, false);

    public static FieldDefinitionNode AddDelegationPath(
        this FieldDefinitionNode field,
        string schemaName,
        SelectionPathComponent selectionPath,
        bool overwrite)
    {
        if (field == null)
        {
            throw new ArgumentNullException(nameof(field));
        }

        if (selectionPath == null)
        {
            throw new ArgumentNullException(nameof(selectionPath));
        }

        schemaName.EnsureGraphQLName(nameof(schemaName));

        return AddDelegationPath(
            field,
            schemaName,
            selectionPath.ToString(),
            overwrite);
    }

    public static FieldDefinitionNode AddDelegationPath(
        this FieldDefinitionNode field,
        string schemaName,
        IReadOnlyCollection<SelectionPathComponent> selectionPath) =>
        AddDelegationPath(field, schemaName, selectionPath, false);

    public static FieldDefinitionNode AddDelegationPath(
        this FieldDefinitionNode field,
        string schemaName,
        IReadOnlyCollection<SelectionPathComponent> selectionPath,
        bool overwrite)
    {
        if (field == null)
        {
            throw new ArgumentNullException(nameof(field));
        }

        if (selectionPath == null)
        {
            throw new ArgumentNullException(nameof(selectionPath));
        }

        schemaName.EnsureGraphQLName(nameof(schemaName));

        if (selectionPath.Count == 0)
        {
            return AddDelegationPath(field, schemaName);
        }

        if (selectionPath.Count == 1)
        {
            return AddDelegationPath(
                field,
                schemaName,
                selectionPath.Single());
        }

        var path = new StringBuilder();
        path.Append(selectionPath.First());

        foreach (var component in selectionPath.Skip(1))
        {
            path.Append('.');
            path.Append(component);
        }

        return AddDelegationPath(field, schemaName, path.ToString(), overwrite);
    }

    public static FieldDefinitionNode AddDelegationPath(
        this FieldDefinitionNode field,
        string schemaName,
        string? delegationPath)
        => AddDelegationPath(field, schemaName, delegationPath, false);

    public static FieldDefinitionNode AddDelegationPath(
        this FieldDefinitionNode field,
        string schemaName,
        string? delegationPath,
        bool overwrite)
    {
        if (field is null)
        {
            throw new ArgumentNullException(nameof(field));
        }

        schemaName.EnsureGraphQLName(nameof(schemaName));

        if (!overwrite && field.Directives.Any(t =>
            DirectiveNames.Delegate.Equals(t.Name.Value)))
        {
            return field;
        }

        var list = new List<DirectiveNode>(field.Directives);

        list.RemoveAll(t => DirectiveNames.Delegate.Equals(t.Name.Value));

        var arguments = new List<ArgumentNode>
        {
            new ArgumentNode(
                DirectiveFieldNames.Delegate_Schema,
                schemaName)
        };

        if (!string.IsNullOrEmpty(delegationPath))
        {
            arguments.Add(new ArgumentNode(
                DirectiveFieldNames.Delegate_Path,
                delegationPath));
        }

        list.Add(new DirectiveNode
        (
            DirectiveNames.Delegate,
            arguments
        ));

        return field.WithDirectives(list);
    }

    public static string GetOriginalName(
        this INamedSyntaxNode typeDefinition,
        string schemaName)
    {
        if (typeDefinition == null)
        {
            throw new ArgumentNullException(nameof(typeDefinition));
        }

        schemaName.EnsureGraphQLName(nameof(schemaName));

        var sourceDirective = typeDefinition.Directives
            .FirstOrDefault(t => HasSourceDirective(t, schemaName));

        if (sourceDirective is not null)
        {
            var argument = sourceDirective.Arguments.First(t =>
                DirectiveFieldNames.Source_Name.Equals(t.Name.Value));
            if (argument.Value is StringValueNode value)
            {
                return value.Value;
            }
        }

        return typeDefinition.Name.Value;
    }

    public static bool IsFromSchema(
        this INamedSyntaxNode typeDefinition,
        string schemaName)
    {
        if (typeDefinition == null)
        {
            throw new ArgumentNullException(nameof(typeDefinition));
        }

        schemaName.EnsureGraphQLName(nameof(schemaName));

        return typeDefinition.Directives.Any(t =>
            HasSourceDirective(t, schemaName));
    }
}
