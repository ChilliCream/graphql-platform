using System;
using System.Reflection;
using System.Threading.Tasks;
using CookieCrumble;
using HotChocolate.ApolloFederation.Constants;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using Microsoft.Extensions.DependencyInjection;
using DirectiveLocation = HotChocolate.Types.DirectiveLocation;

namespace HotChocolate.ApolloFederation;

public class ComposeDirectiveTests
{
    [Fact]
    public async Task TestServiceTypeEmptyQueryTypePureCodeFirst()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType()
            .AddType<Address>()
            .ExportDirective<Custom>()
            .BuildSchemaAsync();

        var entityType = schema.GetType<ObjectType>(FederationTypeNames.ServiceType_Name);
        var sdlResolver = entityType.Fields[WellKnownFieldNames.Sdl].Resolver!;

        // act
        var value = await sdlResolver(TestHelper.CreateResolverContext(schema));

        Utf8GraphQLParser
            .Parse((string)value!)
            .MatchSnapshot();
    }

    [Key("field")]
    public class Address
    {
        [CustomDirective]
        public string Field => "abc";
    }
    
    [Package("https://specs.custom.dev/custom/v1.0")]
    [DirectiveType(DirectiveLocation.FieldDefinition)]
    public sealed class Custom;
    
    public sealed class CustomDirectiveAttribute : DirectiveAttribute<Custom>
    {
        public CustomDirectiveAttribute() 
            : base(new Custom())
        {
        }
    }

    public abstract class DirectiveAttribute<T>(T directive) : DescriptorAttribute where T : class
    {
        protected override void TryConfigure(
            IDescriptorContext context, 
            IDescriptor descriptor, 
            ICustomAttributeProvider element)
        {
            switch (descriptor)
            {
                case ArgumentDescriptor desc:
                    desc.Directive(directive);
                    break;

                case DirectiveArgumentDescriptor desc:
                    desc.Directive(directive);
                    break;

                case EnumTypeDescriptor desc:
                    desc.Directive(directive);
                    break;

                case EnumValueDescriptor desc:
                    desc.Directive(directive);
                    break;

                case InputFieldDescriptor desc:
                    desc.Directive(directive);
                    break;

                case InputObjectTypeDescriptor desc:
                    desc.Directive(directive);
                    break;

                case InterfaceFieldDescriptor desc:
                    desc.Directive(directive);
                    break;

                case InterfaceTypeDescriptor desc:
                    desc.Directive(directive);
                    break;

                case ObjectFieldDescriptor desc:
                    desc.Directive(directive);
                    break;

                case ObjectTypeDescriptor desc:
                    desc.Directive(directive);
                    break;

                case SchemaTypeDescriptor desc:
                    desc.Directive(directive);
                    break;

                case UnionTypeDescriptor desc:
                    desc.Directive(directive);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(descriptor));
            }
        }
    } 
}