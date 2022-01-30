using System;
using HotChocolate.Types;

namespace HotChocolate.Caching;

// public static class InterfaceTypeDescriptorExtensions
// {
//     public static IInterfaceTypeDescriptor CacheControl(
//         this IInterfaceTypeDescriptor descriptor,
//         int? maxAge = null, CacheControlScope? scope = null,
//         bool? inheritMaxAge = null)
//     {
//         if (descriptor is null)
//         {
//             throw new ArgumentNullException(nameof(descriptor));
//         }

//         return descriptor.Directive(new CacheControlDirective(maxAge, scope, inheritMaxAge));
//     }

//     public static IInterfaceTypeDescriptor<T> CacheControl<T>(
//         this IInterfaceTypeDescriptor<T> descriptor,
//         int? maxAge = null, CacheControlScope? scope = null,
//         bool? inheritMaxAge = null)
//     {
//         if (descriptor is null)
//         {
//             throw new ArgumentNullException(nameof(descriptor));
//         }

//         return descriptor.Directive(new CacheControlDirective(maxAge, scope, inheritMaxAge));
//     }
// }