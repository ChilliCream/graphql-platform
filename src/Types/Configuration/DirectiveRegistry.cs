using System;
using System.Collections.Generic;
using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    internal class DirectiveRegistry
        : IDirectiveRegistry
    {
        private readonly List<DirectiveType> _directives = new List<DirectiveType>();

        public void RegisterDirective<T>()
            where T : DirectiveType, new()
        {
            RegisterDirective(new T());
        }

        public void RegisterDirective<T>(T directive) where T : DirectiveType
        {
            if (directive == null)
            {
                throw new ArgumentNullException(nameof(directive));
            }

            _directives.Add(directive);
        }

        public IReadOnlyCollection<DirectiveType> GetDirectives()
        {
            return _directives;
        }
    }
}
