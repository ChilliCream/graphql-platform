using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using HotChocolate.Language;

#if !ASPNETCLASSIC
using System.Collections.ObjectModel;
#endif

#if ASPNETCLASSIC
namespace HotChocolate.AspNetClassic.Authorization
#else
namespace HotChocolate.AspNetCore.Authorization
#endif
{
    public class AuthorizeDirective
        : ISerializable
    {
        public AuthorizeDirective()
        {
            Roles = Array.Empty<string>();
        }

#if ASPNETCLASSIC
        public AuthorizeDirective(IEnumerable<string> roles)
        {
            if (roles == null)
            {
                throw new ArgumentNullException(nameof(roles));
            }

            Roles = roles.ToArray();
        }

        public AuthorizeDirective(
            SerializationInfo info,
            StreamingContext context)
        {
            var node = info.GetValue(
                nameof(DirectiveNode),
                typeof(DirectiveNode))
                as DirectiveNode;

            if (node == null)
            {
                Roles = (string[])info.GetValue(
                    nameof(Roles),
                    typeof(string[]));
            }
            else
            {
                ArgumentNode rolesArgument = node.Arguments
                    .FirstOrDefault(t => t.Name.Value == "roles");

                Roles = Array.Empty<string>();
                if (rolesArgument != null)
                {
                    if (rolesArgument.Value is ListValueNode lv)
                    {
                        Roles = lv.Items.OfType<StringValueNode>()
                            .Select(t => t.Value?.Trim())
                            .Where(s => !string.IsNullOrEmpty(s))
                            .ToArray();
                    }
                    else if (rolesArgument.Value is StringValueNode svn)
                    {
                        Roles = new[] { svn.Value };
                    }
                }
            }
        }

#else
        public AuthorizeDirective(string policy)
            : this(policy, null)
        { }

        public AuthorizeDirective(IReadOnlyCollection<string> roles)
            : this(null, roles)
        { }

        public AuthorizeDirective(
            string policy,
            IReadOnlyCollection<string> roles)
        {
            Policy = policy;
            Roles = roles ?? Array.Empty<string>();
        }

        public AuthorizeDirective(
            SerializationInfo info,
            StreamingContext context)
        {
            var node = info.GetValue(
                nameof(DirectiveNode),
                typeof(DirectiveNode))
                as DirectiveNode;

            if (node == null)
            {
                Policy = info.GetString(nameof(Policy));
                Roles = (string[])info.GetValue(
                    nameof(Roles),
                    typeof(string[]));
            }
            else
            {
                ArgumentNode policyArgument = node.Arguments
                    .FirstOrDefault(t => t.Name.Value == "policy");
                ArgumentNode rolesArgument = node.Arguments
                    .FirstOrDefault(t => t.Name.Value == "roles");

                Policy = (policyArgument != null
                    && policyArgument.Value is StringValueNode sv)
                    ? sv.Value
                    : null;

                Roles = Array.Empty<string>();
                if (rolesArgument != null)
                {
                    if (rolesArgument.Value is ListValueNode lv)
                    {
                        Roles = lv.Items.OfType<StringValueNode>()
                            .Select(t => t.Value?.Trim())
                            .Where(s => !string.IsNullOrEmpty(s))
                            .ToArray();
                    }
                    else if (rolesArgument.Value is StringValueNode svn)
                    {
                        Roles = new[] { svn.Value };
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the policy name that determines access to the resource.
        /// </summary>
        public string Policy { get; }
#endif

        /// <summary>
        /// Gets or sets of roles that are allowed to access the resource.
        /// </summary>
        public IReadOnlyCollection<string> Roles { get; }


        public void GetObjectData(
            SerializationInfo info,
            StreamingContext context)
        {
#if !ASPNETCLASSIC
            info.AddValue(nameof(Policy), Policy);
#endif
            info.AddValue(nameof(Roles), Roles?.ToArray());
        }
    }
}
