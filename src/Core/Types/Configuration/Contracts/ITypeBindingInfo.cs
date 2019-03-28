using System.ComponentModel;
using System.Runtime.InteropServices.ComTypes;
using System;
using System.Reflection;
using HotChocolate.Resolvers;
using System.Collections.Generic;

namespace HotChocolate.Configuration
{
    internal interface ITypeBindingInfo
    {
        NameString Name { get; }

        Type SourceType { get; }

        bool TryGetFieldMember(NameString fieldName, out MemberInfo member);

        bool TryGetFieldProperty(NameString fieldName, out PropertyInfo prop);

        void TrackField(NameString fieldName);

        IEnumerable<RegisteredResolver> CreateResolvers();
    }
}
