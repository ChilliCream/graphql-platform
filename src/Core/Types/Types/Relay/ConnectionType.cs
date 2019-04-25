﻿using System;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Relay
{
    public class ConnectionType<T>
        : ObjectType<IConnection>
        , IConnectionType
        where T : IOutputType, new()
    {
        public ConnectionType()
            : base(descriptor => Configure(descriptor))
        {
        }

        public ConnectionType(
            Action<IObjectTypeDescriptor<IConnection>> configure)
            : base(descriptor =>
            {
                Configure(descriptor);
                configure?.Invoke(descriptor);
            })
        {
        }

        public IEdgeType EdgeType { get; private set; }

        protected new static void Configure(
            IObjectTypeDescriptor<IConnection> descriptor)
        {
            descriptor.Name(dependency => dependency.Name + "Connection")
                .DependsOn<T>();

            descriptor.Description("A connection to a list of items.");

            descriptor.BindFields(BindingBehavior.Explicit);

            descriptor.Field(t => t.PageInfo)
                .Name("pageInfo")
                .Description("Information to aid in pagination.")
                .Type<NonNullType<PageInfoType>>();

            descriptor.Field(t => t.Edges)
                .Name("edges")
                .Description("A list of edges.")
                .Type<ListType<NonNullType<EdgeType<T>>>>();
        }

        protected override void OnRegisterDependencies(
            IInitializationContext context,
            ObjectTypeDefinition definition)
        {
            base.OnRegisterDependencies(context, definition);

            context.RegisterDependency(
                ClrTypeReference.FromSchemaType<EdgeType<T>>(),
                TypeDependencyKind.Default);
        }

        protected override void OnCompleteType(
            ICompletionContext context,
            ObjectTypeDefinition definition)
        {
            base.OnCompleteType(context, definition);

            EdgeType = context.GetType<EdgeType<T>>(
                ClrTypeReference.FromSchemaType<EdgeType<T>>());
        }

        public static ConnectionType<T> CreateWithTotalCount()
        {
            return new ConnectionType<T>(c =>
            {
                c.Field("totalCount")
                    .Type<NonNullType<IntType>>()
                    .Resolver(ctx => GetTotalCount(ctx));
            });
        }

        private static IResolverResult<long> GetTotalCount(
            IResolverContext context)
        {
            IConnection connection = context.Parent<IConnection>();
            if (connection.PageInfo.TotalCount.HasValue)
            {
                return ResolverResult.CreateValue(
                    connection.PageInfo.TotalCount.Value);
            }
            return ResolverResult.CreateError<long>(
                "The total count was not provided by the connection.");
        }
    }
}
