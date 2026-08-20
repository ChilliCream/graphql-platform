using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;

namespace HotChocolate.Validation.Rules;

/// <summary>
/// If any operation defines more than one variable with the same name,
/// it is ambiguous and invalid. It is invalid even if the type of the
/// duplicate variable is the same.
///
/// https://spec.graphql.org/September2025/#sec-Validation.Variables
///
/// AND
///
/// Variables can only be input types. Objects,
/// unions, and interfaces cannot be used as inputs.
///
/// https://spec.graphql.org/September2025/#sec-Variables-Are-Input-Types
///
/// AND
///
/// All variables defined by an operation must be used in that operation
/// or a fragment transitively included by that operation.
///
/// Unused variables cause a validation error.
///
/// https://spec.graphql.org/September2025/#sec-All-Variables-Used
///
/// AND
///
/// Variables are scoped on a per‐operation basis. That means that
/// any variable used within the context of an operation must be defined
/// at the top level of that operation
///
/// https://spec.graphql.org/September2025/#sec-All-Variable-Uses-Defined
///
/// AND
///
/// Variable usages must be compatible with the arguments
/// they are passed to.
///
/// Validation failures occur when variables are used in the context
/// of types that are complete mismatches, or if a nullable type in a
///  variable is passed to a non‐null argument type.
///
/// https://spec.graphql.org/September2025/#sec-All-Variable-Usages-Are-Allowed
/// </summary>
internal sealed class VariableVisitor : TypeDocumentValidatorVisitor
{
    public VariableVisitor()
        : base(new SyntaxVisitorOptions
        {
            VisitDirectives = true,
            VisitArguments = true
        })
    {
    }

    protected override ISyntaxVisitorAction Enter(
        OperationDefinitionNode node,
        DocumentValidatorContext context)
    {
        context.Features.GetOrSet<VariableVisitorFeature>().Reset();
        return base.Enter(node, context);
    }

    protected override ISyntaxVisitorAction Leave(
        OperationDefinitionNode node,
        DocumentValidatorContext context)
    {
        var feature = context.Features.GetRequired<VariableVisitorFeature>();
        feature.Unused.ExceptWith(feature.Used);
        feature.Used.ExceptWith(feature.Declared);

        if (feature.Unused.Count > 0)
        {
            context.ReportError(context.VariableNotUsed(node, feature.Unused));
        }

        if (feature.Used.Count > 0)
        {
            context.ReportError(context.VariableNotDeclared(node, feature.Used));
        }

        return base.Leave(node, context);
    }

    protected override ISyntaxVisitorAction Enter(
        FragmentDefinitionNode node,
        DocumentValidatorContext context)
    {
        var result = base.Enter(node, context);

        // A frame is only pushed when the fragment is entered, because Skip also skips Leave.
        if (result.IsContinue())
        {
            PushFragmentVariableFrame(context);
        }

        return result;
    }

    protected override ISyntaxVisitorAction Leave(
        FragmentDefinitionNode node,
        DocumentValidatorContext context)
    {
        var frame = PeekFragmentVariableFrame(context);
        List<string>? unused = null;

        if (frame is not null)
        {
            foreach (var declared in frame.Declared)
            {
                if (!frame.Used.Contains(declared))
                {
                    (unused ??= []).Add(declared);
                }
            }

            context.Features.GetRequired<VariableVisitorFeature>().FragmentVariableDepth--;
        }

        if (unused is not null)
        {
            unused.Sort(StringComparer.Ordinal);
            context.ReportError(context.FragmentVariableNotUsed(node, unused));
        }

        return base.Leave(node, context);
    }

    private static VariableVisitorFeature.FragmentVariableFrame PushFragmentVariableFrame(
        DocumentValidatorContext context)
    {
        var feature = context.Features.GetRequired<VariableVisitorFeature>();

        if (feature.FragmentVariableDepth == feature.FragmentVariableFrames.Count)
        {
            feature.FragmentVariableFrames.Add(new VariableVisitorFeature.FragmentVariableFrame());
        }

        var frame = feature.FragmentVariableFrames[feature.FragmentVariableDepth++];
        frame.Declared.Clear();
        frame.Used.Clear();
        return frame;
    }

    private static VariableVisitorFeature.FragmentVariableFrame? PeekFragmentVariableFrame(
        DocumentValidatorContext context)
    {
        var feature = context.Features.GetRequired<VariableVisitorFeature>();

        return feature.FragmentVariableDepth == 0
            ? null
            : feature.FragmentVariableFrames[feature.FragmentVariableDepth - 1];
    }

