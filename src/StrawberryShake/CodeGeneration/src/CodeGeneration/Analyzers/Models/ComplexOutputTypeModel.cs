using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Analyzers.Models
{
    public class ComplexOutputTypeModel
        : ITypeModel
    {
        public ComplexOutputTypeModel(
            string name,
            string? description,
            INamedType type,
            SelectionSetNode selectionSet,
            IReadOnlyList<ComplexOutputTypeModel> types,
            IReadOnlyList<OutputFieldModel> fields)
        {
            Name = name;
            Description = description;
            Type = type;
            SelectionSet = selectionSet;
            Types = types;
            Fields = fields;
        }

        public string Name { get; }

        public string? Description { get; }

        public INamedType Type { get; }

        public SelectionSetNode SelectionSet { get; }

        public IReadOnlyList<ComplexOutputTypeModel> Types { get; }

        public IReadOnlyList<OutputFieldModel> Fields { get; }

    }
}
