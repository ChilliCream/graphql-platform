using System;
using System.Collections.Generic;
using System.Text;

namespace HotChocolate.Client.Core.Syntax
{
    public class FragmentDefinition : SelectionSet
    {
        public string TypeCondition { get; }
        public string Name { get; }

        public FragmentDefinition(Type typeCondition, string name)
        {
            if (name.ToLowerInvariant() == "on")
            {
                throw new ArgumentException("'on' is an invalid fragment name", nameof(name));
            }

            TypeCondition = GetIdentifier(typeCondition);
            Name = name;
        }
    }
}
