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
            isEntityType: true
        );

        public static TypeDescriptor HumanTypeDescriptor => new(
            "Human",
            "StarWarsClient",
            new[] {ICharacterName},
            new[] {TestHelper.GetNamedNonNullStringTypeReference("Name")},
            isEntityType: true
        );

        public static TypeDescriptor ICharacterDescriptor => new(
            ICharacterName,
            "StarWarsClient",
            isImplementedBy: new[] {DroidTypeDescriptor, HumanTypeDescriptor},
            isEntityType: true
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
                    new TypeDescriptor(
                        ICharacterDescriptor.Name,
                        "StarWarsClient"
                    ),
                    false,
                    ListType.List,
                    "Nodes"
                ),
                TestHelper.GetNamedNonNullIntTypeReference("TotalCount")
            }
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
            isEntityType: true
        );

        public static TypeDescriptor DroidHeroTypeDescriptor => new(
            "DroidHero",
            "StarWarsClient",
            new[] {IHeroName},
            new[] {TestHelper.GetNamedNonNullStringTypeReference("Name"), friendsReferenceDescriptor},
            isEntityType: true
        );

        public static TypeDescriptor IHeroDescriptor => new(
            "IHero",
            "StarWarsClient",
            new[] {ICharacterName},
            new[] {friendsReferenceDescriptor},
            new[] {HumanHeroTypeDescriptor, DroidHeroTypeDescriptor},
            true
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
