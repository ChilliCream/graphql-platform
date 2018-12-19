using System;
using System.Collections.Generic;
using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    internal interface IDirectiveRegistry
    {
        void RegisterDirectiveType<T>() where T : DirectiveType, new();

        void RegisterDirectiveType(Type type);


        void RegisterDirectiveType<T>(T directive) where T : DirectiveType;

        IReadOnlyCollection<DirectiveType> GetDirectiveTypes();

        DirectiveType GetDirectiveType(DirectiveReference directiveReference);
    }
}
