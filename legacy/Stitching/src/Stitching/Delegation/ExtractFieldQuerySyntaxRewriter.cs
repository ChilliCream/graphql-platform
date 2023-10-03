using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Stitching.Utilities;
using HotChocolate.Types;
using HotChocolate.Utilities;
using HotChocolate.Utilities.Introspection;

namespace HotChocolate.Stitching.Delegation;

public partial class ExtractFieldQuerySyntaxRewriter
    : SyntaxRewriter<ExtractFieldQuerySyntaxRewriter.Context>
{
    private readonly ISchema _schema;
    private readonly FieldDependencyResolver _dependencyResolver;
    private readonly IQueryDelegationRewriter[] _rewriters;

    public ExtractFieldQuerySyntaxRewriter(
        ISchema schema,
        IEnumerable<IQueryDelegationRewriter> rewriters)
    {
        _schema = schema ?? throw new ArgumentNullException(nameof(schema));
        _dependencyResolver = new FieldDependencyResolver(schema);
        _rewriters = rewriters.ToArray();
    }

    public ExtractedField ExtractField(
        string sourceSchema,
        DocumentNode document,
        OperationDefinitionNode operation,
        ISelection selection,
        INamedOutputType declaringType)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (operation == null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        if (selection == null)
        {
            throw new ArgumentNullException(nameof(selection));
        }

        if (declaringType == null)
        {
            throw new ArgumentNullException(nameof(declaringType));
        }

        if (string.IsNullOrEmpty(sourceSchema))
        {
            throw new ArgumentNullException(nameof(sourceSchema));
        }

        var context = new Context(
            sourceSchema,
            declaringType,
            document,
            operation);

        var syntaxNodes = new List<FieldNode>();

        var syntaxNode = selection.SyntaxNode;

        var field = RewriteField(syntaxNode, context);

        if (selection.Field.Type.NamedType().IsLeafType() ||
            (field.SelectionSet is not null &&
                field.SelectionSet.Selections.Count > 0))
        {
            syntaxNodes.Add(field);
        }

        return new ExtractedField(
            syntaxNodes,
            context.Variables.Values.ToList(),
            context.Fragments.Values.ToList());
    }

    public IValueNode RewriteValueNode(
        string sourceSchema,
        IInputType inputType,
        IValueNode value)
    {
        sourceSchema.EnsureGraphQLName(nameof(sourceSchema));

        var context = new Context(sourceSchema, null, null, null) { InputType = inputType };
        return (IValueNode)Rewrite(value, context)!;
    }

    protected override FieldNode RewriteField(
        FieldNode node,
        Context context)
    {
        if (context.TypeContext is IComplexOutputType type &&
            type.Fields.TryGetField(node.Name.Value, out var field))
        {
            var cloned = context.Clone();
            cloned.OutputField = field;

            var name = node.Name;
            var alias = node.Alias;

            if (field.TryGetSourceDirective(cloned.Schema, out var sourceDirective))
            {
                alias ??= name;
                name = new NameNode(sourceDirective.Name);
            }

            var directives = node.Directives;

            if (directives.Count > 0)
            {
                List<DirectiveNode>? temp = null;

                foreach (var directive in directives)
                {
                    if (BuiltInTypes.IsBuiltInType(directive.Name.Value))
                    {
                        temp ??= new List<DirectiveNode>(directives);
                        temp.Remove(directive);
                    }
                }

                if (temp is not null)
                {
                    directives = temp;
                }

                directives = RewriteList(directives, cloned);
            }

            var required = RewriteNodeOrDefault(node.Required, cloned);
            var arguments = RewriteList(node.Arguments, cloned);
            var selectionSet = node.SelectionSet;

            if (node.SelectionSet is not null && field.Type.NamedType() is INamedOutputType n)
            {
                var selectionSetContext = cloned.Clone();
                selectionSetContext.TypeContext = n;
                selectionSet = RewriteNodeOrDefault(node.SelectionSet, selectionSetContext);
            }

            if (!ReferenceEquals(name, node.Name) ||
                !ReferenceEquals(alias, node.Alias) ||
                !ReferenceEquals(required, node.Required) ||
                !ReferenceEquals(directives, node.Directives) ||
                !ReferenceEquals(arguments, node.Arguments) ||
                !ReferenceEquals(selectionSet, node.SelectionSet))
            {
                node = new FieldNode(
                    node.Location,
                    name,
                    alias,
                    required,
                    directives,
                    arguments,
                    selectionSet);
            }

            return OnRewriteField(node, cloned);
        }

        return node;
    }

    private FieldNode OnRewriteField(
        FieldNode node,
        Context context)
    {
        if (_rewriters.Length == 0)
        {
            return node;
        }

        var current = node;

        for (var i = 0; i < _rewriters.Length; i++)
        {
            current = _rewriters[i].OnRewriteField(
                context.Schema,
                context.TypeContext!,
                context.OutputField!,
                current);
        }

        return current;
    }

    protected override SelectionSetNode RewriteSelectionSet(
        SelectionSetNode node,
        Context context)
    {
        var current = node;

        var selections = new List<ISelectionNode>(current.Selections);

        var dependencies =
            _dependencyResolver.GetFieldDependencies(
                context.Document!,
                current,
                context.TypeContext!);

        RemoveDelegationFields(current, context, selections);
        AddDependencies(context.TypeContext!, selections, dependencies);

        if (selections.OfType<FieldNode>().All(n => n.Name.Value != WellKnownFieldNames.TypeName))
        {
            selections.Add(CreateField(WellKnownFieldNames.TypeName));
        }

        current = current.WithSelections(selections);
        current = base.RewriteSelectionSet(current, context);
        current = OnRewriteSelectionSet(current!, context);

        return current;
    }

    protected override DirectiveNode? RewriteDirective(DirectiveNode node, Context context)
    {
        if (node.Arguments.Count > 0)
        {
            var arguments = RewriteList(node.Arguments, context);
            if (!ReferenceEquals(arguments, node.Arguments))
            {
                return node.WithArguments(arguments);
            }
        }

        return base.RewriteDirective(node, context);
    }

    private SelectionSetNode OnRewriteSelectionSet(
        SelectionSetNode node,
        Context context)
    {
        if (_rewriters.Length == 0)
        {
            return node;
        }

        var current = node;

        for (var i = 0; i < _rewriters.Length; i++)
        {
            current = _rewriters[i].OnRewriteSelectionSet(
                context.Schema,
                context.TypeContext!,
                context.OutputField!,
                current);
        }

        return current;
    }

    protected override ArgumentNode? RewriteArgument(
        ArgumentNode node,
        Context context)
    {
        var current = node;

        if (context.OutputField != null &&
            context.OutputField.Arguments.TryGetField(
                current.Name.Value,
                out var inputField))
        {
            var cloned = context.Clone();
            cloned.InputField = inputField;
            cloned.InputType = inputField.Type;

            if (inputField.TryGetSourceDirective(
                    context.Schema,
                    out var sourceDirective) &&
                !sourceDirective.Name.Equals(current.Name.Value))
            {
                current = current.WithName(new NameNode(sourceDirective.Name));
            }

            return base.RewriteArgument(current, cloned);
        }

        return base.RewriteArgument(current, context);
    }

    protected override ObjectFieldNode? RewriteObjectField(
        ObjectFieldNode node,
        Context context)
    {
        var current = node;

        if (context.InputType?.NamedType() is InputObjectType inputType &&
            inputType.Fields.TryGetField(current.Name.Value, out var inputField))
        {
            var cloned = context.Clone();
            cloned.InputField = inputField;
            cloned.InputType = inputField.Type;

            if (inputField.TryGetSourceDirective(context.Schema, out var sourceDirective) &&
                !sourceDirective.Name.Equals(current.Name.Value))
            {
                current = current.WithName(
                    new NameNode(sourceDirective.Name));
            }

            Rewrite(node.Value, context);

            return base.RewriteObjectField(current, cloned);
        }

        return base.RewriteObjectField(current, context);
    }

    private static void RemoveDelegationFields(
        SelectionSetNode node,
        Context context,
        ICollection<ISelectionNode> selections)
    {
        if (context.TypeContext is IComplexOutputType type)
        {
            foreach (var selection in node.Selections.OfType<FieldNode>())
            {
                if (type.Fields.TryGetField(selection.Name.Value, out var field) &&
                    IsDelegationField(field.Directives))
                {
                    selections.Remove(selection);
                }
            }
        }
    }

    private static bool IsDelegationField(IDirectiveCollection directives)
        => directives.ContainsDirective(DirectiveNames.Delegate) ||
            directives.ContainsDirective(DirectiveNames.Computed);

    private static void AddDependencies(
        Types.IHasName typeContext,
        List<ISelectionNode> selections,
        IEnumerable<FieldDependency> dependencies)
    {
        foreach (var typeGroup in dependencies.GroupBy(t => t.TypeName))
        {
            var fields = new List<FieldNode>();

            foreach (var fieldName in typeGroup.Select(t => t.FieldName))
            {
                fields.Add(CreateField(fieldName));
            }

            if (typeGroup.Key.Equals(typeContext.Name))
            {
                selections.AddRange(fields);
            }
            else
            {
                selections.Add(
                    new InlineFragmentNode(
                        null,
                        new NamedTypeNode(null, new NameNode(typeGroup.Key)),
                        Array.Empty<DirectiveNode>(),
                        new SelectionSetNode(null, fields)));
            }
        }
    }

    private static FieldNode CreateField(string fieldName)
        => new(null,
            new NameNode(fieldName),
            null,
            null,
            Array.Empty<DirectiveNode>(),
            Array.Empty<ArgumentNode>(),
            null);

    protected override VariableNode? RewriteVariable(
        VariableNode node,
        Context context)
    {
        if (!context.Variables.ContainsKey(node.Name.Value))
        {
            var variableDefinition =
                context.Operation!.VariableDefinitions
                    .First(t => t.Variable.Name.Value.EqualsOrdinal(node.Name.Value));
            context.Variables[node.Name.Value] = variableDefinition;
        }

        return base.RewriteVariable(node, context);
    }

    protected override FragmentSpreadNode? RewriteFragmentSpread(
        FragmentSpreadNode node,
        Context context)
    {
        var name = node.Name.Value;

        if (!context.Fragments.TryGetValue(name, out var fragment))
        {
            fragment = context.Document!.Definitions
                .OfType<FragmentDefinitionNode>()
                .First(t => t.Name.Value.EqualsOrdinal(name));
            fragment = RewriteFragmentDefinition(fragment, context);
            context.Fragments[name] = fragment!;
        }

        return base.RewriteFragmentSpread(node, context);
    }

    protected override FragmentDefinitionNode? RewriteFragmentDefinition(
        FragmentDefinitionNode node,
        Context context)
    {
        var currentContext = context;
        var current = node;

        if (currentContext.FragmentPath.Contains(current.Name.Value))
        {
            return node;
        }

        if (_schema.TryGetType<IComplexOutputType>(current.TypeCondition.Name.Value, out var type))
        {
            currentContext = currentContext.Clone();
            currentContext.TypeContext = type;
            currentContext.FragmentPath = currentContext.FragmentPath.Add(current.Name.Value);

            if (type.TryGetSourceDirective(context.Schema, out var sourceDirective))
            {
                current = current.WithTypeCondition(
                    current.TypeCondition.WithName(
                        new NameNode(sourceDirective.Name)));
            }
        }

        return base.RewriteFragmentDefinition(current, currentContext);
    }

    protected override InlineFragmentNode? RewriteInlineFragment(
        InlineFragmentNode node,
        Context context)
    {
        var currentContext = context;
        var current = node;

        if (_schema.TryGetType<IComplexOutputType>(current.TypeCondition!.Name.Value, out var type))
        {
            currentContext = currentContext.Clone();
            currentContext.TypeContext = type;

            if (type.TryGetSourceDirective(
                context.Schema,
                out var sourceDirective))
            {
                current = current.WithTypeCondition(
                    current.TypeCondition.WithName(
                        new NameNode(sourceDirective.Name)));
            }
        }

        return base.RewriteInlineFragment(current, currentContext);
    }
}
