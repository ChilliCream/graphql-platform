using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using HotChocolate.Language;

namespace HotChocolate.AspNetCore.Authorization
{
    public sealed class AuthorizeDirective
        : ISerializable
    {
        public AuthorizeDirective(
            IReadOnlyList<string> roles,
            ApplyPolicy apply = ApplyPolicy.BeforeResolver)
            : this(null, roles, apply)
        { }

        public AuthorizeDirective(
            string? policy = null,
            IReadOnlyList<string>? roles = null,
            ApplyPolicy apply = ApplyPolicy.BeforeResolver)
        {
            Policy = policy;
            Roles = roles;
            Apply = apply;
        }

        public AuthorizeDirective(SerializationInfo info, StreamingContext context)
        {
            var node = info.GetValue(
                nameof(DirectiveNode),
                typeof(DirectiveNode))
                as DirectiveNode;

            if (node == null)
            {
                Policy = info.GetString(nameof(Policy));
                Roles = info.GetValue(nameof(Roles), typeof(List<string>)) as List<string>;
                Apply = (ApplyPolicy)info.GetInt16(nameof(Apply));
            }
            else
            {
                ArgumentNode? policyArgument = node.Arguments
                    .FirstOrDefault(t => t.Name.Value == "policy");
                ArgumentNode? rolesArgument = node.Arguments
                    .FirstOrDefault(t => t.Name.Value == "roles");
                ArgumentNode? resolverArgument = node.Arguments
                    .FirstOrDefault(t => t.Name.Value == "apply");

                Policy = (policyArgument is { }
                    && policyArgument.Value is StringValueNode sv)
                    ? sv.Value
                    : null;

                if (rolesArgument is { })
                {
                    if (rolesArgument.Value is ListValueNode lv)
                    {
                        Roles = lv.Items.OfType<StringValueNode>()
                            .Select(t => t.Value?.Trim())
                            .Where(s => !string.IsNullOrEmpty(s))
                            .ToArray()!;
                    }
                    else if (rolesArgument.Value is StringValueNode svn)
                    {
                        Roles = new[] { svn.Value };
                    }
                }

                Apply = ApplyPolicy.BeforeResolver;
                if (resolverArgument is { }
                    && resolverArgument.Value.Value is string s
                    && s == "AFTER_RESOLVER")
                {
                    Apply = ApplyPolicy.AfterResolver;
                }
            }
        }

        /// <summary>
        /// Gets the policy name that determines access to the resource.
        /// </summary>
        public string? Policy { get; }

        /// <summary>
        /// Gets of roles that are allowed to access the resource.
        /// </summary>
        public IReadOnlyList<string>? Roles { get; }

        /// <summary>
        /// Gets a value indicating if the resolver has to be executed
        /// before the policy is run or after the policy is run.
        ///
        /// The before policy option is good if the actual object is needed
        /// for the policy to be evaluated.
        ///
        /// The default is BeforeResolver.
        /// </summary>
        public ApplyPolicy Apply { get; }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Policy), Policy);
            info.AddValue(nameof(Roles), Roles?.ToList());
            info.AddValue(nameof(Apply), (int)Apply);
        }
    }
}
