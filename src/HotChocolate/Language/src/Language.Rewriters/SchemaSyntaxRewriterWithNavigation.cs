namespace HotChocolate.Language.Rewriters;

public class SchemaSyntaxRewriterWithNavigation<TContext>
    : SchemaSyntaxRewriter<TContext>
{
    protected override DocumentNode RewriteDocument(
        DocumentNode node,
        TContext context)
    {
        Push(node, context);

        try
        {
            return base.RewriteDocument(node, context);
        }
        finally
        {
            Pop(context);
        }
    }

    protected override ITypeSystemDefinitionNode RewriteTypeDefinition(
        ITypeSystemDefinitionNode node,
        TContext context)
    {
        Push(node, context);

        try
        {
            return base.RewriteTypeDefinition(node, context);
        }
        finally
        {
            Pop(context);
        }
    }

    protected override ITypeSystemExtensionNode RewriteTypeExtensionDefinition(
        ITypeSystemExtensionNode node,
        TContext context)
    {
        Push(node, context);

        try
        {
            return base.RewriteTypeExtensionDefinition(node, context);
        }
        finally
        {
            Pop(context);
        }
    }

    protected override FieldDefinitionNode RewriteFieldDefinition(
        FieldDefinitionNode node,
        TContext context)
    {
        Push(node, context);

        try
        {
            return base.RewriteFieldDefinition(node, context);
        }
        finally
        {
            Pop(context);
        }
    }

    protected override InputValueDefinitionNode RewriteInputValueDefinition(
        InputValueDefinitionNode node,
        TContext context)
    {
        Push(node, context);

        try
        {
            return base.RewriteInputValueDefinition(node, context);
        }
        finally
        {
            Pop(context);
        }
    }

    protected override EnumValueDefinitionNode RewriteEnumValueDefinition(
       EnumValueDefinitionNode node,
       TContext context)
    {
        Push(node, context);

        try
        {
            return base.RewriteEnumValueDefinition(node, context);
        }
        finally
        {
            Pop(context);
        }

    }

    protected override NameNode RewriteName(
        NameNode node,
        TContext context)
    {
        Push(node, context);

        try
        {
            return base.RewriteName(node, context);
        }
        finally
        {
            Pop(context);
        }
    }

    protected override VariableNode RewriteVariable(
        VariableNode node,
        TContext context)
    {
        Push(node, context);

        try
        {
            return base.RewriteVariable(node, context);
        }
        finally
        {
            Pop(context);
        }
    }

    protected override ArgumentNode RewriteArgument(
        ArgumentNode node,
        TContext context)
    {
        Push(node, context);

        try
        {
            return base.RewriteArgument(node, context);
        }
        finally
        {
            Pop(context);
        }

    }

    protected override IntValueNode RewriteIntValue(
        IntValueNode node,
        TContext context)
    {
        Push(node, context);

        try
        {
            return base.RewriteIntValue(node, context);
        }
        finally
        {
            Pop(context);
        }
    }

    protected override FloatValueNode RewriteFloatValue(
        FloatValueNode node,
        TContext context)
    {
        Push(node, context);

        try
        {
            return base.RewriteFloatValue(node, context);
        }
        finally
        {
            Pop(context);
        }
    }

    protected override StringValueNode RewriteStringValue(
        StringValueNode node,
        TContext context)
    {
        Push(node, context);

        try
        {
            return base.RewriteStringValue(node, context);
        }
        finally
        {
            Pop(context);
        }
    }

    protected override BooleanValueNode RewriteBooleanValue(
        BooleanValueNode node,
        TContext context)
    {
        Push(node, context);

        try
        {
            return base.RewriteBooleanValue(node, context);
        }
        finally
        {
            Pop(context);
        }
    }

    protected override EnumValueNode RewriteEnumValue(
        EnumValueNode node,
        TContext context)
    {
        Push(node, context);

        try
        {
            return base.RewriteEnumValue(node, context);
        }
        finally
        {
            Pop(context);
        }
    }

    protected override NullValueNode RewriteNullValue(
        NullValueNode node,
        TContext context)
    {
        Push(node, context);

        try
        {
            return base.RewriteNullValue(node, context);
        }
        finally
        {
            Pop(context);
        }
    }

    protected override ListValueNode RewriteListValue(
        ListValueNode node,
        TContext context)
    {
        Push(node, context);

        try
        {
            return base.RewriteListValue(node, context);
        }
        finally
        {
            Pop(context);
        }
    }

    protected override ObjectValueNode RewriteObjectValue(
        ObjectValueNode node,
        TContext context)
    {
        Push(node, context);

        try
        {
            return base.RewriteObjectValue(node, context);
        }
        finally
        {
            Pop(context);
        }
    }

    protected override ObjectFieldNode RewriteObjectField(
        ObjectFieldNode node,
        TContext context)
    {
        Push(node, context);

        try
        {
            return base.RewriteObjectField(node, context);
        }
        finally
        {
            Pop(context);
        }
    }

    protected override DirectiveNode RewriteDirective(
        DirectiveNode node,
        TContext context)
    {
        Push(node, context);

        try
        {
            return base.RewriteDirective(node, context);
        }
        finally
        {
            Pop(context);
        }
    }

    protected override NamedTypeNode RewriteNamedType(
        NamedTypeNode node,
        TContext context)
    {
        Push(node, context);

        try
        {
            return base.RewriteNamedType(node, context);
        }
        finally
        {
            Pop(context);
        }
    }

    protected override ListTypeNode RewriteListType(
        ListTypeNode node,
        TContext context)
    {
        Push(node, context);

        try
        {
            return base.RewriteListType(node, context);
        }
        finally
        {
            Pop(context);
        }
    }

    protected override NonNullTypeNode RewriteNonNullType(
        NonNullTypeNode node,
        TContext context)
    {
        Push(node, context);

        try
        {
            return base.RewriteNonNullType(node, context);
        }
        finally
        {
            Pop(context);
        }
    }

    protected override IValueNode RewriteValue(
        IValueNode node, TContext context)
    {
        Push(node, context);

        try
        {
            return base.RewriteValue(node, context);
        }
        finally
        {
            Pop(context);
        }
    }

    protected override ITypeNode RewriteType(
        ITypeNode node,
        TContext context)
    {
        Push(node, context);

        try
        {
            return base.RewriteType(node, context);
        }
        finally
        {
            Pop(context);
        }
    }

    protected virtual void Push(ISyntaxNode node, TContext context)
    {
        if (context is not IHasNavigator
            {
                Navigator: { } navigator
            })
        {
            return;
        }

        navigator.Push(node);
    }

    protected virtual void Pop(TContext context)
    {
        if (context is not IHasNavigator
            {
                Navigator: { } navigator
            })
        {
            return;
        }

        navigator.Pop(out _);
    }
}
