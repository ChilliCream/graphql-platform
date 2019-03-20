using System;
using System.Collections.Generic;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate
{
    internal sealed class CompletionContext
        : ICompletionContext
    {
        private InitializationContext _initializationContext;

        public CompletionContext(
            InitializationContext initializationContext,
            IReadOnlyList<FieldMiddleware> globalComponents)
        {
            _initializationContext = initializationContext
                ?? throw new ArgumentNullException(nameof(initializationContext));
            GlobalComponents = globalComponents
                ?? throw new ArgumentNullException(nameof(globalComponents));
        }

        public TypeStatus Status { get; } = TypeStatus.Initialized;

        public bool? IsQueryType { get; set; }

        public IReadOnlyList<FieldMiddleware> GlobalComponents { get; }

        public ITypeSystemObject Type => _initializationContext.Type;

        public bool IsType => _initializationContext.IsType;

        public bool IsIntrospectionType =>
            _initializationContext.IsIntrospectionType;

        public bool IsDirective => _initializationContext.IsDirective;

        public IServiceProvider Services => _initializationContext.Services;

        public T GetType<T>(ITypeReference reference)
            where T : IType
        {
            throw new NotImplementedException();
        }

        public bool TryGetType<T>(ITypeReference reference, out T type)
            where T : IType
        {
            throw new NotImplementedException();
        }

        public DirectiveType GetDirectiveType(IDirectiveReference reference)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyCollection<ObjectType> GetPossibleTypes()
        {
            throw new NotImplementedException();
        }

        public FieldResolver GetResolver(IFieldReference reference)
        {
            throw new NotImplementedException();
        }

        public Func<ISchema> GetSchemaResolver()
        {
            throw new NotImplementedException();
        }



        public IEnumerable<IType> GetTypes()
        {
            throw new NotImplementedException();
        }

        public void ReportError(ISchemaError error)
        {
            _initializationContext.ReportError(error);
        }


    }
}
