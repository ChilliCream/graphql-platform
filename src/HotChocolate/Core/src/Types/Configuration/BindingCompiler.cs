using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Configuration.Bindings;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Configuration
{
    internal class BindingCompiler
        : IBindingCompiler
    {
        private readonly HashSet<Type> _supportedBindings =
            new HashSet<Type>
            {
                typeof(ComplexTypeBindingInfo),
                typeof(ResolverBindingInfo),
                typeof(ResolverTypeBindingInfo),
            };

        private readonly List<IBindingInfo> _bindings =
            new List<IBindingInfo>();

        public bool CanHandle(IBindingInfo binding)
        {
            return binding != null
                && _supportedBindings.Contains(binding.GetType());
        }

        public void AddBinding(IBindingInfo binding)
        {
            if (binding is null)
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
            if (descriptorContext is null)
            {
                throw new ArgumentNullException(nameof(descriptorContext));
            }

            CompleteComplexTypeBindings(descriptorContext.Naming);
            CompleteResolverTypeBindings(
                descriptorContext.TypeInspector,
                descriptorContext.Naming);
            CompleteResolverBindings(descriptorContext.Naming);

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

        private void CompleteResolverTypeBindings(
            ITypeInspector inspector,
            INamingConventions naming)
        {
            foreach (ResolverTypeBindingInfo binding in
                _bindings.OfType<ResolverTypeBindingInfo>()
                    .ToList())
            {
                ComplexTypeBindingInfo typeBinding = null;

                if (binding.SourceType is null)
                {
                    binding.SourceType = typeof(object);
                }

                if (binding.SourceType != null && binding.TypeName.IsEmpty)
                {
                    typeBinding = _bindings.OfType<ComplexTypeBindingInfo>()
                        .FirstOrDefault(t => t.Type == binding.SourceType);
                    if (typeBinding is null)
                    {
                        binding.TypeName = naming.GetTypeName(
                            binding.SourceType, TypeKind.Object);
                    }
                    else
                    {
                        binding.TypeName = typeBinding.Name;
                    }
                }

                typeBinding = _bindings.OfType<ComplexTypeBindingInfo>()
                    .FirstOrDefault(t => t.Name.Equals(binding.TypeName));
                if (typeBinding is null)
                {
                    _bindings.Add(new ComplexTypeBindingInfo
                    {
                        Name = binding.TypeName,
                        Type = binding.SourceType
                    });
                }
                else if (binding.SourceType == typeof(object))
                {
                    binding.SourceType = typeBinding.Type;
                }

                foreach (ResolverFieldBindingInfo field in binding.Fields)
                {
                    if (field.FieldName.IsEmpty)
                    {
                        field.FieldName = naming.GetMemberName(
                            field.FieldMember, MemberKind.ObjectField);
                    }
                }

                if (binding.BindingBehavior == BindingBehavior.Implicit)
                {
                    AddImplicitResolverBindings(inspector, naming, binding);
                }
            }
        }

        private static void AddImplicitResolverBindings(
            ITypeInspector inspector,
            INamingConventions naming,
            ResolverTypeBindingInfo binding)
        {
            var names = new HashSet<NameString>(
                binding.Fields.Select(t => t.FieldName));

            foreach (MemberInfo member in
                inspector.GetMembers(binding.ResolverType))
            {
                NameString fieldName =
                    naming.GetMemberName(member, MemberKind.ObjectField);
                if (names.Add(fieldName))
                {
                    var builder = ResolverFieldBindingBuilder.New();
                    builder.SetResolver(member);
                    builder.SetField(fieldName);
                    binding.Fields = binding.Fields.Add(builder.Create());
                }
            }
        }

        private void CompleteResolverBindings(INamingConventions naming)
        {
            foreach (ResolverBindingInfo binding in
                _bindings.OfType<ResolverBindingInfo>()
                    .ToList())
            {
                if (binding.TypeName.IsEmpty)
                {
                    ComplexTypeBindingInfo typeBinding =
                        _bindings.OfType<ComplexTypeBindingInfo>()
                            .FirstOrDefault(t => t.Type == binding.SourceType);
                    binding.TypeName = typeBinding is null
                        ? naming.GetTypeName(binding.SourceType)
                        : typeBinding.Name;
                }

                if (binding.FieldName.IsEmpty)
                {
                    binding.FieldName = naming.GetMemberName(
                        binding.Member, MemberKind.ObjectField);
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
            }
        }

        private IEnumerable<TypeBindingInfo> CreateTypeBindingInfos(
            IDescriptorContext context)
        {
            var bindings = new List<TypeBindingInfo>();

            foreach (ComplexTypeBindingInfo binding in
                _bindings.OfType<ComplexTypeBindingInfo>())
            {
                bindings.Add(CreateTypeBindingInfo(context, binding));
            }

            return bindings;
        }

        private TypeBindingInfo CreateTypeBindingInfo(
            IDescriptorContext context,
            ComplexTypeBindingInfo binding)
        {
            var registerdResolvers =
                new Dictionary<NameString, RegisteredResolver>();
            var members = new Dictionary<NameString, MemberInfo>();

            RegisterResolvers(binding, registerdResolvers);

            foreach (ResolverTypeBindingInfo resolverBinding in
                _bindings.OfType<ResolverTypeBindingInfo>()
                    .Where(t => t.TypeName.Equals(binding.Name)))
            {
                RegisterResolverFields(
                    binding.Name, resolverBinding,
                    registerdResolvers, members);
            }

            RegisterFields(binding, registerdResolvers, members);

            return new TypeBindingInfo(
                context,
                binding.Name,
                binding.Type,
                binding.BindingBehavior,
                registerdResolvers,
                members);
        }

        private static void RegisterResolverFields(
            NameString typeName,
            ResolverTypeBindingInfo resolverBinding,
            IDictionary<NameString, RegisteredResolver> registerdResolvers,
            IDictionary<NameString, MemberInfo> members)
        {
            foreach (ResolverFieldBindingInfo field in
                resolverBinding.Fields)
            {
                if (!registerdResolvers.ContainsKey(field.FieldName))
                {
                    IFieldReference fieldReference =
                        field.ResolverMember is null
                        ? (IFieldReference)new FieldResolver(
                            typeName,
                            field.FieldName,
                            field.ResolverDelegate)
                        : new FieldMember(
                            typeName,
                            field.FieldName,
                            field.ResolverMember);
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

        private void RegisterResolvers(
            ComplexTypeBindingInfo binding,
            IDictionary<NameString, RegisteredResolver> registerdResolvers)
        {
            foreach (ResolverBindingInfo resolver in
                _bindings.OfType<ResolverBindingInfo>()
                    .Where(t => t.TypeName.Equals(binding.Name)))
            {
                if (!registerdResolvers.ContainsKey(resolver.FieldName))
                {
                    registerdResolvers.Add(resolver.FieldName,
                        new RegisteredResolver(
                            binding.Type ?? typeof(object),
                            new FieldResolver(
                                binding.Name,
                                resolver.FieldName,
                                resolver.Resolver)));
                }
            }
        }

        private static void RegisterFields(
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
