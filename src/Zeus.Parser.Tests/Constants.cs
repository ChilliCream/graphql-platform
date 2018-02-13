namespace Zeus.Parser.Tests
{
    public static class Constants
    {
        public static readonly string DefaultSchema = @"
interface Pet
{
    name: String!
}

type Dog implements Pet
{
    name: String!
    flees(max: Int = 10) : [Flee!]
    barks(visit: VisitingPetInput) : Boolean!
}

type Flee implements Pet
{
    name: String!
}

input VisitingPetInput
{
    name: PetType!
}

enum PetType 
{
  CAT
  FLEE
}

union PetResult = Flee | Dog

type Query
{
    pets: [PetResult!]
    dog: Dog!
}

type Mutation
{
    removeFlees(dog: Dog!) : [Flee!]!
}
";
    }
}
