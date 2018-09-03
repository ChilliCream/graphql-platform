using System.Collections.Generic;
using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    internal interface IDirectiveRegistry
    {
        void RegisterDirective<T>() where T : DirectiveType, new();

        void RegisterDirective<T>(T directive) where T : DirectiveType;

        IReadOnlyCollection<DirectiveType> GetDirectives();
    }
}
