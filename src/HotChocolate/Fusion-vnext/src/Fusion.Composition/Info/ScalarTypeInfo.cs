using HotChocolate.Types;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Info;

internal record ScalarTypeInfo(IScalarTypeDefinition Scalar, MutableSchemaDefinition Schema);
