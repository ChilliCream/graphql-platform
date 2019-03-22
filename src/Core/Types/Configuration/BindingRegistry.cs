using System.Runtime.InteropServices.ComTypes;
using System;
using System.Reflection;
using HotChocolate.Configuration.Bindings;
using HotChocolate.Resolvers;
using System.Collections.Generic;

namespace HotChocolate.Configuration
{
    internal interface IBindingCompiler
    {
        bool CanHandle(IBindingInfo binding);

        void AddBinding(IBindingInfo binding);

        IBindingLookup Compile();
    }

    internal interface IBindingLookup
    {
        IReadOnlyList<ITypeBindingInfo> Bindings { get; }

        ITypeBindingInfo GetBindingInfo(NameString typeName);
    }

    internal interface ITypeBindingInfo
    {
        NameString Name { get; }

        Type SourceType { get; }

        bool TryGetFieldMember(NameString fieldName, out MemberInfo member);

        void TrackField(NameString fieldName);

        IEnumerable<RegisteredResolver> CreateResolvers();
    }



    internal class BindingRegistry
    {

    }
}
