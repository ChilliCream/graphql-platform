using System.Collections.ObjectModel;
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
            kind: TypeKind.EntityType,
            graphQLTypeName: "Droid"
        );

        public static TypeDescriptor HumanTypeDescriptor => new(
            "Human",
            "StarWarsClient",
            new[] {ICharacterName},
            new[] {TestHelper.GetNamedNonNullStringTypeReference("Name")},
            kind: TypeKind.EntityType,
            graphQLTypeName: "Human"
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
                    "Nodes",
                    new ListTypeDescriptor(
                        false,
                        ICharacterDescriptor
                    )
                ),
                TestHelper.GetNamedNonNullIntTypeReference("TotalCount")
            },
            kind: TypeKind.DataType
        );

        private static NamedTypeReferenceDescriptor friendsReferenceDescriptor => new(
            "Friends",
            FriendsConnectionDescriptor
        );

        public static TypeDescriptor HumanHeroTypeDescriptor => new(
            "HumanHero",
            "StarWarsClient",
            new[] {IHeroName},
            new[] {TestHelper.GetNamedNonNullStringTypeReference("Name"), friendsReferenceDescriptor},
            kind: TypeKind.EntityType,
            graphQLTypeName: "Human"
        );

        public static TypeDescriptor DroidHeroTypeDescriptor => new(
            "DroidHero",
            "StarWarsClient",
            new[] {IHeroName},
            new[] {TestHelper.GetNamedNonNullStringTypeReference("Name"), friendsReferenceDescriptor},
            kind: TypeKind.EntityType,
            graphQLTypeName: "Droid"
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

        public static EntityTypeDescriptor DroidEntityTypeDescriptor => new(
            "Droid",
            "StarWarsClient",
            new[] { DroidTypeDescriptor, DroidHeroTypeDescriptor }
        );

        public static EntityTypeDescriptor HumanEntityTypeDescriptor => new(
            "Human",
            "StarWarsClient",
            new[] { HumanTypeDescriptor, HumanHeroTypeDescriptor });

        public static TypeDescriptor GetHeroResultDescriptor => new(
            "GetHeroResult",
            "StarWarsClient",
            new string[] { },
            new[]
            {
                new NamedTypeReferenceDescriptor(
                    "Hero",
                    IHeroDescriptor
                ),
                TestHelper.GetNamedNonNullStringTypeReference("Version")
            });

        public static QueryOperationDescriptor GetHeroQueryDescriptor =>
            new(
                GetHeroResultDescriptor,
                "StarWarsClient",
                new Collection<NamedTypeReferenceDescriptor>(),
                @"query GetHero {
                    hero {
                        __typename
                        id
                        name
                        friends {
                            nodes {
                                __typename
                                id
                                name
                            }
                            totalCount
                        }
                    }
                    version
                }");

        public static ResultBuilderDescriptor GetHeroResultBuilderDescriptor =>
            new(
                GetHeroResultDescriptor,
                new[]
                {
                    new ValueParserDescriptor("string", "string", "String"),
                    new ValueParserDescriptor("int", "int", "Int")
                });
    }
}
