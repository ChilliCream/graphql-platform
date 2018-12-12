using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Utilities;

namespace HotChocolate.Types
{
    public class Edge
    {
        public static void Foo(IQueryable<string> s)
        {
        }
    }

    public class PageInfo
    {
        public bool HasNextPage { get; set; }

        public bool HasPreviousPage { get; set; }
    }

    public class Connection<T>
    {
        public PageInfo PageInfo { get; set; }

        public ICollection<Edge<T>> Edges { get; set; }
    }

    public class Edge<T>
    {
        public string Cursor { get; set; }

        public T Node { get; set; }
    }

    public class EdgeType<T> : ObjectType
        where T : IOutputType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Name("{0}");
            descriptor.Field("cursor").Type<NonNullType<StringType>>();
            descriptor.Field("node").Type<T>();
        }

        protected override
    }

    public class PageInfoType : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Name("PageInfo");

            descriptor.Field("hasNextPage")
                .Type<NonNullType<BooleanType>>();

            descriptor.Field("hasPreviousPage")
                .Type<NonNullType<BooleanType>>();
        }
    }

    public class ConnectionType<T> : ObjectType
        where T : IOutputType
    {

        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Name("{0}");

            descriptor.Field("pageInfo")
                .Type<NonNullType<PageInfoType>>();

            descriptor.Field("edges")
                .Type<ListType<NonNullType<EdgeType<T>>>>();
        }
    }
}
