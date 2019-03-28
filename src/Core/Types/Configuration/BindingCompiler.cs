using System.Collections;
using System.Reflection;
using System.Net;
using System.Linq;
using System;
using HotChocolate.Configuration.Bindings;
using System.Collections.Generic;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types;
using HotChocolate.Resolvers;

namespace HotChocolate.Configuration
{
    internal class BindingCompiler
        : IBindingCompiler
    {
        private static HashSet<Type> _supportedBindings = new HashSet<Type>
        {
            typeof(ComplexTypeBindingInfo),
            typeof(ResolverBindingInfo),
            typeof(ResolverTypeBindingInfo),
        };

        private List<IBindingInfo> _bindings = new List<IBindingInfo>();

        public bool CanHandle(IBindingInfo binding)
        {
            return binding != null
                && _supportedBindings.Contains(binding.GetType());
        }

        public void AddBinding(IBindingInfo binding)
        {
            if (binding == null)
            {
                throw new ArgumentNullException(nameof(binding));
            }

            if (!CanHandle(binding))
            {
                throw new ArgumentException(
                    "The specified binding cannot be handled.",
                    nameof(binding));
            }

            _bindings.Add(binding);
        }

        public IBindingLookup Compile(
            IDescriptorContext descriptorContext)
        {
            if (descriptorContext == null)
            {
                throw new ArgumentNullException(nameof(descriptorContext));
            }

            CompleteComplexTypeBindings(descriptorContext.Naming);
            CompleteResolverTypeBindings(descriptorContext.Naming);

            IEnumerable<TypeBindingInfo> bindings =
                CreateTypeBindingInfos(descriptorContext);

            return new BindingLookup(descriptorContext, bindings);
        }

        private void CompleteComplexTypeBindings(
            INamingConventions naming)
        {
            foreach (ComplexTypeBindingInfo binding in
                _bindings.OfType<ComplexTypeBindingInfo>())
            {
                if (binding.Name.IsEmpty)
                {
                    binding.Name = naming.GetTypeName(binding.Type);
                }

                foreach (ComplexTypeFieldBindingInfo field in binding.Fields)
                {
                    if (field.Name.IsEmpty)
                    {
                        field.Name = naming.GetMemberName(
                            field.Member, MemberKind.Field);
                    }
                }
            }
        }

        private void CompleteResolverTypeBindings(INamingConventions naming)
        {
            foreach (ResolverTypeBindingInfo binding in
                _bindings.OfType<ResolverTypeBindingInfo>())
            {
                if (binding.SourceType == null)
                {
                    binding.SourceType = binding.ResolverType;
                }

                if (binding.SourceType != null && binding.TypeName.IsEmpty)
                {
                    ComplexTypeBindingInfo typeBinding =
                        _bindings.OfType<ComplexTypeBindingInfo>()
                            .FirstOrDefault(t => t.Type == binding.SourceType);
                    if (typeBinding == null)
                    {
                        binding.TypeName = naming.GetTypeName(
                            binding.SourceType, TypeKind.Object);
                    }
                    else
                    {
                        binding.TypeName = typeBinding.Name;
                    }
                }

                if (!_bindings.OfType<ComplexTypeBindingInfo>()
                    .Any(t => t.Name.Equals(binding.TypeName)))
                {
                    _bindings.Add(new ComplexTypeBindingInfo
                    {
                        Name = binding.TypeName,
                        Type = binding.SourceType
                    });
                }

                foreach (ResolverFieldBindingInfo field in binding.Fields)
                {
                    if (field.FieldName.IsEmpty)
                    {
                        field.FieldName = naming.GetMemberName(
                            field.FieldMember, MemberKind.ObjectField);
                    }
                }
            }
        }

        private IEnumerable<TypeBindingInfo> CreateTypeBindingInfos(
            IDescriptorContext context)
        {
            var bindings = new List<TypeBindingInfo>();

            foreach (TypeBindingInfo binding in
                _bindings.OfType<TypeBindingInfo>())
            {
                bindings.Add(binding);
            }

            return bindings;
        }

        private TypeBindingInfo CreateTypeBindingInfo(
            IDescriptorContext context,
            ComplexTypeBindingInfo binding)
        {
            Dictionary<NameString, RegisteredResolver> registerdResolvers =
                new Dictionary<NameString, RegisteredResolver>();
            Dictionary<NameString, MemberInfo> members =
                new Dictionary<NameString, MemberInfo>();
            HashSet<MemberInfo> processed = new HashSet<MemberInfo>();

            foreach (ResolverTypeBindingInfo resolverBinding in
                _bindings.OfType<ResolverTypeBindingInfo>()
                    .Where(t => t.TypeName.Equals(binding.Name)
                        && t.SourceType == binding.Type))
            {
                RegisterResolverFields(
                    binding.Name, resolverBinding,
                    registerdResolvers, members);
            }

            RegisterFields(binding, registerdResolvers, members);

            return new TypeBindingInfo(
                context, binding.Name, binding.Type,
                registerdResolvers, members);
        }

        private void RegisterResolverFields(
            NameString typeName,
            ResolverTypeBindingInfo resolverBinding,
            Dictionary<NameString, RegisteredResolver> registerdResolvers,
            Dictionary<NameString, MemberInfo> members)
        {
            foreach (ResolverFieldBindingInfo field in
                resolverBinding.Fields)
            {
                if (!registerdResolvers.ContainsKey(field.FieldName))
                {
                    IFieldReference fieldReference = field.FieldMember == null
                        ? (IFieldReference)new FieldResolver(
                            typeName,
                            field.FieldName,
                            field.ResolverDelegate)
                        : new FieldMember(
                            typeName,
                            field.FieldName,
                            field.FieldMember);
                    registerdResolvers.Add(field.FieldName,
                        new RegisteredResolver(
                            resolverBinding.ResolverType,
                            resolverBinding.SourceType,
                            fieldReference));
                }

                if (!members.ContainsKey(field.FieldName))
                {
                    members.Add(field.FieldName, field.FieldMember);
                }
            }
        }

        private void RegisterFields(
            ComplexTypeBindingInfo binding,
            Dictionary<NameString, RegisteredResolver> registerdResolvers,
            Dictionary<NameString, MemberInfo> members)
        {
            foreach (ComplexTypeFieldBindingInfo field in binding.Fields)
            {
                if (!members.ContainsKey(field.Name)
                    && !members.ContainsValue(field.Member))
                {
                    members.Add(field.Name, field.Member);

                    if (!registerdResolvers.ContainsKey(field.Name))
                    {
                        registerdResolvers.Add(field.Name,
                            new RegisteredResolver(
                                binding.Type,
                                new FieldMember(
                                binding.Name,
                                field.Name,
                                field.Member)));
                    }
                }
            }
        }
    }
}
