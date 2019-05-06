using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Filters
{

    public interface IObjectFilterFieldDescriptor
    {
        IObjectFilterFieldDescriptor BindFilters(
            BindingBehavior bindingBehavior);


        IObjectFilterFieldDetailsDescriptor AllowObject(); 
         
    }

    public interface IObjectFilterFieldDetailsDescriptor
        : IDescriptor<InputFieldDefinition>
        , IFluent
    {
        IObjectFilterFieldDescriptor And();

        IObjectFilterFieldDetailsDescriptor Name(NameString value);

        IObjectFilterFieldDetailsDescriptor Description(string value);

        IObjectFilterFieldDetailsDescriptor Directive<T>(T directiveInstance)
            where T : class;

        IObjectFilterFieldDetailsDescriptor Directive<T>()
            where T : class, new();

        IObjectFilterFieldDetailsDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments);
    }
}
