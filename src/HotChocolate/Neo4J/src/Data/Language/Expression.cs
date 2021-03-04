namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// An expression can be used in many places, i.e. in return statements, pattern
    /// elements etc.
    /// </summary>
    public abstract class Expression : Visitable
    {
        /// <summary>
        /// Creates an expression with an alias. This expression does not track which or
        /// how many aliases have been created.
        /// </summary>
        /// <param name="alias">The alias to use</param>
        /// <returns>An aliased expression</returns>
        public AliasedExpression As(string alias)
        {
            Ensure.HasText(alias, "The alias may not be null or empty.");
            return new AliasedExpression(this, alias);
        }

        /// <summary>
        /// Reuse an existing symbolic name to alias this expression
        /// </summary>
        /// <param name="alias"></param>
        /// <returns>An aliased expression.</returns>
        public AliasedExpression As(SymbolicName alias)
        {
            Ensure.IsNotNull(alias, "The alias may not be null.");
            return As(alias.GetValue());
        }

        public Condition IsEqualTo(Expression rhs) => Conditions.IsEqualTo(this, rhs);
        public Condition IsNotEqualTo(Expression rhs) => Conditions.IsNotEqualTo(this, rhs);
        public Condition LessThan(Expression rhs) => Conditions.LessThan(this, rhs);
        public Condition LessThanOrEqualTo(Expression rhs) => Conditions.LessThanOrEqualTo(this, rhs);
        public Condition GreaterThan(Expression rhs) => Conditions.GreaterThan(this, rhs);
        public Condition GreaterThanOEqualTo(Expression rhs) => Conditions.GreaterThanOEqualTo(this, rhs);
        public Condition IsTrue() => Conditions.IsEqualTo(this, Cypher.LiteralTrue());
        public Condition IsFalse() => Conditions.IsEqualTo(this, Cypher.LiteralFalse());
        public Condition Matches(Expression expression) => Conditions.Matches(this, expression);
        public Condition Matches(string pattern) => Conditions.Matches(this, Cypher.LiteralOf(pattern));
        public Condition StartsWith(Expression expression) => Conditions.StartsWith(this, expression);
        public Condition EndsWith(Expression expression) => Conditions.EndsWith(this, expression);
        public Condition Contains(Expression expression) => Conditions.Contains(this, expression);
        public Condition IsNull() => Conditions.IsNull(this);
        public Condition IsNotNull() => Conditions.IsNotNull(this);
        public Condition IsEmpty() => Conditions.IsEmpty(this);
        public Condition In(Expression expression) => Comparison.Create(this, Operator.In, expression);
        public Operation Concat(Expression expression) => Operations.Concat(this, expression);
        public Operation Add(Expression expression) => Operations.Add(this, expression);
        public Operation Subtract(Expression expression) => Operations.Subtract(this, expression);
        public Operation Multiply(Expression expression) => Operations.Multiply(this, expression);
        public Operation Divide(Expression expression) => Operations.Divide(this, expression);
        public Operation Remainder(Expression expression) => Operations.Remainder(this, expression);
        public Operation Pow(Expression expression) => Operations.Pow(this, expression);
        // TODO Implement FunctionInvocation
        //public Condition IsEmpty() => Conditions.IsEmpty(this);
        public SortItem Descending() => SortItem.Create(this, SortDirection.Descending);
        public SortItem Ascending() => SortItem.Create(this, SortDirection.Ascending);
    }
}
