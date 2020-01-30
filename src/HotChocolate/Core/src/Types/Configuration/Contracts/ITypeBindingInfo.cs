using System;
using System.Reflection;
using System.Collections.Generic;
using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    internal interface ITypeBindingInfo
    {
        NameString Name { get; }

        Type SourceType { get; }

        bool TryGetFieldMember(
            NameString fieldName,
            MemberKind kind, out
            MemberInfo member);

        bool TryGetFieldProperty(
            NameString fieldName,
            MemberKind kind,
            out PropertyInfo prop);

        void TrackField(NameString fieldName);

        IEnumerable<RegisteredResolver> CreateResolvers();
    }
}
