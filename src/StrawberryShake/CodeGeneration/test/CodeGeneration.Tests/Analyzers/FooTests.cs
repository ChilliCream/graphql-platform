using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.StarWars;
using Microsoft.Extensions.DependencyInjection;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using Xunit;

namespace StrawberryShake.CodeGeneration.Analyzers
{
    public class FooTests
    {
        [Fact]
        public async Task Interface_With_Fragment_Definition_Two_Models()
        {
            // arrange
            var schema =
                await new ServiceCollection()
                    .AddStarWarsRepositories()
                    .AddGraphQL()
                    .AddStarWars()
                    .BuildSchemaAsync();

            var document =
                Utf8GraphQLParser.Parse(@"
                    query GetHero {
                        hero(episode: NEW_HOPE) {
                            ... Hero
                        }
                    }

                    fragment Hero on Character {
                        ... HasName
                        ... Human
                        ... Droid
                    }

                    fragment HasName on Character {
                        name
                    }

                    fragment Human on Human {
                        homePlanet
                    }

                    fragment Droid on Droid {
                        primaryFunction
                    }");

            var context = new DocumentAnalyzerContext(schema, document);
            var selectionSetVariants = context.CollectFields();
            var fieldSelection = selectionSetVariants.ReturnType.Fields.First();
            selectionSetVariants = context.CollectFields(fieldSelection);

            // act
            var list = new List<OutputTypeModel>();
            var returnTypeFragment = FragmentHelper.CreateFragmentNode(
                selectionSetVariants.ReturnType,
                fieldSelection.Path);
            list.Add(FragmentHelper.CreateInterface(context, returnTypeFragment, fieldSelection.Path));

            foreach (SelectionSet selectionSet in selectionSetVariants.Variants)
            {
                returnTypeFragment = FragmentHelper.CreateFragmentNode(
                    selectionSet,
                    fieldSelection.Path,
                    appendTypeName: true);
                OutputTypeModel type = FragmentHelper.CreateInterface(context, returnTypeFragment, fieldSelection.Path);
                list.Add(type);
            }

        }
    }
}
