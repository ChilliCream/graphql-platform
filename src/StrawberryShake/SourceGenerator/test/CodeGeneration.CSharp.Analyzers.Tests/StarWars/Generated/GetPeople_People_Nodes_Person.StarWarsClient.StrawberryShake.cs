// StrawberryShake.CodeGeneration.CSharp.Generators.ResultTypeGenerator

#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Analyzers.StarWars
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class GetPeople_People_Nodes_Person
        : global::System.IEquatable<GetPeople_People_Nodes_Person>
        , IGetPeople_People_Nodes_Person
    {
        public GetPeople_People_Nodes_Person(
            global::System.String name,
            global::System.String email,
            global::System.Boolean isOnline,
            global::System.DateTimeOffset lastSeen)
        {
            Name = name;
            Email = email;
            IsOnline = isOnline;
            LastSeen = lastSeen;
        }

        public global::System.String Name { get; }

        public global::System.String Email { get; }

        public global::System.Boolean IsOnline { get; }

        public global::System.DateTimeOffset LastSeen { get; }

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

            return Equals((GetPeople_People_Nodes_Person)obj);
        }

        public global::System.Boolean Equals(GetPeople_People_Nodes_Person? other)
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

            return (Name.Equals(other.Name))
                && Email.Equals(other.Email)
                && IsOnline == other.IsOnline
                && LastSeen.Equals(other.LastSeen);
        }

        public override global::System.Int32 GetHashCode()
        {
            unchecked
            {
                int hash = 5;

                hash ^= 397 * Name.GetHashCode();

                hash ^= 397 * Email.GetHashCode();

                hash ^= 397 * IsOnline.GetHashCode();

                hash ^= 397 * LastSeen.GetHashCode();

                return hash;
            }
        }
    }
}
