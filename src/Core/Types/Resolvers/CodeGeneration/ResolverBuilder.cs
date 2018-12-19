using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Resolvers.CodeGeneration;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal class ResolverBuilder
    {
        private readonly ClassSourceCodeGenerator _codeGenerator =
            new ClassSourceCodeGenerator();

        private readonly List<IDelegateDescriptor> _descriptors =
            new List<IDelegateDescriptor>();

        public void AddDescriptor(IDelegateDescriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            _descriptors.Add(descriptor);
        }

        public void AddDescriptors(IEnumerable<IDelegateDescriptor> descriptors)
        {
            if (descriptors == null)
            {
                throw new ArgumentNullException(nameof(descriptors));
            }

            _descriptors.AddRange(descriptors);
        }

        public ResolverBuilderResult Build()
        {
            if (_descriptors.Count == 0)
            {
                return ResolverBuilderResult.Empty;
            }

            IFieldResolverDescriptor[] resolverDescriptors =
                _descriptors.OfType<IFieldResolverDescriptor>().ToArray();
            IDirectiveMiddlewareDescriptor[] middlewareDescriptors =
                _descriptors.OfType<IDirectiveMiddlewareDescriptor>().ToArray();

            CompiledTypes compiledTypes =
                Compile(resolverDescriptors, middlewareDescriptors);

            return new ResolverBuilderResult(
                ExtractResolvers(resolverDescriptors, compiledTypes).ToArray(),
                ExtractMiddlewares(middlewareDescriptors, compiledTypes)
                    .ToArray());
        }

        private CompiledTypes Compile(
            IFieldResolverDescriptor[] resolverDescriptors,
            IDirectiveMiddlewareDescriptor[] middlewareDescriptors)
        {
            GeneratedClass resolverClass = _codeGenerator
                .Generate(resolverDescriptors);

            GeneratedClass middlewareClass = _codeGenerator
                .Generate(middlewareDescriptors);

            Assembly assembly = CSharpCompiler.Compile(
                    resolverClass.SourceText,
                    middlewareClass.SourceText);

            return new CompiledTypes(
                assembly.GetType(resolverClass.FullName),
                assembly.GetType(middlewareClass.FullName));
        }

        private IEnumerable<FieldResolver> ExtractResolvers(
            IFieldResolverDescriptor[] resolverDescriptors,
            CompiledTypes compiledTypes)
        {
            for (var i = 0; i < resolverDescriptors.Length; i++)
            {
                string delegateName =
                    ClassSourceCodeGenerator.GetDelegateName(i);

                FieldInfo field = compiledTypes.ResolverType.GetField(
                        delegateName,
                        BindingFlags.Static | BindingFlags.Public);

                yield return new FieldResolver(
                    resolverDescriptors[i].Field.TypeName,
                    resolverDescriptors[i].Field.FieldName,
                    (FieldResolverDelegate)field.GetValue(field));
            }
        }

        private IEnumerable<IDirectiveMiddleware> ExtractMiddlewares(
            IDirectiveMiddlewareDescriptor[] middlewareDescriptors,
            CompiledTypes compiledTypes)
        {
            for (var i = 0; i < middlewareDescriptors.Length; i++)
            {
                string delegateName =
                    ClassSourceCodeGenerator.GetDelegateName(i);

                FieldInfo field = compiledTypes.MiddlewareType.GetField(
                        delegateName,
                        BindingFlags.Static | BindingFlags.Public);

                yield return new DirectiveDelegateMiddleware(
                    middlewareDescriptors[i].DirectiveName,
                    (DirectiveMiddleware)field.GetValue(field));
            }
        }

        private class CompiledTypes
        {
            public CompiledTypes(Type resolverType, Type middlewareType)
            {
                ResolverType = resolverType
                    ?? throw new ArgumentNullException(nameof(resolverType));
                MiddlewareType = middlewareType
                    ?? throw new ArgumentNullException(nameof(middlewareType));
            }

            public Type ResolverType { get; }
            public Type MiddlewareType { get; }
        }
    }
}
