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
    /// `directive @variable(
    ///     name: Name!
    ///     select: Selection!
    ///     subgraph: Name!
    /// ) repeatable on OBJECT | FIELD_DEFINITION`.
    /// </summary>
    internal sealed class VariableDirective : IEquatable<VariableDirective>
    {
        public VariableDirective(string name, FieldNode select, string subgraph)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Select = select ?? throw new ArgumentNullException(nameof(select));
            Subgraph = subgraph ?? throw new ArgumentNullException(nameof(subgraph));
        }

        public string Name { get; }

        public FieldNode Select { get; }

        public string Subgraph { get; }
        
        public bool Equals(VariableDirective? other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }
            
            return Name == other.Name && 
                Select.Equals(other.Select, SyntaxComparison.Syntax) && 
                Subgraph == other.Subgraph;
        }

        public override bool Equals(object? obj)
            => ReferenceEquals(this, obj) || obj is VariableDirective other && Equals(other);

        public override int GetHashCode()
            => HashCode.Combine(Name, SyntaxComparer.BySyntax.GetHashCode(Select), Subgraph);

        public Directive ToDirective(IFusionTypeContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            var args = new[]
            {
                new Argument(NameArg, Name),
                new Argument(SelectArg, new StringValueNode(Select.ToString(false))),
                new Argument(SubgraphArg, Subgraph)
            };

            return new Directive(context.VariableDirective, args);
        }

        public static bool TryParse(
            Directive directiveNode,
            IFusionTypeContext context,
            [NotNullWhen(true)] out VariableDirective? directive)
        {
            ArgumentNullException.ThrowIfNull(directiveNode);
            ArgumentNullException.ThrowIfNull(context);

            if (!directiveNode.Name.EqualsOrdinal(context.VariableDirective.Name))
            {
                directive = null;
                return false;
            }

            var name = directiveNode.Arguments.GetValueOrDefault(NameArg)?.ExpectStringLiteral().Value;
            var select = directiveNode.Arguments.GetValueOrDefault(SelectArg)?.ExpectFieldSelection();
            var subgraph = directiveNode.Arguments.GetValueOrDefault(SubgraphArg)?.ExpectStringLiteral().Value;

            if (name is null || select is null || subgraph is null)
            {
                directive = null;
                return false;
            }

            directive = new VariableDirective(name,  select, subgraph);
            return true;
        }

        public static DirectiveType CreateType()
        {
            var nameType = new MissingType(FusionTypeBaseNames.Name);
            var selectionSetType = new MissingType(FusionTypeBaseNames.SelectionSet);

            return new DirectiveType("variable")
            {
                Locations = DirectiveLocation.Object | 
                    DirectiveLocation.FieldDefinition,
                IsRepeatable = true,
                Arguments =
                {
                    new InputField(NameArg, new NonNullType(nameType)),
                    new InputField(SelectArg, new NonNullType(selectionSetType)),
                    new InputField(SubgraphArg, new NonNullType(nameType))
                }
            };
        }
    }
}
