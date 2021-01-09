using StrawberryShake.CodeGeneration;

namespace StrawberryShake.Integration
{
    public static class IntegrationDescriptors
    {
        #region ICharacter

        private const string ICharacterName = "ICharacter";

        public static TypeDescriptor DroidTypeDescriptor => new(
            "Droid",
            "StarWarsClient",
            new[] {ICharacterName},
            new[] {TestHelper.GetNamedNonNullStringTypeReference("Name")},
            kind: TypeKind.EntityType
        );

        public static TypeDescriptor HumanTypeDescriptor => new(
            "Human",
            "StarWarsClient",
            new[] {ICharacterName},
            new[] {TestHelper.GetNamedNonNullStringTypeReference("Name")},
            kind: TypeKind.EntityType
        );

        public static TypeDescriptor ICharacterDescriptor => new(
            ICharacterName,
            "StarWarsClient",
            isImplementedBy: new[] {DroidTypeDescriptor, HumanTypeDescriptor},
            kind: TypeKind.EntityType
        );

        #endregion


        #region IHero

        private const string IHeroName = "IHero";

        public static TypeDescriptor FriendsConnectionDescriptor => new(
            "FriendsConnection",
            "StarWarsClient",
            properties: new[]
            {
                new NamedTypeReferenceDescriptor(
                    ICharacterDescriptor,
                    false,
                    ListType.List,
                    "Nodes"
                ),
                TestHelper.GetNamedNonNullIntTypeReference("TotalCount")
            },
            kind: TypeKind.DataType
        );

        private static NamedTypeReferenceDescriptor friendsReferenceDescriptor => new(
            FriendsConnectionDescriptor,
            false,
            ListType.NoList,
            "Friends"
        );

        public static TypeDescriptor HumanHeroTypeDescriptor => new(
            "HumanHero",
            "StarWarsClient",
            new[] {IHeroName},
            new[] {TestHelper.GetNamedNonNullStringTypeReference("Name"), friendsReferenceDescriptor},
            kind: TypeKind.EntityType
        );

        public static TypeDescriptor DroidHeroTypeDescriptor => new(
            "DroidHero",
            "StarWarsClient",
            new[] {IHeroName},
            new[] {TestHelper.GetNamedNonNullStringTypeReference("Name"), friendsReferenceDescriptor},
            kind: TypeKind.EntityType
        );

        public static TypeDescriptor IHeroDescriptor => new(
            IHeroName,
            "StarWarsClient",
            new[] {ICharacterName},
            new[] {friendsReferenceDescriptor},
            new[] {HumanHeroTypeDescriptor, DroidHeroTypeDescriptor},
            TypeKind.EntityType
        );

        #endregion


        public static TypeDescriptor GetHeroResultDescriptor => new(
            "GetHeroResult",
            "StarWarsClient",
            new string[] { },
            new[]
            {
                new NamedTypeReferenceDescriptor(
                    IHeroDescriptor,
                    false,
                    ListType.NoList,
                    "Hero"
                ),
                TestHelper.GetNamedNonNullStringTypeReference("Version")
            }
        );


        public static ResultBuilderDescriptor GetHeroResultBuilderDescriptor => new()
        {
            ResultType = GetHeroResultDescriptor, ValueParsers = new[] {("string", "string", "String")}
        };
    }
}
