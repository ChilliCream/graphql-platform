using System;
using System.Collections.Generic;
using System.Linq;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Language
{
    public class SelectionSetNodeTests
    {
        [Fact]
        public void CreateSelectionSet()
        {
            // arrange
            Location location = AstTestHelper.CreateDummyLocation();
            var selections = new List<ISelectionNode>
            {
                new FieldNode
                (
                    null,
                    new NameNode("bar"),
                    null,
                    Array.Empty<DirectiveNode>(),
                    Array.Empty<ArgumentNode>(),
                    null
                )
            };

            // act
            var selectionSet = new SelectionSetNode
            (
                location,
                selections
            );

            // assert
            selectionSet.MatchSnapshot();
        }

        [Fact]
        public void WithLocation()
        {
            // arrange
            Location location = AstTestHelper.CreateDummyLocation();
            var selections = new List<ISelectionNode>
            {
                new FieldNode
                (
                    null,
                    new NameNode("bar"),
                    null,
                    Array.Empty<DirectiveNode>(),
                    Array.Empty<ArgumentNode>(),
                    null
                )
            };

            var selectionSet = new SelectionSetNode
            (
                null,
                selections
            );

            // act
            selectionSet = selectionSet.WithLocation(location);

            // assert
            selectionSet.MatchSnapshot();
        }

        [Fact]
        public void AddSelection()
        {
            // arrange
            Location location = AstTestHelper.CreateDummyLocation();
            var selections = new List<ISelectionNode>
            {
                new FieldNode
                (
                    null,
                    new NameNode("bar"),
                    null,
                    Array.Empty<DirectiveNode>(),
                    Array.Empty<ArgumentNode>(),
                    null
                )
            };

            var selectionSet = new SelectionSetNode
            (
                location,
                selections
            );

            // act
            selectionSet = selectionSet.AddSelection(
                new FieldNode
                (
                    null,
                    new NameNode("baz"),
                    null,
                    Array.Empty<DirectiveNode>(),
                    Array.Empty<ArgumentNode>(),
                    null
                ));

            // assert
            selectionSet.MatchSnapshot();
        }

        [Fact]
        public void AddSelections()
        {
            // arrange
            Location location = AstTestHelper.CreateDummyLocation();
            var selections = new List<ISelectionNode>
            {
                new FieldNode
                (
                    null,
                    new NameNode("bar"),
                    null,
                    Array.Empty<DirectiveNode>(),
                    Array.Empty<ArgumentNode>(),
                    null
                )
            };

            var selectionSet = new SelectionSetNode
            (
                location,
                selections
            );

            // act
            selectionSet = selectionSet.AddSelections(
                new FieldNode
                (
                    null,
                    new NameNode("baz"),
                    null,
                    Array.Empty<DirectiveNode>(),
                    Array.Empty<ArgumentNode>(),
                    null
                ));

            // assert
            selectionSet.MatchSnapshot();
        }

        [Fact]
        public void AddSelections_Two()
        {
            // arrange
            Location location = AstTestHelper.CreateDummyLocation();
            var selections = new List<ISelectionNode>
            {
                new FieldNode
                (
                    null,
                    new NameNode("bar"),
                    null,
                    Array.Empty<DirectiveNode>(),
                    Array.Empty<ArgumentNode>(),
                    null
                )
            };

            var selectionSet = new SelectionSetNode
            (
                location,
                selections
            );

            // act
            selectionSet = selectionSet.AddSelections(
                new FieldNode
                (
                    null,
                    new NameNode("baz"),
                    null,
                    Array.Empty<DirectiveNode>(),
                    Array.Empty<ArgumentNode>(),
                    null
                ),
                new FieldNode
                (
                    null,
                    new NameNode("qux"),
                    null,
                    Array.Empty<DirectiveNode>(),
                    Array.Empty<ArgumentNode>(),
                    null
                ));

            // assert
            selectionSet.MatchSnapshot();
        }

        [Fact]
        public void RemoveSelection()
        {
            // arrange
            Location location = AstTestHelper.CreateDummyLocation();
            var selections = new List<ISelectionNode>
            {
                new FieldNode
                (
                    null,
                    new NameNode("bar"),
                    null,
                    Array.Empty<DirectiveNode>(),
                    Array.Empty<ArgumentNode>(),
                    null
                ),
                new FieldNode
                (
                    null,
                    new NameNode("baz"),
                    null,
                    Array.Empty<DirectiveNode>(),
                    Array.Empty<ArgumentNode>(),
                    null
                )
            };

            var selectionSet = new SelectionSetNode
            (
                location,
                selections
            );

            // act
            selectionSet = selectionSet.RemoveSelection(
                selectionSet.Selections.First());

            // assert
            selectionSet.MatchSnapshot();
        }

        [Fact]
        public void RemoveSelections()
        {
            // arrange
            Location location = AstTestHelper.CreateDummyLocation();
            var selections = new List<ISelectionNode>
            {
                new FieldNode
                (
                    null,
                    new NameNode("bar"),
                    null,
                    Array.Empty<DirectiveNode>(),
                    Array.Empty<ArgumentNode>(),
                    null
                ),
                new FieldNode
                (
                    null,
                    new NameNode("baz"),
                    null,
                    Array.Empty<DirectiveNode>(),
                    Array.Empty<ArgumentNode>(),
                    null
                )
            };

            var selectionSet = new SelectionSetNode
            (
                location,
                selections
            );

            // act
            selectionSet = selectionSet.RemoveSelections(
                selectionSet.Selections.First());

            // assert
            selectionSet.MatchSnapshot();
        }

        [Fact]
        public void WithSelections()
        {
            // arrange
            Location location = AstTestHelper.CreateDummyLocation();
            var selections = new List<ISelectionNode>
            {
                new FieldNode
                (
                    null,
                    new NameNode("bar"),
                    null,
                    Array.Empty<DirectiveNode>(),
                    Array.Empty<ArgumentNode>(),
                    null
                )
            };

            var selectionSet = new SelectionSetNode
            (
                location,
                selections
            );

            // act
            selectionSet = selectionSet.WithSelections(
                new List<ISelectionNode>
                {
                    new FieldNode
                    (
                        null,
                        new NameNode("baz"),
                        null,
                        Array.Empty<DirectiveNode>(),
                        Array.Empty<ArgumentNode>(),
                        null
                    )
                });

            // assert
            selectionSet.MatchSnapshot();
        }
    }
}
