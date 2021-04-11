using System;
using System.Collections.Generic;

namespace HotChocolate.Data.Neo4J.Language
{
    public class ParameterCollectingVisitor<T>
    {
        class ParameterInformation {

            private readonly HashSet<string> _names;
            private readonly Dictionary<string, object> _values;

            public ParameterInformation(HashSet<string> names, Dictionary<string, object> values)
            {
                _names = names;
                _values = values;
            }
        }

        private readonly IStatementContext<T> _statementContext;
        private readonly SortedSet<string> _names;
        private readonly SortedDictionary<string, object> _values;
        private readonly SortedDictionary<string, object> _erroneousParamters;

        public ParameterCollectingVisitor(IStatementContext<T> context)
        {
            _statementContext = context;
        }


        public void Enter(Parameter<T> parameter)
        {
            string parameterName = _statementContext.GetParameterName(parameter);
            object newValue = parameter.GetValue();
            /*if (newValue is ConstantParameterHolder) {
                if (!statementContext.isRenderConstantsAsParameters()) {
                    return;
                }
                newValue = ((ConstantParameterHolder) newValue).getValue();
            }
            boolean knownParameterName = !this.names.add(parameterName);

            Object oldValue = knownParameterName && this.values.containsKey(parameterName) ?
                this.values.get(parameterName) :
                Parameter.NO_VALUE;
            if (parameter.hasValue()) {
                this.values.put(parameterName, newValue);
            }
            if (knownParameterName && !Objects.equals(oldValue, newValue)) {
                Set<Object> conflictingObjects = this.erroneousParameters.computeIfAbsent(parameterName, s -> {
                    HashSet<Object> list = new HashSet<>();
                    list.add(oldValue);
                    return list;
                });
                conflictingObjects.add(newValue);
            }*/
        }

        public void Leave(IVisitable visitable)
        {
            throw new System.NotImplementedException();
        }
    }
}
