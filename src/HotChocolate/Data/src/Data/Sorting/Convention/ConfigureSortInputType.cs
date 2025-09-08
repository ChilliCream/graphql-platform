namespace HotChocolate.Data.Sorting;

public delegate void ConfigureSortInputType(ISortInputTypeDescriptor descriptor);

public delegate void ConfigureSortInputType<T>(ISortInputTypeDescriptor<T> descriptor);

public delegate void ConfigureSortEnumType(ISortEnumTypeDescriptor descriptor);