    protected override ISyntaxVisitorAction Enter(
        VariableDefinitionNode node,
        DocumentValidatorContext context)
    {
        var feature = context.Features.GetRequired<VariableVisitorFeature>();
        var isFragmentVariable = context.Path.Peek() is FragmentDefinitionNode;

        base.Enter(node, context);

        var variableName = node.Variable.Name.Value;

        if (!isFragmentVariable)
        {
            feature.Unused.Add(variableName);
            feature.Declared.Add(variableName);
        }

        if (context.Schema.Types.TryGetType<ITypeDefinition>(
            node.Type.NamedType().Name.Value, out var type)
            && !type.IsInputType())
        {
            context.ReportError(context.VariableNotInputType(node, variableName));
        }

        // Uniqueness applies per operation or per fragment, so a fragment may declare a
        // variable whose name an enclosing operation also declares.
        var variableNames = isFragmentVariable
            ? PeekFragmentVariableFrame(context)?.Declared ?? feature.VariableNames
            : feature.VariableNames;

        if (!variableNames.Add(variableName))
        {
            context.ReportError(context.VariableNameNotUnique(node, variableName));
        }

        return Skip;
    }

    protected override ISyntaxVisitorAction Enter(
        FieldNode node,
        DocumentValidatorContext context)
    {
        if (IntrospectionFieldNames.TypeName.Equals(node.Name.Value, StringComparison.Ordinal))
        {
            if (node.Directives.Count > 0)
            {
                foreach (var directive in node.Directives)
                {
                    var result = Visit(directive, context);
                    if (result.IsBreak())
                    {
                        return result;
                    }
                }
            }

            return Skip;
        }

        if (context.Types.TryPeek(out var type)
            && type.NamedType() is IComplexTypeDefinition ot
            && ot.Fields.TryGetField(node.Name.Value, out var of))
        {
            context.OutputFields.Push(of);
            context.Types.Push(of.Type);
            return Continue;
        }

        context.UnexpectedErrorsDetected = true;
        return Skip;
    }

