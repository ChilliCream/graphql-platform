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
                        hero(episode: NEW_HOPE) @returns(fragment: ""Hero"") {
                            ... Characters
                        }
                    }

                    fragment Characters on Character {
                        ... Human
                        ... Droid
                    }

                    fragment Hero on Character {
                        name
                    }

                    fragment Human on Human {
                        ... Hero
                        homePlanet
                    }

                    fragment Droid on Droid {
                        ... Hero
                        primaryFunction
                    }");

            var context = new DocumentAnalyzerContext(schema, document);
            var selectionSetVariants = context.CollectFields();
            var fieldSelection = selectionSetVariants.ReturnType.Fields.First();
            selectionSetVariants = context.CollectFields(fieldSelection);

            // act
            var list = new List<OutputTypeModel>();
            var returnTypeFragmentName = FragmentHelper.GetReturnTypeName(fieldSelection);
            var returnTypeFragment = FragmentHelper.CreateFragmentNode(
                selectionSetVariants.Variants[0],
                fieldSelection.Path,
                appendTypeName: true);
            returnTypeFragment = FragmentHelper.GetFragment(returnTypeFragment, returnTypeFragmentName!);
            list.Add(FragmentHelper.CreateInterface(context, returnTypeFragment!, fieldSelection.Path));

            foreach (SelectionSet selectionSet in selectionSetVariants.Variants)
            {
                returnTypeFragment = FragmentHelper.CreateFragmentNode(
                    selectionSet,
                    fieldSelection.Path,
                    appendTypeName: true);
                OutputTypeModel @interface = FragmentHelper.CreateInterface(context, returnTypeFragment, fieldSelection.Path);
                OutputTypeModel @class = FragmentHelper.CreateClass(context, returnTypeFragment, selectionSet, @interface);

                list.Add(@interface);
                list.Add(@class);
            }
        }

        [Fact]
        public async Task Interface_With_Fragment_Definition_Two_Models_2()
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
                        hero(episode: NEW_HOPE) @returns(fragment: ""hero"") {
                            ... Characters
                        }
                    }

                    fragment Characters on Character {
                        ... Human
                        ... Droid
                    }

                    fragment Hero on Character {
                        name
                    }

                    fragment Human on Human {
                        ... Hero
                        homePlanet
                    }

                    fragment Droid on Droid {
                        ... Hero
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
                OutputTypeModel @interface = FragmentHelper.CreateInterface(context, returnTypeFragment, fieldSelection.Path);
                OutputTypeModel @class = FragmentHelper.CreateClass(context, returnTypeFragment, selectionSet, @interface);

                list.Add(@interface);
                list.Add(@class);
            }
        }
    }
}
