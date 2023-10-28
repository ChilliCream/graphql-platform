using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Skimmed;
using HotChocolate.Utilities;
using static HotChocolate.Fusion.FusionDirectiveArgumentNames;
using DirectiveLocation = HotChocolate.Skimmed.DirectiveLocation;

namespace HotChocolate.Fusion.Composition
{
    /// <summary>
    /// Represents the runtime value of 
    /// `directive @resolver(
    ///     operation: OperationDefinition!
    ///     kind: ResolverKind
    ///     subgraph: Name!
    /// ) repeatable on OBJECT | FIELD_DEFINITION`.
    /// </summary>
    internal sealed class ResolverDirective
    {
        public ResolverDirective(OperationDefinitionNode operation, ResolverKind kind, string subgraph)
        {
            Operation = operation ?? throw new ArgumentNullException(nameof(operation));
            Kind = kind;
            Subgraph = subgraph ?? throw new ArgumentNullException(nameof(subgraph));
        }

        public OperationDefinitionNode Operation { get; }

        public ResolverKind Kind { get; }

        public string Subgraph { get; }

        public Directive ToDirective(IFusionTypeContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            var args = new[]
            {
                new Argument(OperationArg, new StringValueNode(Operation.ToString(false))),
                new Argument(KindArg, new EnumValueNode(Kind.ToString().ToUpperInvariant())),
                new Argument(SubgraphArg, Subgraph)
            };

            return new Directive(context.ResolverDirective, args);
        }

        public static bool TryParse(
            Directive directiveNode,
            IFusionTypeContext context,
            [NotNullWhen(true)] out ResolverDirective? directive)
        {
            ArgumentNullException.ThrowIfNull(directiveNode);
            ArgumentNullException.ThrowIfNull(context);

            if (!directiveNode.Name.EqualsOrdinal(context.ResolverDirective.Name))
            {
                directive = null;
                return false;
            }

            var operation = directiveNode.Arguments.GetValueOrDefault(OperationArg)?.ExpectOperationDefinition();
            var subgraph = directiveNode.Arguments.GetValueOrDefault(SubgraphArg)?.ExpectStringLiteral().Value;
            if (operation is null || subgraph is null)
            {
                directive = null;
                return false;
            }

            var kindString = directiveNode.Arguments.GetValueOrDefault(KindArg)?.ExpectStringLiteral().Value;
            if(kindString is null || !Enum.TryParse<ResolverKind>(kindString, ignoreCase: true, out var kind))
            {
                directive = null;
                return false;
            }

            directive = new ResolverDirective(operation, kind, subgraph);
            return true;
        }

        public static DirectiveType CreateType()
        {
            var operationDefinitionType = new MissingType(FusionTypeBaseNames.OperationDefinition);
            var resolverKindType = new MissingType(FusionTypeBaseNames.ResolverKind);
            var nameType = new MissingType(FusionTypeBaseNames.Name);

            return new DirectiveType(FusionTypeBaseNames.ResolverDirective)
            {
                IsRepeatable = true,
                Locations = DirectiveLocation.Object | 
                    DirectiveLocation.FieldDefinition,
                Arguments =
                {
                    new InputField(OperationArg, new NonNullType(operationDefinitionType)),
                    new InputField(KindArg, resolverKindType),
                    new InputField(SubgraphArg, new NonNullType(nameType))
                }
            };
        }
    }
}