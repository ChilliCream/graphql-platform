#nullable enable

using System.Collections;

namespace HotChocolate.Configuration;

internal sealed class TypeInterceptorCollection : IList<TypeInterceptorDescriptor>
{
    private readonly List<TypeInterceptorDescriptor> _descriptors = [];

    public TypeInterceptorDescriptor this[int index]
    {
        get => _descriptors[index];
        set => _descriptors[index] = value;
    }

    public int Count => _descriptors.Count;

    public bool IsReadOnly => false;

    public void Add(TypeInterceptorDescriptor item)
        => _descriptors.Add(item);

    void IList<TypeInterceptorDescriptor>.Insert(int index, TypeInterceptorDescriptor item)
        => _descriptors.Insert(index, item);

    public bool Remove(TypeInterceptorDescriptor item)
        => _descriptors.Remove(item);

    void IList<TypeInterceptorDescriptor>.RemoveAt(int index)
        => _descriptors.RemoveAt(index);

    public void Clear() => _descriptors.Clear();

    bool ICollection<TypeInterceptorDescriptor>.Contains(TypeInterceptorDescriptor item)
        => _descriptors.Contains(item);

    int IList<TypeInterceptorDescriptor>.IndexOf(TypeInterceptorDescriptor item)
        => _descriptors.IndexOf(item);

    public void CopyTo(TypeInterceptorDescriptor[] array, int arrayIndex)
        => _descriptors.CopyTo(array, arrayIndex);

    public IEnumerator<TypeInterceptorDescriptor> GetEnumerator()
        => _descriptors.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

internal sealed class TypeInterceptorDescriptor
{
    public TypeInterceptorDescriptor(Type interceptType, Func<IServiceProvider, TypeInterceptor>? factory = null)
    {
        ArgumentNullException.ThrowIfNull(interceptType);

        Type = interceptType;
        Factory = factory;
    }

    public TypeInterceptorDescriptor(TypeInterceptor interceptor)
    {
        ArgumentNullException.ThrowIfNull(interceptor);

        Type = interceptor.GetType();
        Interceptor = interceptor;
    }

    public Type Type { get; }

    public TypeInterceptor? Interceptor { get; }

    public Func<IServiceProvider, TypeInterceptor>? Factory { get; }
}

internal static class TypeInterceptorDescriptorExtensions
{
    public static TypeInterceptorCollection TryAdd(
        this TypeInterceptorCollection collection,
        TypeInterceptor interceptor,
        bool uniqueByType = false)
    {
        if (uniqueByType)
        {
            var type = interceptor.GetType();

            if (collection.Any(t => t.Type == type))
            {
                return collection;
            }
        }
        else
        {
            if (collection.Any(t => t.Interceptor == interceptor))
            {
                return collection;
            }
        }

        collection.Add(new TypeInterceptorDescriptor(interceptor));

        return collection;
    }

    public static TypeInterceptorCollection TryAdd<T>(
        this TypeInterceptorCollection collection,
        Func<IServiceProvider, T> factory)
        where T : TypeInterceptor
    {
        if (collection.Any(t => t.Type == typeof(T)))
        {
            return collection;
        }

        collection.Add(new TypeInterceptorDescriptor(typeof(T), factory));

        return collection;
    }

    public static TypeInterceptorCollection TryAdd(
        this TypeInterceptorCollection collection,
        Type interceptorType)
    {
        if (collection.Any(t => t.Type == interceptorType))
        {
            return collection;
        }

        collection.Add(new TypeInterceptorDescriptor(interceptorType));

        return collection;
    }
}
