using System;
using System.Collections.Generic;
using Generator.ClassGenerator;

namespace Generator
{
    /// <summary>
    /// error-code - String
    /// args - Object(optional)
    /// loc - Array of Objects | Array of Arrays of Numbers(optional)
    ///     line - Number
    ///     column - Number
    /// </summary>
    internal class ErrorCodeMatch : IAssertion
    {
        private static readonly string _errorCodeKey = "error-code";
        private static readonly string _argsKey = "args";
        private static readonly string _defNameKey = "defName";
        private static readonly string _locKey = "loc";
        private static readonly string _lineKey = "line";
        private static readonly string _columnKey = "column";
        private readonly string _errorCode;
        private readonly string _defName;
        private readonly int _line;
        private readonly int _column;

        private ErrorCodeMatch(Dictionary<object, object> value)
        {
            _errorCode = value[_errorCodeKey] as string;

            var argsValue = value[_argsKey] as Dictionary<object, object>;
            if (argsValue == null)
            {
                throw new InvalidOperationException("Error code match, args should be an object");
            }
            if (!argsValue.ContainsKey(_defNameKey))
            {
                throw new InvalidOperationException("Error code match, invalid args structure");
            }
            _defName = argsValue[_defNameKey] as string;

            var locValue = value[_locKey] as Dictionary<object, object>;
            if (locValue == null)
            {
                throw new InvalidOperationException("Error code match, loc should be an object");
            }
            if (!locValue.ContainsKey(_lineKey) || !locValue.ContainsKey(_columnKey))
            {
                throw new InvalidOperationException("Error code match, invalid loc structure");
            }
            _line = int.Parse(locValue[_lineKey] as string);
            _column = int.Parse(locValue[_columnKey] as string);
        }

        public static (bool, CreateAssertion) TryCreate(
            Dictionary<object, object> value,
            TestContext context)
        {
            return (value.ContainsKeys(_errorCodeKey, _argsKey, _locKey), Create);
        }

        public static IAssertion Create(Dictionary<object, object> value)
        {
            return new ErrorCodeMatch(value);
        }

        public Block CreateBlock()
        {
            return new Block(new Statement(
                $"Assert.Equal(1, result.Errors.Count(e => e.Code == \"{_errorCode}\" /*&& arg:{_defName}, line:{_line}, col:{_column}*/));"));
        }
    }
}
