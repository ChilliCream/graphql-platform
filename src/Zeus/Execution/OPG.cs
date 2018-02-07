using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Zeus.Abstractions;
using Zeus.Resolvers;

namespace Zeus.Execution
{

    public interface IOperationOptimizer
    {
        IOptimizedOperation Optimize(ISchema schema, QueryDocument queryDocument, string operationName);
    }

    public class OperationCompiler
        : IOperationCompiler
    {
        public ICompiledOperation Compile(ISchema schema, QueryDocument queryDocument, string operationName)
        {
            OperationDefinition operationDefinition = GetOperation(queryDocument, operationName);





        }

        private CompiledOperation CreateOperation(ISchema schema, OperationDefinition operation)
        {
            if (schema.ObjectTypes.TryGetValue(operation.Type.ToString(), out var typeDefinition))
            {

            }
        }




        private static OperationDefinition GetOperation(QueryDocument queryDocument, string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                if (document.Operations.Count == 1)
                {
                    return document.Operations.Values.First();
                }
                throw new Exception("TODO: Query Exception");
            }
            else
            {
                if (document.Operations.TryGetValue(name, out var operation))
                {
                    return operation;
                }
                throw new Exception("TODO: Query Exception");
            }
        }
    }

    public interface IOptimizedOperation
    {
        OperationDefinition Operation { get; }
        IReadOnlyCollection<IOptimizedSelection> Selections { get; }
    }

    public interface IOptimizedSelection
    {
        string Name { get; }


        ObjectTypeDefinition TypeDefinition { get; }

        FieldDefinition FieldDefinition { get; }

        Field Field { get; }

        IResolver Resolver { get; }

        IReadOnlyCollection<IOptimizedSelection> Selections { get; }

        IResolverContext CreateContext(object parentResult, IResolverContext parentContext, IVariableCollection variables);
    }

    public interface IVariableCollection
    {
        T GetVariable<T>(string variableName);
    }

    public class CompiledOperation
        : CompiledNode
        , ICompiledOperation
    {
        private CompiledOperation(OperationDefinition operation,
            ObjectTypeDefinition typeDefinition,
            FieldDefinition fieldDefinition,
            IReadOnlyCollection<ICompiledSelection> selections,
            IResolver resolver)
            : base(typeDefinition, fieldDefinition, selections, resolver)
        {

        }

        public OperationDefinition Operation { get; }

        public IResolverContext CreateContext(object root, IVariableCollection variables)
        {
            throw new System.NotImplementedException();
        }
    }


    public abstract class CompiledNode
        : ICompiledNode
    {
        protected CompiledNode(
            ObjectTypeDefinition typeDefinition,
            FieldDefinition fieldDefinition,
            IResolver resolver)
        {
            TypeDefinition = typeDefinition ?? throw new System.ArgumentNullException(nameof(typeDefinition));
            FieldDefinition = fieldDefinition ?? throw new System.ArgumentNullException(nameof(fieldDefinition));
            Resolver = resolver ?? throw new System.ArgumentNullException(nameof(resolver));
        }

        public ObjectTypeDefinition TypeDefinition { get; }

        public FieldDefinition FieldDefinition { get; }

        public abstract IReadOnlyCollection<ICompiledSelection> Selections { get; }

        public IResolver Resolver { get; }

    }
































}