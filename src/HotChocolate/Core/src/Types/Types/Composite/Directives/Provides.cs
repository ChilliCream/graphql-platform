#nullable enable

using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Composite;

[DirectiveType(
    DirectiveNames.Provides.Name,
    DirectiveLocation.FieldDefinition,
    IsRepeatable = false)]
public sealed class Provides
{
    public Provides(SelectionSetNode fields)
    {
        ArgumentNullException.ThrowIfNull(fields);
        Fields = fields;
    }

    public Provides(string fields)
    {
        ArgumentNullException.ThrowIfNull(fields);
        fields = $"{{ {fields.Trim('{', '}')} }}";
        Fields = Utf8GraphQLParser.Syntax.ParseSelectionSet(fields);
    }

    [GraphQLType<NonNullType<FieldSelectionSetType>>]
    public SelectionSetNode Fields { get; }

    public override string ToString()
        => $"@provides(fields: {Fields.ToString(false)[1..^1]})";
}

public static class ProvidesDirectiveExtensions
{
    public static IObjectFieldDescriptor Provides(
        this IObjectFieldDescriptor descriptor,
        string fields)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(fields);

        SelectionSetNode selectionSet;

        try
        {
            fields = $"{{ {fields.Trim('{', '}')} }}";
            selectionSet = Utf8GraphQLParser.Syntax.ParseSelectionSet(fields);
        }
        catch (SyntaxException ex)
        {
            descriptor.Extend().OnBeforeNaming(
                (ctx, _) => ctx.ReportError(
                    SchemaErrorBuilder.New()
                        .SetMessage("The field selection set syntax is invalid.")
                        .SetException(ex)
                        .Build()));
            return descriptor;
        }

        return descriptor.Directive(new Provides(selectionSet));
    }
}

public sealed class ProvidesAttribute : ObjectFieldDescriptorAttribute
{
    public ProvidesAttribute(string fields)
    {
        ArgumentNullException.ThrowIfNull(fields);
        Fields = fields;
    }

    public string Fields { get; }

    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectFieldDescriptor descriptor,
        MemberInfo member)
        => descriptor.Provides(Fields);
}
