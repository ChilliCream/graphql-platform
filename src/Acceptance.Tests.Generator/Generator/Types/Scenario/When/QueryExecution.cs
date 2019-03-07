using System;
using System.Collections.Generic;
using Generator.ClassGenerator;

namespace Generator
{
    /// operation-name - String(optional) - the name of an operation to execute(in case query contains more than one)
    /// variables - Object(optional) - variables for query execution
    /// validate-query - Boolean(optional) - true if query should be validated during the execution, false otherwise(true by default)
    /// test-value - String(optional) - the name of a field defined in the test-data.This value should be passed as a root value to an executor.
    internal class QueryExecution : IAction
    {
        private bool _onlyExecute;

        public QueryExecution(object value)
        {
            if (value is bool onlyExecute)
            {
                _onlyExecute = onlyExecute;
            }
            else if (value is Dictionary<object, object> execute)
            {
                var operationName = execute.TryGet("operation-name", string.Empty);
                var variables = execute.TryGet("variables", new object());
                var validateQuery = execute.TryGet("validate-query", false);
                var testValue = execute.TryGet("test-value", string.Empty);
            }
            else
            {
                throw new InvalidOperationException("Invalid execute structure");
            }
        }

        public Block CreateBlock(Statement header)
        {
            return new Block(new Statement("throw new NotImplementedException();"));
        }
    }
}
