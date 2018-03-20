using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Prometheus.Types
{

    /*

    name: string,
      interfaces?: Thunk<?Array<GraphQLInterfaceType>>,
      fields: Thunk<GraphQLFieldConfigMap<TSource, TContext>>,
      isTypeOf?: ?GraphQLIsTypeOfFn<TSource, TContext>,
      description?: ?string,
      astNode?: ?ObjectTypeDefinitionNode,
      extensionASTNodes?: ?$ReadOnlyArray<ObjectTypeExtensionNode>,
     */

    public class ObjectType
        : IOutputType
    {
        private readonly ObjectTypeConfig _config;
        private IReadOnlyDictionary<string, InterfaceType> _interfaces;
        private IReadOnlyDictionary<string, Field> _fields;

        public ObjectType(ObjectTypeConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            _config = config;
            Name = config.Name;
            Description = config.Description;
        }

        public string Name { get; }
        
        public string Description { get; }

        public IReadOnlyDictionary<string, InterfaceType> Interfaces
        {
            get
            {
                if (_interfaces == null)
                {
                    var interfaces = _config.Interfaces();
                    _interfaces = (interfaces == null)
                        ? new Dictionary<string, InterfaceType>()
                        : interfaces.ToDictionary(t => t.Name);
                }
                return _interfaces;
            }
        }

        public IReadOnlyDictionary<string, Field> Fields
        {
            get
            {
                if()
            }
        }
    }

    /*

        name: string,
          description?: ?string,
        
          interfaces?: Thunk<?Array<GraphQLInterfaceType>>,
          fields: Thunk<GraphQLFieldConfigMap<TSource, TContext>>,
          isTypeOf?: ?GraphQLIsTypeOfFn<TSource, TContext>,
          astNode?: ?ObjectTypeDefinitionNode,
          extensionASTNodes?: ?$ReadOnlyArray<ObjectTypeExtensionNode>,
         */

    public class ObjectTypeConfig
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public Func<IEnumerable<InterfaceType>> Interfaces { get; set; }

        public Func<IEnumerable<Field>> Fields { get; set; }
    }

    public class InterfaceType
    {
        public string Name { get; internal set; }
    }


    public interface IFieldCollection
        : IReadOnlyCollection<Field>
    {
        Field GetField(stirng name);
        bool ContainsField(string name);
    }



}