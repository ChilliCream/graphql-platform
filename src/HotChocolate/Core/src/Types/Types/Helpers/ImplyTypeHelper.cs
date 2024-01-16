#nullable enable
using System;
using System.Diagnostics;

namespace HotChocolate.Types.Descriptors;

internal static class ImplyTypeHelper
{
    /// <summary>
    /// Adds RuntimeType on the generic analog of the most nested type, for NonNullType and ListType.
    /// </summary>
    /// <param name="type">
    /// Either the concrete type that will be returned,
    /// or the type that will be used for implying the desired type.
    /// Can be any valid combination of <c>NonNullType</c> and <c>ListType</c> (up to 4 levels deep).
    /// </param>
    /// <param name="objectType">Expected to not be null if any implying will take place.</param>
    /// <returns></returns>
    /// <example>
    /// <c>NonNullType{ListType{NonNullType}}</c> with an object type like <c>ExampleType</c>
    /// would become <c>NonNullType{ListType{NonNullType{ExampleType}}}</c>.
    /// </example>
    /// <exception cref="ArgumentException">
    /// When <paramref name="type"/> is not a valid combination of <c>NonNullType</c> and <c>ListType</c>.
    /// Also, when <paramref name="objectType"/> is null when it's needed.
    /// </exception>
    public static Type ImplyType(Type type, Type? objectType)
    {
        // TODO: Thread local? Or some context thing?
        var typeCache = new Type[1];
        Type Wrap(Type t, Type into)
        {
            typeCache[0] = t;
            return into.MakeGenericType(typeCache);
        }

        const int maxWrapCounter = 4;
        var unwrappedTypes = new Type[maxWrapCounter];
        var currentType = type;
        var previousKind = PreviousKind.None;
        int wrapCounter = 0;
        while (wrapCounter < maxWrapCounter)
        {
            void DoThing(PreviousKind newKind)
            {
                if (previousKind == newKind)
                {
                    throw new ArgumentException(nameof(type));
                }
                previousKind = newKind;

#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
                unwrappedTypes[wrapCounter] = newKind switch
                {
                    PreviousKind.List => typeof(ListType<>),
                    PreviousKind.NonNull => typeof(NonNullType<>),
                };
#pragma warning restore CS8509

                wrapCounter++;
            }
            void DoList() => DoThing(PreviousKind.List);
            void DoNonNull() => DoThing(PreviousKind.NonNull);

            if (currentType == typeof(NonNullType))
            {
                DoList();
                break;
            }

            if (currentType == typeof(ListType))
            {
                DoNonNull();
                break;
            }

            if (!currentType.IsGenericType)
            {
                if (wrapCounter == 0)
                {
                    return type;
                }

                throw new ArgumentException(nameof(type));
            }

            var genericDefinition = currentType.GetGenericTypeDefinition();
            if (genericDefinition == typeof(NonNullType<>))
            {
                DoNonNull();
            }
            else if (genericDefinition == typeof(ListType<>))
            {
                DoList();
            }
            else
            {
                throw new ArgumentException(nameof(type));
            }
            currentType = currentType.GetGenericArguments()[0];
        }

        Debug.Assert(wrapCounter > 0);

        if (objectType is null)
        {
            throw new InvalidOperationException(nameof(objectType));
        }

        var t = objectType;
        do
        {
            t = Wrap(t, unwrappedTypes[wrapCounter]);
            wrapCounter--;
        }
        while (wrapCounter >= 0);

        return t;

    }

    private enum PreviousKind
    {
        List,
        NonNull,
        None,
    }
}
