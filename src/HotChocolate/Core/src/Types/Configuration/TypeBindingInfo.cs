using System.Reflection;
using System.Linq;
using System;
using System.Collections.Generic;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types;
using HotChocolate.Resolvers;

namespace HotChocolate.Configuration
{
    internal sealed class TypeBindingInfo
        : ITypeBindingInfo
    {
        private readonly IDescriptorContext _context;
        private readonly Dictionary<NameString, RegisteredResolver> _resolvers;
        private readonly Dictionary<NameString, MemberInfo> _members;
        private readonly BindingBehavior _bindingBehavior;
        private readonly HashSet<NameString> _fieldNames =
            new HashSet<NameString>();
        private List<MemberInfo> _allMembers;        

        public TypeBindingInfo(
            IDescriptorContext context,
            NameString name,
            Type sourceType,
            BindingBehavior bindingBehavior,
            IDictionary<NameString, RegisteredResolver> resolvers,
            IDictionary<NameString, MemberInfo> members)
        {
            if (resolvers is null)
            {
                throw new ArgumentNullException(nameof(resolvers));
            }

            if (members is null)
            {
                throw new ArgumentNullException(nameof(members));
            }

            name.EnsureNotEmpty(nameof(name));

            _context = context
                ?? throw new ArgumentNullException(nameof(context));
            Name = name;
            SourceType = sourceType;
            _bindingBehavior = bindingBehavior;
            _resolvers = new Dictionary<NameString, RegisteredResolver>(
                resolvers);
            _members = new Dictionary<NameString, MemberInfo>(
                members);
        }

        public NameString Name { get; }

        public Type SourceType { get; }

        public IEnumerable<RegisteredResolver> CreateResolvers()
        {
            return _resolvers.Values;
        }

        public void TrackField(NameString fieldName)
        {
            if (fieldName.HasValue)
            {
                _fieldNames.Add(fieldName);
            }
        }

        public bool TryGetFieldMember(
            NameString fieldName,
            MemberKind kind,
            out MemberInfo member)
        {
            fieldName.EnsureNotEmpty(nameof(fieldName));

            if (_members.TryGetValue(fieldName, out member))
            {
                return true;
            }

            if (SourceType != typeof(object)
                && _fieldNames.Contains(fieldName))
            {
                InitializeAllMembers();
                member = _allMembers.FirstOrDefault(t =>
                    _context.Naming.GetMemberName(t, kind)
                        .Equals(fieldName));

                if (member != null)
                {
                    TryRegisterResolver(fieldName, member);
                    return true;
                }
            }

            return false;
        }

        public bool TryGetFieldProperty(
            NameString fieldName,
            MemberKind kind,
            out PropertyInfo prop)
        {
            fieldName.EnsureNotEmpty(nameof(fieldName));

            if (_members.TryGetValue(
                fieldName, out MemberInfo member)
                && member is PropertyInfo p)
            {
                prop = p;
                return true;
            }

            if (SourceType != typeof(object)
                && _fieldNames.Contains(fieldName))
            {
                InitializeAllMembers();
                prop = _allMembers.OfType<PropertyInfo>().FirstOrDefault(t =>
                    _context.Naming.GetMemberName(t, kind)
                       .Equals(fieldName));

                if (prop != null)
                {
                    TryRegisterResolver(fieldName, prop);
                    return true;
                }
            }

            prop = null;
            return false;
        }

        private void TryRegisterResolver(
            NameString fieldName,
            MemberInfo member)
        {
            if (!_resolvers.ContainsKey(fieldName))
            {
                _resolvers.Add(
                    fieldName,
                    new RegisteredResolver(
                        SourceType,
                        new FieldMember(Name, fieldName, member)));
            }
        }

        private void InitializeAllMembers()
        {
            if (_allMembers is null)
            {
                _allMembers = SourceType is null
                    || _bindingBehavior == BindingBehavior.Explicit
                    ? new List<MemberInfo>()
                    : _context.TypeInspector.GetMembers(SourceType).ToList();
            }
        }
    }
}
