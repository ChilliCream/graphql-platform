using System.Collections.Immutable;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.CostAnalysis.WellKnownArgumentNames;
using DirectiveLocation = HotChocolate.Types.DirectiveLocation;

namespace HotChocolate.CostAnalysis.Types;

/// <summary>
/// The purpose of the <c>@listSize</c> directive is to either inform the static-analysis about the
/// size of returned lists (if that information is statically available), or to point the analysis
/// to where to find that information.
/// </summary>
/// <seealso href="https://ibm.github.io/graphql-specs/cost-spec.html#sec-The-List-Size-Directive">
/// Specification URL
/// </seealso>
public sealed class ListSizeDirectiveType : DirectiveType<ListSizeDirective>
{
    private const string _name = "listSize";

    protected override void Configure(IDirectiveTypeDescriptor<ListSizeDirective> descriptor)
    {
        descriptor
            .Name(_name)
            .Description(
                "The purpose of the `@listSize` directive is to either inform the static " +
                "analysis about the size of returned lists (if that information is statically " +
                "available), or to point the analysis to where to find that information.")
            .Location(DirectiveLocation.FieldDefinition);

        descriptor
            .Argument(t => t.AssumedSize)
            .Name(AssumedSize)
            .Type<IntType>()
            .Description(
                "The `assumedSize` argument can be used to statically define the maximum length " +
                "of a list returned by a field.");

        descriptor
            .Argument(t => t.SlicingArguments)
            .Name(SlicingArguments)
            .Type<ListType<NonNullType<StringType>>>()
            .Description(
                "The `slicingArguments` argument can be used to define which of the field's " +
                "arguments with numeric type are slicing arguments, so that their value " +
                "determines the size of the list returned by that field. It may specify a list " +
                "of multiple slicing arguments.");

        descriptor
            .Argument(t => t.SizedFields)
            .Name(SizedFields)
            .Type<ListType<NonNullType<StringType>>>()
            .Description(
                "The `sizedFields` argument can be used to define that the value of the " +
                "`assumedSize` argument or of a slicing argument does not affect the size of a " +
                "list returned by a field itself, but that of a list returned by one of its " +
                "sub-fields.");

        descriptor
            .Argument(t => t.RequireOneSlicingArgument)
            .Name(RequireOneSlicingArgument)
            .Type<NonNullType<BooleanType>>()
            .DefaultValue(true)
            .Description(
                "The `requireOneSlicingArgument` argument can be used to inform the static " +
                "analysis that it should expect that exactly one of the defined slicing " +
                "arguments is present in a query. If that is not the case (i.e., if none or " +
                "multiple slicing arguments are present), the static analysis may throw an error.");
    }

    protected override Func<DirectiveNode, object> OnCompleteParse(
        ITypeCompletionContext context,
        DirectiveTypeDefinition definition)
        => ParseLiteral;

    private static object ParseLiteral(DirectiveNode directiveNode)
    {
        int? assumedSize = null;
        var slicingArguments = ImmutableArray<string>.Empty;
        var sizedFields = ImmutableArray<string>.Empty;
        var requireOneSlicingArgument = false;

        foreach (var argument in directiveNode.Arguments)
        {
            if (argument.Value is NullValueNode)
            {
                continue;
            }

            switch (argument.Name.Value)
            {
                case AssumedSize:
                    assumedSize = argument.Value.ExpectInt();
                    break;

                case SlicingArguments:
                    slicingArguments = argument.Value.ExpectStringList();
                    break;

                case SizedFields:
                    sizedFields = argument.Value.ExpectStringList();
                    break;

                case RequireOneSlicingArgument:
                    requireOneSlicingArgument = argument.Value.ExpectBoolean();
                    break;

                default:
                    throw new InvalidOperationException("Invalid argument name.");
            }
        }

        return new ListSizeDirective(assumedSize, slicingArguments, sizedFields, requireOneSlicingArgument);
    }

    protected override Func<object, DirectiveNode> OnCompleteFormat(
        ITypeCompletionContext context,
        DirectiveTypeDefinition definition)
        => FormatValue;

    private static DirectiveNode FormatValue(object value)
    {
        if(value is not ListSizeDirective directive)
        {
            throw new InvalidOperationException("The value is not a list size directive.");
        }

        var arguments = ImmutableArray.CreateBuilder<ArgumentNode>();

        if (directive.AssumedSize is not null)
        {
            arguments.Add(new ArgumentNode(AssumedSize, directive.AssumedSize.Value));
        }

        if (directive.SlicingArguments.Length > 0)
        {
            arguments.Add(new ArgumentNode(SlicingArguments, directive.SlicingArguments.ToListValueNode()));
        }

        if (directive.SizedFields.Length > 0)
        {
            arguments.Add(new ArgumentNode(SizedFields, directive.SizedFields.ToListValueNode()));
        }

        arguments.Add(new ArgumentNode(RequireOneSlicingArgument, directive.RequireOneSlicingArgument));

        return new DirectiveNode(_name, arguments.ToImmutableArray());
    }
}

file static class Extensions
{
    public static int ExpectInt(this IValueNode value)
    {
        if (value is IntValueNode intValue)
        {
            return intValue.ToInt32();
        }

        throw new InvalidOperationException("The value is not an int value.");
    }

    public static bool ExpectBoolean(this IValueNode value)
    {
        if (value is BooleanValueNode booleanValue)
        {
            return booleanValue.Value;
        }

        throw new InvalidOperationException("The value is not a boolean value.");
    }

    public static ImmutableArray<string> ExpectStringList(this IValueNode value)
    {
        if (value is ListValueNode listValue)
        {
            var builder = ImmutableArray.CreateBuilder<string>(listValue.Items.Count);

            foreach (var item in listValue.Items)
            {
                if (item is not StringValueNode stringValue)
                {
                    throw new InvalidOperationException("The value is not a list of strings.");
                }

                builder.Add(stringValue.Value);
            }

            return builder.MoveToImmutable();
        }

        throw new InvalidOperationException("The value is not a list of strings.");
    }

    public static ListValueNode ToListValueNode(this ImmutableArray<string> values)
    {
        var items = ImmutableArray.CreateBuilder<IValueNode>(values.Length);

        foreach (var value in values)
        {
            items.Add(new StringValueNode(value));
        }

        return new ListValueNode(items.MoveToImmutable());
    }
}
