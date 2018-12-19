using System;
using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    internal class DirectiveTypeDescription
    {
        public DirectiveDefinitionNode SyntaxNode { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public Type ClrType { get; set; }

        public BindingBehavior ArgumentBindingBehavior { get; set; }

        public IDirectiveMiddleware Middleware { get; set; }
        public HashSet<DirectiveLocation> Locations { get; } =
            new HashSet<DirectiveLocation>();

        public List<DirectiveArgumentDescription> Arguments { get; } =
            new List<DirectiveArgumentDescription>();
    }
}
