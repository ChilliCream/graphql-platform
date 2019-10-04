using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace  StrawberryShake.Client.GitHub
{
    public class GetUserOperation
        : IOperation<IGetUser>
    {
        private bool _modified_login;

        private string _value_login;

        public string Name => "getUser";

        public IDocument Document => Queries.Default;

        public Type ResultType => typeof(IGetUser);

        public string Login
        {
            get => _value_login;
            set
            {
                _value_login = value;
                _modified_login = true;
            }
        }

        public IReadOnlyList<VariableValue> GetVariableValues()
        {
            var variables = new List<VariableValue>();

            if(_modified_login)
            {
                variables.Add(new VariableValue("login", "String", Login));
            }

            return variables;
        }
    }
}
