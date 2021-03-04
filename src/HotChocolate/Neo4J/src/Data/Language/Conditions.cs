using System;

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// Builder for various conditions
    /// </summary>
    public static class Conditions
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static Condition Matches(Expression lhs, Expression rhs) =>
            Comparison.Create(lhs, Operator.Matches, rhs);

        /// <summary>
        ///
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static Condition IsEqualTo(Expression lhs, Expression rhs) =>
            Comparison.Create(lhs, Operator.Equality, rhs);

        /// <summary>
        ///
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static Condition IsNotEqualTo(Expression lhs, Expression rhs) =>
            Comparison.Create(lhs, Operator.InEquality, rhs);

        /// <summary>
        ///
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static Condition LessThan(Expression lhs, Expression rhs) =>
            Comparison.Create(lhs, Operator.LessThan, rhs);

        /// <summary>
        ///
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static Condition LessThanOrEqualTo(Expression lhs, Expression rhs) =>
            Comparison.Create(lhs, Operator.LessThanOrEqualTo, rhs);

        /// <summary>
        ///
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static Condition GreaterThan(Expression lhs, Expression rhs) =>
            Comparison.Create(lhs, Operator.GreaterThan, rhs);

        /// <summary>
        ///
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static Condition GreaterThanOEqualTo(Expression lhs, Expression rhs) =>
            Comparison.Create(lhs, Operator.GreaterThanOrEqualTo, rhs);

        public static Condition Not(Condition condition)
        {
            Ensure.IsNotNull(condition, "Condition to negate must not be null.");
            return condition.Not();
        }

        public static Condition Not(IPatternElement patternElement)
        {
            Ensure.IsNotNull(patternElement, "Pattern to negate must not be null.");
            return new ExcludePattern(patternElement);
        }

        public static Condition StartsWith(Expression lhs, Expression rhs) =>
            Comparison.Create(lhs, Operator.StartsWith, rhs);

        public static Condition EndsWith(Expression lhs, Expression rhs) =>
            Comparison.Create(lhs, Operator.EndsWith, rhs);

        public static Condition Contains(Expression lhs, Expression rhs) =>
            Comparison.Create(lhs, Operator.Contains, rhs);

        public static Condition IsNull(Expression expression) =>
            Comparison.Create(Operator.IsNull, expression);

        public static Condition IsNotNull(Expression expression) =>
            Comparison.Create(Operator.IsNotNull, expression);

        // TODO: Implement Functions
        public static Condition IsEmpty(Expression expression) =>
            throw new NotImplementedException();

        public static Condition NoCondition() =>
            CompoundCondition.Empty();

        public static Condition IsTrue() =>
            ConstantCondition.True;

        public static Condition IsFalse() =>
            ConstantCondition.False;

    }
}
