using System;

namespace HotChocolate.Data.Filters
{
    public interface IFilterConventionDescriptor
    {
        IFilterOperationConventionDescriptor Operation(int operation);

        IFilterConventionDescriptor Binding<TRuntime, TInput>();

        IFilterConventionDescriptor Extension<TFilterType>(
            Action<IFilterInputTypeDescriptor> extension)
            where TFilterType : FilterInputType;

        IFilterConventionDescriptor Extension(
            NameString typeName,
            Action<IFilterInputTypeDescriptor> extension);

        IFilterConventionDescriptor Extension<TFilterType, TType>(
            Action<IFilterInputTypeDescriptor<TType>> extension)
            where TFilterType : FilterInputType<TType>;
<<<<<<< HEAD

        IFilterConventionDescriptor Provider<TProvider>()
            where TProvider : FilterProviderBase;

        /// <summary>
        /// Defines the argument name of the filter used by
        /// <see cref="FilterObjectFieldDescriptorExtensions.UseFiltering"/>
        /// </summary> 
        /// <param name="argumentName">The argument name.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="argumentName"/> is <c>null</c> or
        /// <see cref="string.Empty"/>.
        /// </exception>
        IFilterConventionDescriptor ArgumentName(NameString argumentName);

=======
>>>>>>> pse/filter-extensions
    }
}
