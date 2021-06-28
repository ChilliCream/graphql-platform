using System;
 using System.Reflection;

 namespace HotChocolate.Resolvers.Expressions.Parameters
 {
     internal abstract class CustomContextCompilerBase<T>
         : ResolverParameterCompilerBase<T>
         where T : IResolverContext
     {
         public CustomContextCompilerBase()
         {
             ContextData = typeof(IHasContextData)
                 .GetTypeInfo().GetDeclaredProperty(
                     nameof(IResolverContext.ContextData));
             ScopedContextData = ContextTypeInfo.GetDeclaredProperty(
                 nameof(IResolverContext.ScopedContextData));
             LocalContextData = ContextTypeInfo.GetDeclaredProperty(
                 nameof(IResolverContext.LocalContextData));
         }

         protected PropertyInfo ContextData { get; }

         protected PropertyInfo ScopedContextData { get; }

         protected PropertyInfo LocalContextData { get; }

         protected bool IsSetter(Type parameterType)
         {
             if (parameterType == typeof(SetState))
             {
                 return true;
             }

             if (parameterType.IsGenericType
                 && parameterType.GetGenericTypeDefinition() == typeof(SetState<>))
             {
                 return true;
             }

             return false;
         }
     }
 }