    protected override ISyntaxVisitorAction Leave(
        FieldNode node,
        DocumentValidatorContext context)
    {
        context.Types.Pop();
        context.OutputFields.Pop();
        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(
        DirectiveNode node,
        DocumentValidatorContext context)
    {
        if (context.Schema.DirectiveDefinitions.TryGetDirective(node.Name.Value, out var d))
        {
            context.Directives.Push(d);
            return Continue;
        }

        context.UnexpectedErrorsDetected = true;
        return Skip;
    }

    protected override ISyntaxVisitorAction Leave(
        DirectiveNode node,
        DocumentValidatorContext context)
    {
        context.Directives.Pop();
        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(
        ArgumentNode node,
        DocumentValidatorContext context)
    {
        // An argument of a fragment spread is declared by a variable definition of the
        // fragment it targets, not by a field or a directive.
        if (context.Path.Peek() is FragmentSpreadNode spread)
        {
            if (FragmentArguments.TryGetArgumentType(
                context,
                spread,
                node.Name.Value,
                out var argumentType))
            {
                context.Types.Push(argumentType);
                return Continue;
            }

            return Skip;
        }

        if (context.Directives.TryPeek(out var directive))
        {
            if (directive.Arguments.TryGetField(node.Name.Value, out var argument))
            {
                context.InputFields.Push(argument);
                context.Types.Push(argument.Type);
                return Continue;
            }
            context.UnexpectedErrorsDetected = true;
            return Skip;
        }

        if (context.OutputFields.TryPeek(out var field))
        {
            if (field.Arguments.TryGetField(node.Name.Value, out var argument))
            {
                context.InputFields.Push(argument);
                context.Types.Push(argument.Type);
                return Continue;
            }
        }

        context.UnexpectedErrorsDetected = true;
        return Skip;
    }

    protected override ISyntaxVisitorAction Leave(
        ArgumentNode node,
        DocumentValidatorContext context)
    {
        if (context.Path.Peek() is FragmentSpreadNode)
        {
            context.Types.Pop();
            return Continue;
        }

        context.InputFields.Pop();
        context.Types.Pop();
        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(
        ObjectFieldNode node,
        DocumentValidatorContext context)
    {
        if (context.Types.TryPeek(out var type)
            && type.NamedType() is IInputObjectTypeDefinition it
            && it.Fields.TryGetField(node.Name.Value, out var field))
        {
            context.InputFields.Push(field);
            context.Types.Push(field.Type);
            return Continue;
        }

        return Skip;
    }

    protected override ISyntaxVisitorAction Leave(
        ObjectFieldNode node,
        DocumentValidatorContext context)
    {
        context.InputFields.Pop();
        context.Types.Pop();
        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(
        VariableNode node,
        DocumentValidatorContext context)
    {
        // A usage that a fragment's own declaration satisfies neither needs an operation
        // variable to declare it, nor counts towards the usages of one it shadows.
        if (!context.IsFragmentVariable(node.Name.Value))
        {
            context.Features.GetRequired<VariableVisitorFeature>().Used.Add(node.Name.Value);
        }

        // A usage only counts for the fragment it appears in, not for one that spreads it.
        PeekFragmentVariableFrame(context)?.Used.Add(node.Name.Value);

        var parent = context.Path.Peek();

        var defaultValue = parent.Kind switch
        {
            SyntaxKind.Argument => GetArgumentDefaultValue(context, (ArgumentNode)parent),
            SyntaxKind.ObjectField => context.InputFields.Peek().DefaultValue,
            _ => null
        };

        var isOneOfVariable =
            parent is ObjectFieldNode
            && context.Types[^2].NullableType() is IInputObjectTypeDefinition inputObjectType
            && inputObjectType.Directives.ContainsName(DirectiveNames.OneOf.Name);

        if (context.Variables.TryGetValue(node.Name.Value, out var variableDefinition)
            && !IsVariableUsageAllowed(
                variableDefinition,
                context.Types.Peek(),
                isOneOfVariable,
                defaultValue))
        {
            context.ReportError(
                isOneOfVariable
                    ? context.OneOfVariableIsNotCompatible(node, variableDefinition)
                    : context.VariableIsNotCompatible(node, variableDefinition));
        }

        return Skip;
    }

    protected override ISyntaxVisitorAction Enter(
        ListValueNode node,
        DocumentValidatorContext context)
    {
        if (context.Types.TryPeek(out var type) && type.IsListType())
        {
            context.Types.Push(type.ElementType());
            return Continue;
        }
        return Break;
    }

    protected override ISyntaxVisitorAction Leave(
        ListValueNode node,
        DocumentValidatorContext context)
    {
        context.Types.Pop();
        return Continue;
    }

    private static IValueNode? GetArgumentDefaultValue(
        DocumentValidatorContext context,
        ArgumentNode argument)
    {
        // An argument of a fragment spread takes its default from the variable definition of
        // the fragment it targets.
        if (context.Path.Count > 1
            && context.Path[^2] is FragmentSpreadNode spread
            && FragmentArguments.TryGetVariableDefinition(
                context,
                spread,
                argument.Name.Value,
                out var variableDefinition))
        {
            return variableDefinition.DefaultValue;
        }

        return context.InputFields.Peek().DefaultValue;
    }

    // http://facebook.github.io/graphql/June2018/#IsVariableUsageAllowed()
    private bool IsVariableUsageAllowed(
        VariableDefinitionNode variableDefinition,
        IType locationType,
        bool isOneOfVariable,
        IValueNode? locationDefault)
    {
        if (IsNonNullPosition(locationType, isOneOfVariable)
            && !variableDefinition.Type.IsNonNullType())
        {
            if (variableDefinition.DefaultValue.IsNull()
                && locationDefault.IsNull())
            {
                return false;
            }

            return AreTypesCompatible(
                variableDefinition.Type,
                locationType.NullableType());
        }

        return AreTypesCompatible(
            variableDefinition.Type,
            locationType);
    }

    private static bool IsNonNullPosition(IType locationType, bool isOneOfVariable)
    {
        return locationType.IsNonNullType() || isOneOfVariable;
    }

    // http://facebook.github.io/graphql/June2018/#AreTypesCompatible()
    private bool AreTypesCompatible(
        ITypeNode variableType,
        IType locationType)
    {
        if (locationType.IsNonNullType())
        {
            if (variableType.IsNonNullType())
            {
                return AreTypesCompatible(
                    variableType.InnerType(),
                    locationType.InnerType());
            }
            return false;
        }

        if (variableType.IsNonNullType())
        {
            return AreTypesCompatible(
                variableType.InnerType(),
                locationType);
        }

        if (locationType.IsListType())
        {
            if (variableType.IsListType())
            {
                return AreTypesCompatible(
                    variableType.InnerType(),
                    locationType.InnerType());
            }
            return false;
        }

        if (variableType.IsListType())
        {
            return false;
        }

        if (variableType is NamedTypeNode vn
            && locationType is ITypeDefinition lt)
        {
            return string.Equals(
                vn.Name.Value,
                lt.Name,
                StringComparison.Ordinal);
        }

        return false;
    }

    private sealed class VariableVisitorFeature : ValidatorFeature
    {
        public HashSet<string> VariableNames { get; } = [];

        public List<FragmentVariableFrame> FragmentVariableFrames { get; } = [];

        public int FragmentVariableDepth { get; set; }

        public HashSet<string> Used { get; } = [];

        public HashSet<string> Declared { get; } = [];

        public HashSet<string> Unused { get; } = [];

        protected internal override void Reset()
        {
            VariableNames.Clear();
            FragmentVariableDepth = 0;

            // The frames themselves are kept for reuse, only their contents are released.
            for (var i = 0; i < FragmentVariableFrames.Count; i++)
            {
                var frame = FragmentVariableFrames[i];
                frame.Declared.Clear();
                frame.Used.Clear();
            }

            Used.Clear();
            Declared.Clear();
            Unused.Clear();
        }

        public sealed class FragmentVariableFrame
        {
            public HashSet<string> Declared { get; } = [];

            public HashSet<string> Used { get; } = [];
        }
    }
}
