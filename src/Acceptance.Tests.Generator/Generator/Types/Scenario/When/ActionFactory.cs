using System;
using System.Collections.Generic;

namespace Generator
{
    /// Actions:
    /// Query parsing
    ///     parse - Boolean - just parses the query
    /// Query validation
    ///     validate - Array of Strings - the list of validation rule names to validate a query against.This action will only validate query without executing it.
    /// Query execution
    ///     execute - Boolean | Object - executes a query
    internal static class ActionFactory
    {
        internal static IAction Create(Dictionary<object, object> when)
        {
            if (when.ContainsKey("parse"))
            {
                return new QueryParsing(bool.Parse(when["parse"] as string));
            }

            if (when.ContainsKey("validate"))
            {
                return new QueryValidation(when["validate"]);
            }

            if (when.ContainsKey("execute"))
            {
                return new QueryExecution(when["execute"]);
            }

            throw new InvalidOperationException("Unknown action type");
        }
    }
}
