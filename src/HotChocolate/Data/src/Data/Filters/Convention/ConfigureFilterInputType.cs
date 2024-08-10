namespace HotChocolate.Data.Filters;

public delegate void ConfigureFilterInputType(IFilterInputTypeDescriptor descriptor);

public delegate void ConfigureFilterInputType<T>(IFilterInputTypeDescriptor<T> descriptor);
