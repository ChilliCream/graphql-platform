using System;
using System.Collections.Generic;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Analyzers.Models
{
    public sealed class OutputTypeModel : ITypeModel
    {
        public OutputTypeModel(
            NameString name,
            string? description,
            bool isInterface,
            INamedType type,
            SelectionSetNode selectionSet,
            IReadOnlyList<OutputFieldModel> fields,
            IReadOnlyList<OutputTypeModel>? implements = null)
        {
            Name = name.EnsureNotEmpty(nameof(name));
            Description = description;
            IsInterface = isInterface;
            Type = type ??
                throw new ArgumentNullException(nameof(type));
            SelectionSet = selectionSet ??
                throw new ArgumentNullException(nameof(selectionSet));
            Fields = fields ??
                throw new ArgumentNullException(nameof(fields));
            Implements = implements ?? Array.Empty<OutputTypeModel>();
        }

        public NameString Name { get; }

        public string? Description { get; }

        public bool IsInterface { get; }

        public INamedType Type { get; }

        public SelectionSetNode SelectionSet { get; }

        public IReadOnlyList<OutputTypeModel> Implements { get; }

        public IReadOnlyList<OutputFieldModel> Fields { get; }

        public override string ToString()
        {
            return Name;
        }
    }
}
