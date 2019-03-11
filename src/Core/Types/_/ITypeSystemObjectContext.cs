using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate
{
    public interface ITypeSystemObjectContext
    {
        ITypeSystemObject Type { get; }

        bool IsType { get; }

        bool IsIntrospectionType { get; }

        bool IsDirective { get; }

        IServiceProvider Services { get; }

        void ReportError(ISchemaError error);
    }

    internal class Foo
    {
        public void InitializeTypes(ISchemaBuilderContext builderContext)
        {
            List<TypeSystemObjectContext> types =
                builderContext.TypeSystemObjects
                    .Select(t => new TypeSystemObjectContext(builderContext, t))
                    .ToList();

            var current = new List<TypeSystemObjectContext>();



        }



    }



    internal interface ISchemaBuilderContext
    {
        IReadOnlyList<ITypeSystemObject> TypeSystemObjects { get; }

        IServiceProvider Services { get; }

        void ReportError(ISchemaError error);

        void RegisterDependency(
            ITypeReference reference,
            TypeDependencyKind kind);

        void RegisterDependencyRange(
            IEnumerable<ITypeReference> references,
            TypeDependencyKind kind);

        void RegisterDependency(IDirectiveReference reference);

        void RegisterResolver(
            IFieldReference reference,
            Type sourceType,
            Type resolverType);

        void RegisterMiddleware(
            IFieldReference reference,
            IEnumerable<FieldMiddleware> components);

        bool TryGetType<T>(ITypeReference reference, out T type) where T : IType;

        T GetType<T>(ITypeReference reference) where T : IType;

        DirectiveType GetDirectiveType(IDirectiveReference reference);

        FieldResolver GetResolver(IFieldReference reference);

        FieldDelegate GetCompiledMiddleware(IFieldReference reference);

        IReadOnlyCollection<ObjectType> GetPossibleTypes();

        Func<ISchema> GetSchemaResolver();
    }

    internal class TypeSystemObjectContext
        : IInitializationContext
        , ICompletionContext
    {
        private readonly ISchemaBuilderContext _context;

        public TypeSystemObjectContext(
            ISchemaBuilderContext context,
            ITypeSystemObject type)
        {
        }

        public TypeStatus Status { get; set; } = TypeStatus.Initializing;

        public ITypeSystemObject Type { get; }

        public bool IsType { get; }

        public bool IsIntrospectionType { get; }

        public bool IsDirective { get; }

        public IServiceProvider Services => _context.Services;

        public FieldDelegate GetCompiledMiddleware(IFieldReference reference) =>
            _context.GetCompiledMiddleware(reference);

        public DirectiveType GetDirectiveType(IDirectiveReference reference) =>
            _context.GetDirectiveType(reference);

        public IReadOnlyCollection<ObjectType> GetPossibleTypes() =>
            _context.GetPossibleTypes();

        public FieldResolver GetResolver(IFieldReference reference) =>
            _context.GetResolver(reference);

        public Func<ISchema> GetSchemaResolver() =>
            _context.GetSchemaResolver();

        public T GetType<T>(ITypeReference reference) where T : IType =>
            _context.GetType<T>(reference);

        public void RegisterDependency(
            ITypeReference reference,
            TypeDependencyKind kind)
        {
            throw new NotImplementedException();
        }

        public void RegisterDependency(IDirectiveReference reference)
        {
            throw new NotImplementedException();
        }

        public void RegisterDependencyRange(IEnumerable<ITypeReference> references, TypeDependencyKind kind)
        {
            throw new NotImplementedException();
        }

        public void RegisterMiddleware(IFieldReference reference, IEnumerable<FieldMiddleware> components)
        {
            throw new NotImplementedException();
        }

        public void RegisterResolver(IFieldReference reference, Type sourceType, Type resolverType)
        {
            throw new NotImplementedException();
        }

        public void ReportError(ISchemaError error)
        {
            throw new NotImplementedException();
        }

        public bool TryGetType<T>(ITypeReference reference, out T type) where T : IType
        {
            throw new NotImplementedException();
        }
    }
}
