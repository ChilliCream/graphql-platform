using System;
using System.Collections.Generic;
using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    internal class DirectiveRegistry
        : IDirectiveRegistry
    {
        private readonly List<Directive> _directives = new List<Directive>();

        public void RegisterDirective<T>()
            where T : Directive, new()
        {
            RegisterDirective(new T());
        }

        public void RegisterDirective<T>(T directive) where T : Directive
        {
            if (directive == null)
            {
                throw new ArgumentNullException(nameof(directive));
            }

            _directives.Add(directive);
        }

        public IEnumerable<Directive> GetDirectives()
        {
            return _directives;
        }
    }
}
