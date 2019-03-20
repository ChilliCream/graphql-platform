using System.Linq;
using System;
using System.Collections.Generic;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;

namespace HotChocolate
{
    internal class TypeInitializer
    {
        private readonly List<InitializationContext> _initContexts =
            new List<InitializationContext>();
        private readonly Dictionary<ITypeReference, RegisteredType> _types =
            new Dictionary<ITypeReference, RegisteredType>();
        private readonly Dictionary<ITypeReference, ITypeReference> _clrTypes =
            new Dictionary<ITypeReference, ITypeReference>();
        private readonly Dictionary<FieldReference, RegisteredResolver> _res =
            new Dictionary<FieldReference, RegisteredResolver>();
        private readonly List<FieldMiddleware> _globalComps =
            new List<FieldMiddleware>();
        private readonly List<ISchemaError> _errors =
            new List<ISchemaError>();

        private readonly IServiceProvider _services;
        private readonly List<ITypeReference> _initialTypes;

        private bool RegisterTypes()
        {
            var typeRegistrar = new TypeRegistrar_new(_initialTypes, _services);
            if (typeRegistrar.Complete())
            {
                foreach (InitializationContext context in
                    typeRegistrar.InitializationContexts)
                {
                    foreach (FieldReference reference in context.Resolvers.Keys)
                    {
                        if (!_res.ContainsKey(reference))
                        {
                            _res[reference] = context.Resolvers[reference];
                        }
                    }
                    _initContexts.Add(context);
                }

                foreach (ITypeReference key in typeRegistrar.Registerd.Keys)
                {
                    _types[key] = typeRegistrar.Registerd[key];
                }

                foreach (ITypeReference key in typeRegistrar.ClrTypes.Keys)
                {
                    _clrTypes[key] = typeRegistrar.ClrTypes[key];
                }
                return true;
            }

            _errors.AddRange(typeRegistrar.Errors);
            return false;
        }

        private void CompileResolvers() =>
            ResolverCompiler.Compile(_res);
    }
}
