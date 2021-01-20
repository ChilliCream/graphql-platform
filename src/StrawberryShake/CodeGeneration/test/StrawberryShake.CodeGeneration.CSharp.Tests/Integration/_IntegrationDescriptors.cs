using System;
using System.Collections.ObjectModel;
using HotChocolate;
using HotChocolate.Types;
using StrawberryShake.CodeGeneration;
using TypeKind = StrawberryShake.CodeGeneration.TypeKind;

namespace StrawberryShake.Integration
{
    public static class IntegrationDescriptors
    {
        private static readonly NameString _characterName = "ICharacter";
        private static readonly NameString _heroName = "IHero";

        public static NamedTypeDescriptor CreateDroidNamedTypeDescriptor() => new(
            "Droid",
            "StarWarsClient",
            new[] { _characterName },
            new[] { TestHelper.GetNamedNonNullStringTypeReference("Name") },
            kind: TypeKind.EntityType,
            graphQLTypeName: "Droid");

        public static NamedTypeDescriptor CreateHumanNamedTypeDescriptor() => new(
            "Human",
            "StarWarsClient",
            new[] { _characterName },
            new[] { TestHelper.GetNamedNonNullStringTypeReference("Name") },
            kind: TypeKind.EntityType,
            graphQLTypeName: "Human");

        public static NamedTypeDescriptor CreateCharacterDescriptor() => new(
            _characterName,
            "StarWarsClient",
            implementedBy: new[]
            {
                CreateDroidNamedTypeDescriptor(),
                CreateHumanNamedTypeDescriptor()
            },
            kind: TypeKind.EntityType);

        public static ITypeDescriptor CreateFriendsConnectionDescriptor() =>
            new NonNullTypeDescriptor(new NamedTypeDescriptor(
            "FriendsConnection",
            "StarWarsClient",
            properties: new[]
            {
                new PropertyDescriptor(
                    "Nodes",
                    new NonNullTypeDescriptor(
                        new ListTypeDescriptor(
                            new NonNullTypeDescriptor(CreateCharacterDescriptor())))),
                TestHelper.GetNamedNonNullIntTypeReference("TotalCount")
            },
            kind: TypeKind.DataType));

        private static PropertyDescriptor CreateFriendsMemberDescriptor() => new(
            "Friends",
            CreateFriendsConnectionDescriptor());

        public static NamedTypeDescriptor CreateHumanHeroNamedTypeDescriptor() => new(
            "HumanHero",
            "StarWarsClient",
            new[] { _heroName },
            new[]
            {
                TestHelper.GetNamedNonNullStringTypeReference("Name"),
                CreateFriendsMemberDescriptor()
            },
            kind: TypeKind.EntityType,
            graphQLTypeName: "Human");

        public static NamedTypeDescriptor CreateDroidHeroNamedTypeDescriptor() => new(
            "DroidHero",
            "StarWarsClient",
            new[] { _heroName },
            new[]
            {
                TestHelper.GetNamedNonNullStringTypeReference("Name"),
                CreateFriendsMemberDescriptor()
            },
            kind: TypeKind.EntityType,
            graphQLTypeName: "Droid");

        public static ITypeDescriptor CreateHeroDescriptor() => new NonNullTypeDescriptor (new NamedTypeDescriptor(
            _heroName,
            "StarWarsClient",
            new[] { _characterName },
            new[] { CreateFriendsMemberDescriptor() },
            new[]
            {
                CreateHumanHeroNamedTypeDescriptor(),
                CreateDroidHeroNamedTypeDescriptor()
            },
            TypeKind.EntityType));

        public static EntityTypeDescriptor CreateDroidEntityTypeDescriptor() => new(
            "Droid",
            "StarWarsClient",
            new[]
            {
                CreateDroidNamedTypeDescriptor(),
                CreateDroidHeroNamedTypeDescriptor()
            });

        public static EntityTypeDescriptor CreateHumanEntityTypeDescriptor() => new(
            "Human",
            "StarWarsClient",
            new[]
            {
                CreateHumanNamedTypeDescriptor(),
                CreateHumanHeroNamedTypeDescriptor()
            });

        public static NamedTypeDescriptor CreateGetHeroResultDescriptor() => new(
            "GetHeroResult",
            "StarWarsClient",
            Array.Empty<NameString>(),
            new[]
            {
                new PropertyDescriptor(
                    "Hero",
                    CreateHeroDescriptor()),
                TestHelper.GetNamedNonNullStringTypeReference("Version")
            });

        public static QueryOperationDescriptor CreateGetHeroQueryDescriptor() =>
            new(
                CreateGetHeroResultDescriptor(),
                "StarWarsClient",
                new Collection<PropertyDescriptor>(),
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

        public static ResultBuilderDescriptor CreateGetHeroResultBuilderDescriptor() =>
            new(
                CreateGetHeroResultDescriptor(),
                new[]
                {
                    new ValueParserDescriptor("string", "string", "String"),
                    new ValueParserDescriptor("int", "int", "Int")
                });
    }
}
