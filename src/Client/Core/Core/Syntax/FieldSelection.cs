using System;
using System.Collections.Generic;
using System.Reflection;

namespace HotChocolate.Client.Core.Syntax
{
    public class FieldSelection : SelectionSet
    {
        public FieldSelection(MemberInfo member, MemberInfo alias)
            : this(GetIdentifier(member), GetIdentifier(alias))
        {
        }

        public FieldSelection(string name, string alias)
        {
            Name = name;
            Arguments = new List<Argument>();
            Alias = alias != name ? alias : null;
        }

        public string Name { get; }
        public IList<Argument> Arguments { get; }
        public string Alias { get; private set; }

        public void SetAlias(MemberInfo member)
        {
            var alias = GetIdentifier(member);

            if (Name != alias)
            {
                Alias = alias;
            }
        }
    }
}
