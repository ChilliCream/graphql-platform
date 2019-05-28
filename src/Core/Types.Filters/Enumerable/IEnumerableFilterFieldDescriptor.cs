using System;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Filters
{

    public interface IEnumerableFilterFieldDescriptor
    {
        IEnumerableFilterFieldDescriptor BindFilters(
            BindingBehavior bindingBehavior);

        IEnumerableFilterFieldDetailsDescriptor AllowSome();

    }

    public interface IEnumerableFilterFieldDetailsDescriptor
        : IDescriptor<InputFieldDefinition>
        , IFluent
    {
        IEnumerableFilterFieldDescriptor And();

        IEnumerableFilterFieldDetailsDescriptor Name(NameString value);

        IEnumerableFilterFieldDetailsDescriptor Description(string value);

        IEnumerableFilterFieldDetailsDescriptor Directive<TDirective>(TDirective directiveInstance)
            where TDirective : class;

        IEnumerableFilterFieldDetailsDescriptor Directive<TDirective>()
            where TDirective : class, new();

        IEnumerableFilterFieldDetailsDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments);
    }
}
