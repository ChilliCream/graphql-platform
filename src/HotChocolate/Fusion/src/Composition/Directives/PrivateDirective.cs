using System.Diagnostics.CodeAnalysis;
using HotChocolate.Skimmed;
using HotChocolate.Utilities;
using DirectiveLocation = HotChocolate.Skimmed.DirectiveLocation;

namespace HotChocolate.Fusion.Composition
{
    /// <summary>
    /// Represents the runtime value of 
    /// `directive @private on FIELD_DEFINITION`.
    /// </summary>
    internal sealed class PrivateDirective
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PrivateDirective"/> class.
        /// </summary>
        public PrivateDirective()
        {
        }

        /// <summary>
        /// Creates a <see cref="Directive"/> from this <see cref="PrivateDirective"/>.
        /// </summary>
        public Directive ToDirective(IFusionTypeContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            return new Directive(context.PrivateDirective);
        }

        /// <summary>
        /// Tries to parse a <see cref="PrivateDirective"/> from a <see cref="Directive"/>.
        /// </summary>
        public static bool TryParse(
            Directive directiveNode,
            IFusionTypeContext context,
            [NotNullWhen(true)] out PrivateDirective? directive)
        {
            ArgumentNullException.ThrowIfNull(directiveNode);
            ArgumentNullException.ThrowIfNull(context);

            if (!directiveNode.Name.EqualsOrdinal(context.PrivateDirective.Name))
            {
                directive = null;
                return false;
            }

            directive = new PrivateDirective();
            return true;
        }

        /// <summary>
        /// Creates the private directive type.
        /// </summary>
        public static DirectiveType CreateType()
        {
            return new DirectiveType(FusionTypeBaseNames.PrivateDirective)
            {
                Locations = DirectiveLocation.FieldDefinition
            };
        }
    }
}
