using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Options;
using Mocha;

namespace Mocha.Sagas;

// public sealed class SagaRegistry(IEnumerable<Saga> sagas)
// {
//     private Dictionary<string, Saga>? _sagasByName;
//     private Dictionary<Type, Saga>? _sagasByType;

//     private JsonTypeInfo? _rootTypeInfo;

//     private bool _initialized;

//     public JsonTypeInfo RootTypeInfo =>
//         _rootTypeInfo ?? throw new InvalidOperationException("Sagas are not initialized.");

//     public void Initialize()
//     {
//         if (_initialized)
//         {
//             return;
//         }

//         var sagasByName = new Dictionary<string, Saga>();
//         var sagasByType = new Dictionary<Type, Saga>();

//         foreach (var saga in sagas)
//         {
//             try
//             {
//                 saga.Initialize(context);
//                 sagasByName.Add(saga.Name, saga);
//                 sagasByType.Add(saga.GetType(), saga);
//             }
//             catch (SagaInitializationException ex)
//             {
//                 context.Errors.Add(ex);
//             }
//         }

//         if (context.Errors.Any())
//         {
//             throw new AggregateException(context.Errors);
//         }

//         _sagasByName = sagasByName;
//         _sagasByType = sagasByType;

//         var stateTypes = sagas.Select(x => x.StateType).ToArray();
//         _rootTypeInfo = new PolymorphicTypeResolver(stateTypes, typeof(SagaStateBase)).GetTypeInfo(
//             typeof(SagaStateBase),
//             JsonSerializerOptions.Default
//         );

//         _initialized = true;
//     }

//     public Saga GetSaga(string name)
//     {
//         if (_sagasByName is null)
//         {
//             throw new InvalidOperationException("Sagas are not initialized.");
//         }

//         if (!_sagasByName.TryGetValue(name, out var saga))
//         {
//             throw new InvalidOperationException($"Saga '{name}' not found.");
//         }

//         return saga;
//     }

//     public Saga GetSaga(Type type)
//     {
//         if (_sagasByType is null)
//         {
//             throw new InvalidOperationException("Sagas are not initialized.");
//         }

//         if (!_sagasByType.TryGetValue(type, out var saga))
//         {
//             throw new InvalidOperationException($"Saga '{type.Name}' not found.");
//         }

//         return saga;
//     }
// }
