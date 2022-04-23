using HotChocolate.Language;

namespace HotChocolate.Stitching.Types;

internal delegate ISchemaCoordinate2 CoordinateFactory(ISchemaCoordinate2? parent, ISyntaxNode node);
