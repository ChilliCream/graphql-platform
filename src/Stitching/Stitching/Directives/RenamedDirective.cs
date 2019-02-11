using System;
using HotChocolate.Types;

namespace HotChocolate.Stitching
{
    public class RenamedDirective
    {
        public string Name { get; set; }
    }

    public class RenamedDirectiveType
        : DirectiveType<RenamedDirective>
    {
        protected override void Configure(
            IDirectiveTypeDescriptor<RenamedDirective> descriptor)
        {
            descriptor.Name(DirectiveNames.Renamed)
                .Description("Annotates the original name of a type.");

            descriptor.Location(DirectiveLocation.Enum)
                .Location(DirectiveLocation.Object)
                .Location(DirectiveLocation.Interface)
                .Location(DirectiveLocation.Union)
                .Location(DirectiveLocation.InputObject);

            descriptor.Argument(t => t.Name)
                .Name(DirectiveFieldNames.Renamed_Name)
                .Type<NonNullType<StringType>>()
                .Description("The original name of the annotated type.");
        }
    }
}
