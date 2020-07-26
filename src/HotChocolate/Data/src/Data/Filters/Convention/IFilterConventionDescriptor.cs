using System;

namespace HotChocolate.Data.Filters
{
    public interface IFilterConventionDescriptor
    {
        IFilterOperationConventionDescriptor Operation(int operation);

        IFilterConventionDescriptor Binding<TRuntime, TInput>();
<<<<<<< HEAD

        IFilterConventionDescriptor Extension<TFilterType>(
            Action<IFilterInputTypeDescriptor> extension)
            where TFilterType : FilterInputType;

        IFilterConventionDescriptor Extension(
            NameString typeName,
            Action<IFilterInputTypeDescriptor> extension);

        IFilterConventionDescriptor Extension<TFilterType, TType>(
            Action<IFilterInputTypeDescriptor<TType>> extension)
            where TFilterType : FilterInputType<TType>;
=======
>>>>>>> develop
    }
}
