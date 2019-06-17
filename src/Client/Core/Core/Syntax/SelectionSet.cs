using System;
using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Client.Core.Utilities;

namespace HotChocolate.Client.Core.Syntax
{
    public class SelectionSet : ISelectionSet
    {
        private static readonly IEnumerable<ISyntaxNode> EmptySelections = new ISyntaxNode[0];

        public SelectionSet()
        {
            Selections = new List<ISyntaxNode>();
        }

        public IList<ISyntaxNode> Selections { get; }

        public static string GetIdentifier(MemberInfo member)
        {
            var attr = member?.GetCustomAttribute<GraphQLIdentifierAttribute>();
            return attr != null ? attr.Identifier : member?.Name.LowerFirstCharacter();
        }

        public static string GetIdentifier(Type type)
        {
            var attr = type.GetTypeInfo().GetCustomAttribute<GraphQLIdentifierAttribute>();

            if (attr != null)
            {
                return attr.Identifier;
            }
            else if (type.GetTypeInfo().IsInterface)
            {
                return type.Name.Substring(1);
            }
            else
            {
                return type.Name;
            }
        }
    }
}
