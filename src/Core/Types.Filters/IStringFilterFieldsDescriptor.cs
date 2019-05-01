using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Filters
{

    public interface IStringFilterFieldDescriptor
    {
        IStringFilterFieldDescriptor BindFilters(
            BindingBehavior bindingBehavior);

        IStringFilterFieldDetailsDescriptor AllowContains();

        IStringFilterFieldDetailsDescriptor AllowEquals();

        IStringFilterFieldDetailsDescriptor AllowIn();
    }

    public interface IStringFilterFieldDetailsDescriptor
        : IDescriptor<InputFieldDefinition>
        , IFluent
    {
        IStringFilterFieldDescriptor And();

        IStringFilterFieldDetailsDescriptor Name(NameString value);

        IStringFilterFieldDetailsDescriptor Description(string value);

        IStringFilterFieldDetailsDescriptor Directive<T>(T directiveInstance)
            where T : class;

        IStringFilterFieldDetailsDescriptor Directive<T>()
            where T : class, new();

        IStringFilterFieldDetailsDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments);
    }

    public class Dummy
    {

        public void Foo(IFilterInputObjectTypeDescriptor<Foo> descriptor)
        {
            descriptor
                .BindFields(BindingBehavior.Explicit)
                .Filter(t => t.Bar)
                .BindFilters(BindingBehavior.Explicit)
                .AllowContains()
                .Name("bar_contains")
                .And()
                .AllowIn()
                .Name("bar_in");
        }
    }

    public class Foo
    {
        public string Bar { get; }
    }
}
