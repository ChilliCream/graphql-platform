using System;
using System.Reflection;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Tests;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate
{
    public class NamingConventionTests
    {
        [Fact]
        public void PureCodeFirst_NamingConvention_RenameArgument()
        {
            SchemaBuilder.New()
                .AddQueryType<QueryNamingConvention>()
                .AddMutationType<MutationNamingConvention>()
                .AddConvention<INamingConventions, CustomNamingConvention>()
                .Create()
                .Print()
                .MatchSnapshot();
        }

        [Fact]
        public async Task PureCodeFirst_NamingConvention_RenameArgument_RequestBuilder()
        {
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryNamingConvention>()
                .AddMutationType<MutationNamingConvention>()
                .AddConvention<INamingConventions, CustomNamingConvention>()
                .BuildSchemaAsync()
                .MatchSnapshotAsync();
        }

        public class CustomNamingConvention : DefaultNamingConventions
        {
            public override NameString GetArgumentName(ParameterInfo parameter)
            {
                NameString name = base.GetArgumentName(parameter);
                return name.Value + "_Named";
            }

            public override string GetArgumentDescription(ParameterInfo parameter)
            {
                return "GetArgumentDescription";
            }

            public override string GetMemberDescription(MemberInfo member, MemberKind kind)
            {
                return "GetMemberDescription";
            }

            public override NameString GetTypeName(Type type, TypeKind kind)
            {
                NameString name = base.GetTypeName(type, kind);
                return name.Value + "_Named";
            }

            public override string GetEnumValueDescription(object value)
            {
                return "GetEnumValueDescription";
            }

            public override NameString GetMemberName(MemberInfo member, MemberKind kind)
            {
                NameString name = base.GetMemberName(member, kind);
                return name.Value + "_Named";
            }

            public override string GetTypeDescription(Type type, TypeKind kind)
            {
                return "GetTypeDescription";
            }
        }

        public class QueryNamingConvention
        {
            public ObjectNamingConvention QueryField(
                int queryArgument,
                InputObjectNamingConvention complexArgument) => default!;
        }

        public class InputObjectNamingConvention
        {
            public string InputField { get; set; }
        }

        public class ObjectNamingConvention
        {
            public string OutputField { get; set; }
        }

        public class MutationNamingConvention
        {
            public ObjectNamingConvention MutationField(
                int mutationArgument,
                InputObjectNamingConvention complexArgumentMutation) => default!;
        }
    }
}
