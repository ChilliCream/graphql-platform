using System.Reflection;
using HotChocolate.Execution;
using HotChocolate.Tests;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate;

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
        public override string GetArgumentName(ParameterInfo parameter)
        {
            var name = base.GetArgumentName(parameter);
            return name + "_Named";
        }

        public override string GetArgumentDescription(ParameterInfo parameter)
        {
            return "GetArgumentDescription";
        }

        public override string GetMemberDescription(MemberInfo member, MemberKind kind)
        {
            return "GetMemberDescription";
        }

        public override string GetTypeName(Type type, TypeKind kind)
        {
            var name = base.GetTypeName(type, kind);
            return name + "_Named";
        }

        public override string GetEnumValueDescription(object value)
        {
            return "GetEnumValueDescription";
        }

        public override string GetMemberName(MemberInfo member, MemberKind kind)
        {
            var name = base.GetMemberName(member, kind);
            return name + "_Named";
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
