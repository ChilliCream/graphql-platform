using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Configuration
{




    /*
        c.RegisterScalar<StringType>(new StringType());
        c.RegisterScalar(new StringType());

        c.BindType<T>().To("xyz")

        c.BindResolver<A>().To("foo");
        c.BindResolver<A>().To("foo")
            .WithMapping(m => m.From(t => t.y).To("x"));

        c.BindResolver<A>().To<B>()
            .WithMapping(m => m.From(t => t.y).To(t => t.x));

        c.BindResolver<A>().To<B>()
            .WithMapping(
                m => m.From(t => t.y).To(t => t.x).And()
                    .From(t => t.y).To("x")).And()
         .BindResolver...

        c.BindResolver(delegate).To("foo", "bar");

       c.BindType<X>().ToQuery();
       c.BindType<X>().ToMutation();
       c.BindType<X>().ToSubscription();

     */

    public class TestApi
    {
        public TestApi(ISchemaConfiguration c)
        {
            c.BindResolver<TestResolverApi>(BindingBehavior.Explicit)
                .To("foo");

            c.BindResolver<TestResolverApi>()
                .To("foo");

            c.BindResolver<TestResolverApi>();

            c.BindResolver<TestResolverApi>().To<Query>();

            c.BindResolver<TestResolverApi>()
                .To<Query>()
                .Resolve(t => t.X).With(t => t.GetX(It.Is<string>()))
                .Resolve("field").With(t => t.GetX(It.Is<string>()));

            c.BindResolver<TestResolverApi>(BindingBehavior.Explicit)
                .To<Query>()
                .Resolve(t => t.X).With(t => t.GetX(It.Is<string>()))
                .Resolve("field").With(t => t.GetX(It.Is<string>()));

            c.BindResolver((ctx, ct) => "string").To("Query", "field");
            c.BindResolver((ctx, ct) => "string").To<Query>(t => t.X);


            c.BindType<Query>()
                .To("Query");

            c.BindType<Query>(BindingBehaviour.Explicit)
                .To("Query")
                .Bind(t => t.X, "a")
                .Bind(t => t.X).To("a")
                .Field("a").Is(t => this.X)
                .Map(hjhgj)
                .Member(t => t.X).WithName("a")
                .BindMember(t => t.X).ToField("a");

            c.BindType<Query>();

            c.BindType<Query>()
                .WithFields(m => m.Bind(t => t.X).To("a"));

            c.RegisterScalar(new StringType())
                .And();

            c.RegisterScalar<StringType>();
        }
    }

    public class TestResolverApi
    {
        public string GetX(string f) { return ""; }
        public string X() { return ""; }
        public string Y { get; }
    }

    public class Query
    {
        public string X { get; }
    }

    public interface ISchemaConfiguration
        : IFluent
    {
        IBindResolverDelegate BindResolver(AsyncFieldResolverDelegate fieldResolver);
        IBindResolverDelegate BindResolver(FieldResolverDelegate fieldResolver);

        IBindResolver<TResolver> BindResolver<TResolver>()
            where TResolver : class;

        IBindResolver<TResolver> BindResolver<TResolver>(BindingBehavior bindingBehavior)
            where TResolver : class;

        IBindType<T> BindType<T>()
            where T : class;

        void RegisterScalar<T>(T scalarType)
            where T : ScalarType;

        void RegisterScalar<T>()
            where T : ScalarType, new();
    }

    public interface IFluent
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        bool Equals(object obj);

        [EditorBrowsable(EditorBrowsableState.Never)]
        int GetHashCode();

        [EditorBrowsable(EditorBrowsableState.Never)]
        string ToString();
    }

    public interface IBindType<T>
        : IBoundType<T>
    {
        IBoundType<T> To(string typeName);
    }

    public interface IBoundType<T>
       : IFluent
    {
        IBindField<T> Field<TPropertyType>(Expression<Func<T, TPropertyType>> field);
    }

    public interface IBindField<T>
    {
        IBoundType<T> Name(string fieldName);
    }


    public interface IBindResolver<TResolver>
        : IBoundResolver<TResolver>
    {
        IBoundResolver<TResolver> To(string typeName);

        IBoundResolver<TResolver, TObjectType> To<TObjectType>();
    }

    public interface IBoundResolver<TResolver>
        : IFluent
    {
        IBindFieldResolver<TResolver> Resolve(string fieldName);

    }

    public interface IBoundResolver<TResolver, TObjectType>
        : IBoundResolver<TResolver>
    {
        IBindFieldResolver<TResolver> Resolve<TPropertyType>(Expression<Func<TObjectType, TPropertyType>> field);
    }


    public interface IBindFieldResolver<TResolver>
        : IFluent
    {
        IBoundResolver<TResolver> With<TPropertyType>(Expression<Func<TResolver, TPropertyType>> resolver);
    }

    public interface IBindFieldResolver<TResolver, TObjectType>
       : IFluent
    {
        IBoundResolver<TResolver, TObjectType> With<TPropertyType>(Expression<Func<TResolver, TPropertyType>> resolver);
    }

    public interface IBindResolverDelegate
        : IFluent
    {
        void To(string typeName, string fieldName);
        void To<TObjectType>(Expression<Func<TObjectType, object>> resolver);
    }


    public enum BindingBehavior
    {
        Implicit = 0,
        Explicit = 1
    }

    internal class ResolverBindingInfo
    {
        public Type ObjectType { get; set; }
        public string ObjectTypeName { get; set; }
    }

    internal class ResolverDelegateBindingInfo
        : ResolverBindingInfo
    {
        public string FieldName { get; set; }
        public MemberInfo FieldMember { get; set; }
        public AsyncFieldResolverDelegate AsyncFieldResolver { get; set; }
        public FieldResolverDelegate FieldResolver { get; set; }

    }

    internal class ResolverCollectionBindingInfo
        : ResolverBindingInfo

    {
        public Type ResolverCollection { get; set; }

        public BindingBehavior Behavior { get; set; }

        public List<FieldResolverBindungInfo> Fields { get; } =
            new List<FieldResolverBindungInfo>();
    }

    public class FieldResolverBindungInfo
    {
        public string FieldName { get; set; }
        public MemberInfo FieldMember { get; set; }
        public MemberInfo ResolverMember { get; set; }
    }

    internal class BindResolver<TResolver>
        : IBindResolver<TResolver>
    {
        private readonly ResolverCollectionBindingInfo _bindingInfo;

        public BindResolver(ResolverCollectionBindingInfo bindingInfo)
        {
            if (bindingInfo == null)
            {
                throw new ArgumentNullException(nameof(bindingInfo));
            }

            _bindingInfo = bindingInfo;
        }

        public IBindFieldResolver<TResolver> Resolve(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName))
            {
                throw new ArgumentNullException(nameof(fieldName));
            }

            FieldResolverBindungInfo fieldBindingInfo =
                new FieldResolverBindungInfo
                {
                    FieldName = fieldName
                };
            _bindingInfo.Fields.Add(fieldBindingInfo);
            return new BindFieldResolver<TResolver>(
                _bindingInfo, fieldBindingInfo);
        }

        public IBoundResolver<TResolver> To(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            _bindingInfo.ObjectTypeName = typeName;
            return this;
        }

        public IBoundResolver<TResolver, TObjectType> To<TObjectType>()
        {
            throw new NotImplementedException();
        }
    }

    public class BindFieldResolver<TResolver>
        : IBindFieldResolver<TResolver>
    {
        private readonly ResolverCollectionBindingInfo _bindingInfo;
        private readonly FieldResolverBindungInfo _fieldBindingInfo;

        internal BindFieldResolver(
            ResolverCollectionBindingInfo bindingInfo,
            FieldResolverBindungInfo fieldBindingInfo)
        {
            if (bindingInfo == null)
            {
                throw new ArgumentNullException(nameof(bindingInfo));
            }

            if (fieldBindingInfo == null)
            {
                throw new ArgumentNullException(nameof(fieldBindingInfo));
            }

            _bindingInfo = bindingInfo;
            _fieldBindingInfo = fieldBindingInfo;
        }

        public IBoundResolver<TResolver> With<TPropertyType>(
            Expression<Func<TResolver, TPropertyType>> resolver)
        {
            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            _fieldBindingInfo.ResolverMember = resolver.ExtractMember();
            return new BindResolver<TResolver>(_bindingInfo);
        }
    }

    public class BoundResolver<TResolver>
       : IBoundResolver<TResolver>
    {
        private readonly ResolverCollectionBindingInfo _bindingInfo;

        internal BoundResolver(ResolverCollectionBindingInfo bindingInfo)
        {
            if (bindingInfo == null)
            {
                throw new ArgumentNullException(nameof(bindingInfo));
            }

            _bindingInfo = bindingInfo;
        }

        public IBindFieldResolver<TResolver> Resolve(string fieldName)
        {
            FieldResolverBindungInfo fieldBindingInfo =
                new FieldResolverBindungInfo
                {
                    FieldName = fieldName
                };
            _bindingInfo.Fields.Add(fieldBindingInfo);
            return new BindFieldResolver<TResolver>(
                _bindingInfo, fieldBindingInfo);
        }
    }

    public class BoundResolver<TResolver, TObjectType>
        : BoundResolver<TResolver>
        , IBoundResolver<TResolver, TObjectType>
    {
        private readonly ResolverCollectionBindingInfo _bindingInfo;

        internal BoundResolver(ResolverCollectionBindingInfo bindingInfo)
            : base(bindingInfo)
        {
            if (bindingInfo == null)
            {
                throw new ArgumentNullException(nameof(bindingInfo));
            }

            _bindingInfo = bindingInfo;
        }

        public new IBindFieldResolver<TResolver> Resolve<TPropertyType>(
            Expression<Func<TObjectType, TPropertyType>> field)
        {
            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            FieldResolverBindungInfo fieldBindingInfo =
                new FieldResolverBindungInfo
                {
                    FieldMember = field.ExtractMember()
                };
            _bindingInfo.Fields.Add(fieldBindingInfo);
            return new BindFieldResolver<TResolver>(
                _bindingInfo, fieldBindingInfo);
        }
    }
}
