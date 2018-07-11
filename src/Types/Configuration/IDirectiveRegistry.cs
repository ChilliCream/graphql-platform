using System.Collections.Generic;
using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    internal interface IDirectiveRegistry
    {
        void RegisterDirective<T>() where T : Directive, new();

        void RegisterDirective<T>(T directive) where T : Directive;

        IEnumerable<Directive> GetDirectives();
    }
}
