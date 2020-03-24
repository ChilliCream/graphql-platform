using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Validation
{
    public interface IDocumentValidationContext : ISyntaxVisitorContext
    {
        ISchema Schema { get; }

        Stack<ISyntaxNode> Path { get; }

        IDictionary<string, FragmentDefinitionNode> Fragments { get; }

        ISet<string> UsedVariables { get; }

        ISet<string> UnusedVariables { get; }

        ISet<string> DeclaredVariables { get; }

        ICollection<IError> Errors { get; }
    }

    public sealed class DocumentValidationContext : IDocumentValidationContext
    {
        public ISchema Schema { get; set; }

        public Stack<ISyntaxNode> Path => throw new System.NotImplementedException();

        public IDictionary<string, FragmentDefinitionNode> Fragments => throw new System.NotImplementedException();

        public ISet<string> UsedVariables => throw new System.NotImplementedException();

        public ISet<string> UnusedVariables => throw new System.NotImplementedException();

        public ISet<string> DeclaredVariables => throw new System.NotImplementedException();

        public ICollection<IError> Errors => throw new System.NotImplementedException();

        public void Clear()
        {

        }
    }

    public class DocumentValidationContextPool
        : DefaultObjectPool<DocumentValidationContext>
    {
        public DocumentValidationContextPool()
            : base(new Policy(), 64)
        {
        }

        private class Policy : IPooledObjectPolicy<DocumentValidationContext>
        {
            public DocumentValidationContext Create() => new DocumentValidationContext();

            public bool Return(DocumentValidationContext obj)
            {
                obj.Clear();
                return true;
            }
        }
    }
}
