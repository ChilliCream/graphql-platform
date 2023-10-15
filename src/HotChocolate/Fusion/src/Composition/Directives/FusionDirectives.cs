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
    /// `directive @fusion(
    ///     prefix: String,
    ///     prefixSelf: Boolean! = false,
    ///     version: Version!
    /// ) on SCHEMA`.
    /// </summary>
    internal sealed class FusionDirective
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FusionDirective"/> class.
        /// </summary>
        public FusionDirective(string? prefix = null, bool prefixSelf = false, string version = "2023-12")
        {
            Prefix = prefix;
            PrefixSelf = prefixSelf;
            Version = version;
        }

        /// <summary>
        /// Gets the prefix.
        /// </summary>
        public string? Prefix { get; }

        /// <summary>
        /// Gets a value indicating whether to prefix self.
        /// </summary>
        public bool PrefixSelf { get; }

        /// <summary>
        /// Gets the version.
        /// </summary>
        public string Version { get; }

        /// <summary>
        /// Creates a <see cref="Directive"/> from this <see cref="FusionDirective"/>.
        /// </summary>
        public Directive ToDirective(IFusionTypeContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            var argCount = 1;
            
            if(Prefix is not null)
            {
                argCount++;
            }
            
            if(PrefixSelf)
            {
                argCount++;
            }

            var args = new Argument[argCount];
            args[0] = new Argument(VersionArg, Version);

            if (Prefix is not null)
            {
                args[1] = new Argument(PrefixArg, Prefix);
            }
            
            if(PrefixSelf)
            {
                args[2] = new Argument(PrefixSelfArg, PrefixSelf);
            }
            
            return new Directive(context.FusionDirective, args);
        }

        /// <summary>
        /// Tries to parse a <see cref="FusionDirective"/> from a <see cref="Directive"/>.
        /// </summary>
        public static bool TryParse(
            Directive directiveNode,
            IFusionTypeContext context,
            [NotNullWhen(true)] out FusionDirective? directive)
        {
            ArgumentNullException.ThrowIfNull(directiveNode);
            ArgumentNullException.ThrowIfNull(context);

            if (!directiveNode.Name.EqualsOrdinal(context.FusionDirective.Name))
            {
                directive = null;
                return false;
            }

            var prefix = directiveNode.Arguments.GetValueOrDefault(PrefixArg)?.ExpectStringLiteral().Value;
            var prefixSelf = directiveNode.Arguments.GetValueOrDefault(PrefixSelfArg)?.ExpectBooleanValue().Value;
            var version = directiveNode.Arguments.GetValueOrDefault(VersionArg)?.ExpectStringLiteral().Value;
            
            if (prefixSelf is null || version is null)
            {
                directive = null;
                return false;
            }

            directive = new FusionDirective(prefix, prefixSelf ?? false, version);
            return true;
        }

        /// <summary>
        /// Creates the fusion directive type.
        /// </summary>
        public static DirectiveType CreateType()
        {
            var stringType = new MissingType("String");
            var booleanType = new MissingType("Boolean");
            
            var directiveType = new DirectiveType(FusionTypeBaseNames.FusionDirective)
            {
                Locations = DirectiveLocation.Schema,
                Arguments =
                {
                    new InputField(PrefixArg, stringType),
                    new InputField(PrefixSelfArg, new NonNullType(booleanType))
                    {
                        DefaultValue = new BooleanValueNode(false)
                    },
                    new InputField(VersionArg, new NonNullType(stringType))
                }
            };

            return directiveType;
        }
    }
}
