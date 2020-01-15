using System;
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
            ExecuteResolver executeResolver = ExecuteResolver.AfterPolicy)
            : this(null, roles, executeResolver)
        { }

        public AuthorizeDirective(
            string? policy = null,
            IReadOnlyList<string>? roles = null,
            ExecuteResolver executeResolver = ExecuteResolver.AfterPolicy)
        {
            Policy = policy;
            Roles = roles ?? Array.Empty<string>();
            ExecuteResolver = executeResolver;
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
                Roles = (List<string>)info.GetValue(nameof(Roles), typeof(List<string>))!;
                ExecuteResolver = (ExecuteResolver)info.GetInt16(nameof(ExecuteResolver));
            }
            else
            {
                ArgumentNode policyArgument = node.Arguments
                    .FirstOrDefault(t => t.Name.Value == "policy");
                ArgumentNode rolesArgument = node.Arguments
                    .FirstOrDefault(t => t.Name.Value == "roles");
                ArgumentNode resolverArgument = node.Arguments
                    .FirstOrDefault(t => t.Name.Value == "executeResolver");

                Policy = (policyArgument is { }
                    && policyArgument.Value is StringValueNode sv)
                    ? sv.Value
                    : null;

                Roles = Array.Empty<string>();
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

                ExecuteResolver = ExecuteResolver.AfterPolicy;
                if (resolverArgument is { }
                    && resolverArgument.Value.Value is string s
                    && s == "BEFORE_POLICY")
                {
                    ExecuteResolver = ExecuteResolver.BeforePolicy;
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
        public IReadOnlyList<string> Roles { get; }

        /// <summary>
        /// Gets a value indicating if the resolver has to be executed
        /// before the policy is run or after the policy has run.
        ///
        /// The before policy option is good if the actual object is needed
        /// for the policy to be evaluated.
        ///
        /// The default is AfterPolicy.
        /// </summary>
        public ExecuteResolver ExecuteResolver { get; }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Policy), Policy);
            info.AddValue(nameof(Roles), Roles.ToList());
            info.AddValue(nameof(ExecuteResolver), (int)ExecuteResolver);
        }
    }
}
