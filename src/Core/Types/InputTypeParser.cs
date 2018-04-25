using System;
using HotChocolate.Language;

namespace HotChocolate
{
    public abstract class InputTypeParser<TOut, TIn>
        : IInputTypeParser
        where TIn : IValueNode
    {
        public object Parse(IValueNode literal)
        {
            if (literal is TIn)
            {
                return Parse((TIn)literal);
            }

            // TODO : EXCEPTION
            throw new Exception("");
        }

        protected abstract TOut Parse(TIn literal);
    }
}
