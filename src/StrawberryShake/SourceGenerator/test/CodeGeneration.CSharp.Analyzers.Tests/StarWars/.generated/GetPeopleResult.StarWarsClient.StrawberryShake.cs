// StrawberryShake.CodeGeneration.CSharp.Generators.ResultTypeGenerator

#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Analyzers.StarWars
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class GetPeopleResult
        : global::System.IEquatable<GetPeopleResult>
        , IGetPeopleResult
    {
        public GetPeopleResult(global::StrawberryShake.CodeGeneration.CSharp.Analyzers.StarWars.IGetPeople_People? people)
        {
            People = people;
        }

        /// <summary>
        /// Gets access to all the people known to this service.
        /// </summary>
        public global::StrawberryShake.CodeGeneration.CSharp.Analyzers.StarWars.IGetPeople_People? People { get; }

        public override global::System.Boolean Equals(global::System.Object? obj)
        {
            if (ReferenceEquals(
                    null,
                    obj))
            {
                return false;
            }

            if (ReferenceEquals(
                    this,
                    obj))
            {
                return false;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((GetPeopleResult)obj);
        }

        public global::System.Boolean Equals(GetPeopleResult? other)
        {
            if (ReferenceEquals(
                    null,
                    other))
            {
                return false;
            }

            if (ReferenceEquals(
                    this,
                    other))
            {
                return false;
            }

            if (other.GetType() != GetType())
            {
                return false;
            }

            return (((People is null && other.People is null) ||People != null && People.Equals(other.People)));
        }

        public override global::System.Int32 GetHashCode()
        {
            unchecked
            {
                int hash = 5;

                if (!(People is null))
                {
                    hash ^= 397 * People.GetHashCode();
                }

                return hash;
            }
        }
    }
}
