using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Analyzers.Models
{
    public sealed class ComplexOutputTypeModel
        : ITypeModel
    {
        public ComplexOutputTypeModel(
            string name,
            string? description,
            bool isInterface,
            INamedType type,
            SelectionSetNode selectionSet,
            IReadOnlyList<ComplexOutputTypeModel> types,
            IReadOnlyList<OutputFieldModel> fields)
        {
            Name = name;
            Description = description;
            IsInterface = isInterface;
            Type = type;
            SelectionSet = selectionSet;
            Types = types;
            Fields = fields;
        }

        public string Name { get; }

        public string? Description { get; }

        public bool IsInterface { get; }

        public INamedType Type { get; }

        public SelectionSetNode SelectionSet { get; }

        public IReadOnlyList<ComplexOutputTypeModel> Types { get; }

        public IReadOnlyList<OutputFieldModel> Fields { get; }
    }
}
