using HotChocolate.Properties;

namespace HotChocolate.Types
{
    /// <summary>
    /// The Int scalar type represents a signed 32‐bit numeric non‐fractional
    /// value. Response formats that support a 32‐bit integer or a number type
    /// should use that type to represent this scalar.
    ///
    /// http://facebook.github.io/graphql/June2018/#sec-Int
    /// </summary>
    [SpecScalar]
    public sealed class IntType
        : IntTypeBase
    {
        public IntType()
            : base("Int")
        {
            Description = TypeResources.IntType_Description;
        }
    }
}
