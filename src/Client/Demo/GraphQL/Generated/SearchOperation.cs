﻿using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace  StrawberryShake.Client.GraphQL
{
    public class SearchOperation
        : IOperation<ISearch>
    {
        private bool _modified_text;

        private string _value_text;

        public string Name => "search";

        public IDocument Document => Queries.Default;

        public Type ResultType => typeof(ISearch);

        public string Text
        {
            get => _value_text;
            set
            {
                _value_text = value;
                _modified_text = true;
            }
        }

        public IReadOnlyList<VariableValue> GetVariableValues()
        {
            var variables = new List<VariableValue>();

            if(_modified_text)
            {
                variables.Add(new VariableValue("text", "String", Text));
            }

            return variables;
        }
    }
}
