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
        /// Gets all @private directives from the specified member.
        /// </summary>
        /// <param name="member">
        /// The member that shall be checked.
        /// </param>
        /// <param name="context">
        /// The fusion type context that provides the directive names.
        /// </param>
        /// <returns>
        /// Returns all @private directives.
        /// </returns>
        public static IEnumerable<PrivateDirective> GetAllFrom(
            IHasDirectives member,
            IFusionTypeContext context)
        {
            foreach (var directive in member.Directives[context.PrivateDirective.Name])
            {
                if (TryParse(directive, context, out var privateDirective))
                {
                    yield return privateDirective;
                }
            }
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
