using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Types.Descriptors
{
    public class DirectiveTypeDescription
        : TypeDescriptionBase<DirectiveDefinitionNode>
        , IHasClrType
    {
        public bool IsRepeatable { get; set; }

        public Type ClrType { get; set; }

        public IDirectiveMiddleware Middleware { get; set; }

        public ISet<DirectiveLocation> Locations { get; } =
            new HashSet<DirectiveLocation>();

        public IFieldDescriptionList<DirectiveArgumentDescription> Arguments
        { get; } = new FieldDescriptionList<DirectiveArgumentDescription>();

        public override IDescriptionValidationResult Validate()
        {
            throw new NotImplementedException();
        }
    }
}
