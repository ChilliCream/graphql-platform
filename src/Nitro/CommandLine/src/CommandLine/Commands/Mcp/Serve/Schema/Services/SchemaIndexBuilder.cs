using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Models;
using HotChocolate.Language;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Services;

internal static class SchemaIndexBuilder
{
    private static readonly HashSet<string> _builtInScalars = new(StringComparer.Ordinal)
    {
        "String",
        "Int",
        "Float",
        "Boolean",
        "ID"
    };

    public static SchemaIndex Build(string sdl)
    {
        var document = Utf8GraphQLParser.Parse(sdl);
        return Build(document);
    }

    public static SchemaIndex Build(DocumentNode document)
    {
        var members = new Dictionary<string, SchemaIndexEntry>(StringComparer.Ordinal);
        var typeToChildren = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var nameIndex = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var reverseEdges = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var rootTypes = DetectRootTypes(document);

        foreach (var definition in document.Definitions)
        {
            switch (definition)
            {
                case ObjectTypeDefinitionNode obj:
                    IndexObjectType(
                        obj.Name.Value,
                        obj.Description?.Value,
                        obj.Fields,
                        obj.Interfaces,
                        obj.Directives,
                        rootTypes,
                        members,
                        typeToChildren,
                        nameIndex,
                        reverseEdges);
                    break;

                case ObjectTypeExtensionNode objExt:
                    IndexObjectType(
                        objExt.Name.Value,
                        null,
                        objExt.Fields,
                        objExt.Interfaces,
                        objExt.Directives,
                        rootTypes,
                        members,
                        typeToChildren,
                        nameIndex,
                        reverseEdges);
                    break;

                case InterfaceTypeDefinitionNode iface:
                    IndexInterfaceType(
                        iface.Name.Value,
                        iface.Description?.Value,
                        iface.Fields,
                        iface.Directives,
                        members,
                        typeToChildren,
                        nameIndex,
                        reverseEdges);
                    break;

                case InterfaceTypeExtensionNode ifaceExt:
                    IndexInterfaceType(
                        ifaceExt.Name.Value,
                        null,
                        ifaceExt.Fields,
                        ifaceExt.Directives,
                        members,
                        typeToChildren,
                        nameIndex,
                        reverseEdges);
                    break;

                case InputObjectTypeDefinitionNode input:
                    IndexInputObjectType(
                        input.Name.Value,
                        input.Description?.Value,
                        input.Fields,
                        input.Directives,
                        members,
                        typeToChildren,
                        nameIndex);
                    break;

                case InputObjectTypeExtensionNode inputExt:
                    IndexInputObjectType(
                        inputExt.Name.Value,
                        null,
                        inputExt.Fields,
                        inputExt.Directives,
                        members,
                        typeToChildren,
                        nameIndex);
                    break;

                case EnumTypeDefinitionNode enumDef:
                    IndexEnumType(
                        enumDef.Name.Value,
                        enumDef.Description?.Value,
                        enumDef.Values,
                        enumDef.Directives,
                        members,
                        typeToChildren,
                        nameIndex);
                    break;

                case EnumTypeExtensionNode enumExt:
                    IndexEnumType(
                        enumExt.Name.Value,
                        null,
                        enumExt.Values,
                        enumExt.Directives,
                        members,
                        typeToChildren,
                        nameIndex);
                    break;

                case UnionTypeDefinitionNode union:
                    IndexUnionType(
                        union.Name.Value,
                        union.Description?.Value,
                        union.Types,
                        union.Directives,
                        members,
                        nameIndex);
                    break;

                case UnionTypeExtensionNode unionExt:
                    IndexUnionType(unionExt.Name.Value, null, unionExt.Types, unionExt.Directives, members, nameIndex);
                    break;

                case ScalarTypeDefinitionNode scalar:
                    IndexScalarType(
                        scalar.Name.Value,
                        scalar.Description?.Value,
                        scalar.Directives,
                        members,
                        nameIndex);
                    break;

                case ScalarTypeExtensionNode scalarExt:
                    IndexScalarType(scalarExt.Name.Value, null, scalarExt.Directives, members, nameIndex);
                    break;

                case DirectiveDefinitionNode directive:
                    IndexDirectiveDefinition(directive, members, nameIndex);
                    break;
            }
        }

        return new SchemaIndex(members, typeToChildren, nameIndex, reverseEdges, rootTypes);
    }

    private static HashSet<string> DetectRootTypes(DocumentNode document)
    {
        foreach (var def in document.Definitions)
        {
            if (def is SchemaDefinitionNode schemaDef)
            {
                return schemaDef.OperationTypes.Select(ot => ot.Type.Name.Value).ToHashSet(StringComparer.Ordinal);
            }
        }

        var roots = new HashSet<string>(StringComparer.Ordinal);
        foreach (var def in document.Definitions)
        {
            if (def is ObjectTypeDefinitionNode obj
                && obj.Name.Value is "Query" or "Mutation" or "Subscription")
            {
                roots.Add(obj.Name.Value);
            }
        }

        return roots;
    }

    private static void IndexObjectType(
        string typeName,
        string? description,
        IReadOnlyList<FieldDefinitionNode> fields,
        IReadOnlyList<NamedTypeNode> interfaces,
        IReadOnlyList<DirectiveNode> directives,
        HashSet<string> rootTypes,
        Dictionary<string, SchemaIndexEntry> members,
        Dictionary<string, List<string>> typeToChildren,
        Dictionary<string, List<string>> nameIndex,
        Dictionary<string, List<string>> reverseEdges)
    {
        if (members.TryGetValue(typeName, out var existing))
        {
            var mergedInterfaces = MergeInterfaces(existing.Interfaces, interfaces);
            var mergedDirectives = MergeDirectives(existing.Directives, directives);

            members[typeName] = new SchemaIndexEntry
            {
                Coordinate = typeName,
                Kind = SchemaIndexMemberKind.Type,
                Name = typeName,
                Description = existing.Description ?? description,
                Directives = mergedDirectives,
                Interfaces = mergedInterfaces
            };
        }
        else
        {
            var entry = new SchemaIndexEntry
            {
                Coordinate = typeName,
                Kind = SchemaIndexMemberKind.Type,
                Name = typeName,
                Description = description,
                Directives = ExtractDirectives(directives),
                Interfaces = interfaces.Count > 0 ? interfaces.Select(i => i.Name.Value).ToArray() : null
            };

            members[typeName] = entry;
            AddToNameIndex(nameIndex, typeName, typeName);
        }

        foreach (var field in fields)
        {
            IndexField(field, typeName, members, typeToChildren, nameIndex, reverseEdges);
        }
    }

    private static void IndexInterfaceType(
        string typeName,
        string? description,
        IReadOnlyList<FieldDefinitionNode> fields,
        IReadOnlyList<DirectiveNode> directives,
        Dictionary<string, SchemaIndexEntry> members,
        Dictionary<string, List<string>> typeToChildren,
        Dictionary<string, List<string>> nameIndex,
        Dictionary<string, List<string>> reverseEdges)
    {
        if (members.TryGetValue(typeName, out var existing))
        {
            members[typeName] = new SchemaIndexEntry
            {
                Coordinate = typeName,
                Kind = SchemaIndexMemberKind.Interface,
                Name = typeName,
                Description = existing.Description ?? description,
                Directives = MergeDirectives(existing.Directives, directives)
            };
        }
        else
        {
            members[typeName] = new SchemaIndexEntry
            {
                Coordinate = typeName,
                Kind = SchemaIndexMemberKind.Interface,
                Name = typeName,
                Description = description,
                Directives = ExtractDirectives(directives)
            };

            AddToNameIndex(nameIndex, typeName, typeName);
        }

        foreach (var field in fields)
        {
            IndexField(field, typeName, members, typeToChildren, nameIndex, reverseEdges);
        }
    }

    private static void IndexField(
        FieldDefinitionNode field,
        string parentTypeName,
        Dictionary<string, SchemaIndexEntry> members,
        Dictionary<string, List<string>> typeToChildren,
        Dictionary<string, List<string>> nameIndex,
        Dictionary<string, List<string>> reverseEdges)
    {
        var coordinate = parentTypeName + "." + field.Name.Value;
        var typeName = TypeNamePrinter.Print(field.Type);
        var namedReturnType = GetNamedType(field.Type);
        var isDeprecated = IsDeprecated(field.Directives, out var deprecationReason);

        var entry = new SchemaIndexEntry
        {
            Coordinate = coordinate,
            Kind = SchemaIndexMemberKind.Field,
            Name = field.Name.Value,
            ParentTypeName = parentTypeName,
            TypeName = typeName,
            Description = field.Description?.Value,
            IsDeprecated = isDeprecated,
            DeprecationReason = deprecationReason,
            Arguments = ExtractArguments(field.Arguments),
            Directives = ExtractDirectives(field.Directives)
        };

        members[coordinate] = entry;
        AddToNameIndex(nameIndex, field.Name.Value, coordinate);
        AddToChildren(typeToChildren, parentTypeName, coordinate);

        if (namedReturnType is not null && !_builtInScalars.Contains(namedReturnType))
        {
            if (!reverseEdges.TryGetValue(namedReturnType, out var edges))
            {
                edges = [];
                reverseEdges[namedReturnType] = edges;
            }

            edges.Add(coordinate);
        }

        foreach (var arg in field.Arguments)
        {
            IndexArgument(arg, coordinate, members, nameIndex);
        }
    }

    private static void IndexArgument(
        InputValueDefinitionNode arg,
        string fieldCoordinate,
        Dictionary<string, SchemaIndexEntry> members,
        Dictionary<string, List<string>> nameIndex)
    {
        var coordinate = fieldCoordinate + "(" + arg.Name.Value + ":)";
        var isDeprecated = IsDeprecated(arg.Directives, out var deprecationReason);

        var entry = new SchemaIndexEntry
        {
            Coordinate = coordinate,
            Kind = SchemaIndexMemberKind.Argument,
            Name = arg.Name.Value,
            ParentTypeName = fieldCoordinate,
            TypeName = TypeNamePrinter.Print(arg.Type),
            Description = arg.Description?.Value,
            IsDeprecated = isDeprecated,
            DeprecationReason = deprecationReason,
            DefaultValue = arg.DefaultValue?.ToString()
        };

        members[coordinate] = entry;
        AddToNameIndex(nameIndex, arg.Name.Value, coordinate);
    }

    private static void IndexEnumType(
        string typeName,
        string? description,
        IReadOnlyList<EnumValueDefinitionNode> values,
        IReadOnlyList<DirectiveNode> directives,
        Dictionary<string, SchemaIndexEntry> members,
        Dictionary<string, List<string>> typeToChildren,
        Dictionary<string, List<string>> nameIndex)
    {
        if (members.TryGetValue(typeName, out var existing))
        {
            var mergedEnumValues = MergeEnumValues(existing.EnumValues, values);
            members[typeName] = new SchemaIndexEntry
            {
                Coordinate = typeName,
                Kind = SchemaIndexMemberKind.Enum,
                Name = typeName,
                Description = existing.Description ?? description,
                Directives = MergeDirectives(existing.Directives, directives),
                EnumValues = mergedEnumValues
            };
        }
        else
        {
            var entry = new SchemaIndexEntry
            {
                Coordinate = typeName,
                Kind = SchemaIndexMemberKind.Enum,
                Name = typeName,
                Description = description,
                Directives = ExtractDirectives(directives),
                EnumValues = values
                    .Select(v =>
                    {
                        var isDeprecated = IsDeprecated(v.Directives, out var reason);
                        return new EnumValueEntry
                        {
                            Name = v.Name.Value,
                            Description = v.Description?.Value,
                            IsDeprecated = isDeprecated,
                            DeprecationReason = reason
                        };
                    })
                    .ToArray()
            };

            members[typeName] = entry;
            AddToNameIndex(nameIndex, typeName, typeName);
        }

        foreach (var value in values)
        {
            var valueCoordinate = typeName + "." + value.Name.Value;

            if (!members.ContainsKey(valueCoordinate))
            {
                var isDeprecated = IsDeprecated(value.Directives, out var deprecationReason);

                members[valueCoordinate] = new SchemaIndexEntry
                {
                    Coordinate = valueCoordinate,
                    Kind = SchemaIndexMemberKind.EnumValue,
                    Name = value.Name.Value,
                    ParentTypeName = typeName,
                    Description = value.Description?.Value,
                    IsDeprecated = isDeprecated,
                    DeprecationReason = deprecationReason
                };

                AddToNameIndex(nameIndex, value.Name.Value, valueCoordinate);
                AddToChildren(typeToChildren, typeName, valueCoordinate);
            }
        }
    }

    private static void IndexInputObjectType(
        string typeName,
        string? description,
        IReadOnlyList<InputValueDefinitionNode> fields,
        IReadOnlyList<DirectiveNode> directives,
        Dictionary<string, SchemaIndexEntry> members,
        Dictionary<string, List<string>> typeToChildren,
        Dictionary<string, List<string>> nameIndex)
    {
        if (members.TryGetValue(typeName, out var existing))
        {
            members[typeName] = new SchemaIndexEntry
            {
                Coordinate = typeName,
                Kind = SchemaIndexMemberKind.InputType,
                Name = typeName,
                Description = existing.Description ?? description,
                Directives = MergeDirectives(existing.Directives, directives)
            };
        }
        else
        {
            members[typeName] = new SchemaIndexEntry
            {
                Coordinate = typeName,
                Kind = SchemaIndexMemberKind.InputType,
                Name = typeName,
                Description = description,
                Directives = ExtractDirectives(directives)
            };

            AddToNameIndex(nameIndex, typeName, typeName);
        }

        foreach (var field in fields)
        {
            var coordinate = typeName + "." + field.Name.Value;

            if (!members.ContainsKey(coordinate))
            {
                var isDeprecated = IsDeprecated(field.Directives, out var deprecationReason);

                members[coordinate] = new SchemaIndexEntry
                {
                    Coordinate = coordinate,
                    Kind = SchemaIndexMemberKind.InputField,
                    Name = field.Name.Value,
                    ParentTypeName = typeName,
                    TypeName = TypeNamePrinter.Print(field.Type),
                    Description = field.Description?.Value,
                    IsDeprecated = isDeprecated,
                    DeprecationReason = deprecationReason,
                    DefaultValue = field.DefaultValue?.ToString()
                };

                AddToNameIndex(nameIndex, field.Name.Value, coordinate);
                AddToChildren(typeToChildren, typeName, coordinate);
            }
        }
    }

    private static void IndexUnionType(
        string typeName,
        string? description,
        IReadOnlyList<NamedTypeNode> types,
        IReadOnlyList<DirectiveNode> directives,
        Dictionary<string, SchemaIndexEntry> members,
        Dictionary<string, List<string>> nameIndex)
    {
        if (members.TryGetValue(typeName, out var existing))
        {
            var mergedTypes = MergePossibleTypes(existing.PossibleTypes, types);
            members[typeName] = new SchemaIndexEntry
            {
                Coordinate = typeName,
                Kind = SchemaIndexMemberKind.Union,
                Name = typeName,
                Description = existing.Description ?? description,
                Directives = MergeDirectives(existing.Directives, directives),
                PossibleTypes = mergedTypes
            };
        }
        else
        {
            members[typeName] = new SchemaIndexEntry
            {
                Coordinate = typeName,
                Kind = SchemaIndexMemberKind.Union,
                Name = typeName,
                Description = description,
                Directives = ExtractDirectives(directives),
                PossibleTypes = types.Select(t => t.Name.Value).ToArray()
            };

            AddToNameIndex(nameIndex, typeName, typeName);
        }
    }

    private static void IndexScalarType(
        string typeName,
        string? description,
        IReadOnlyList<DirectiveNode> directives,
        Dictionary<string, SchemaIndexEntry> members,
        Dictionary<string, List<string>> nameIndex)
    {
        if (members.TryGetValue(typeName, out var existing))
        {
            members[typeName] = new SchemaIndexEntry
            {
                Coordinate = typeName,
                Kind = SchemaIndexMemberKind.Scalar,
                Name = typeName,
                Description = existing.Description ?? description,
                Directives = MergeDirectives(existing.Directives, directives)
            };
        }
        else
        {
            members[typeName] = new SchemaIndexEntry
            {
                Coordinate = typeName,
                Kind = SchemaIndexMemberKind.Scalar,
                Name = typeName,
                Description = description,
                Directives = ExtractDirectives(directives)
            };

            AddToNameIndex(nameIndex, typeName, typeName);
        }
    }

    private static void IndexDirectiveDefinition(
        DirectiveDefinitionNode directive,
        Dictionary<string, SchemaIndexEntry> members,
        Dictionary<string, List<string>> nameIndex)
    {
        var name = directive.Name.Value;
        var coordinate = "@" + name;

        members[coordinate] = new SchemaIndexEntry
        {
            Coordinate = coordinate,
            Kind = SchemaIndexMemberKind.Directive,
            Name = name,
            Description = directive.Description?.Value,
            Arguments =
                directive.Arguments.Count > 0
                    ? directive
                        .Arguments.Select(a =>
                        {
                            var isDeprecated = IsDeprecated(a.Directives, out var reason);
                            return new ArgumentEntry
                            {
                                Name = a.Name.Value,
                                TypeName = TypeNamePrinter.Print(a.Type),
                                Description = a.Description?.Value,
                                DefaultValue = a.DefaultValue?.ToString(),
                                IsDeprecated = isDeprecated,
                                DeprecationReason = reason
                            };
                        })
                        .ToArray()
                    : null
        };

        AddToNameIndex(nameIndex, name, coordinate);
    }

    private static bool IsDeprecated(IReadOnlyList<DirectiveNode> directives, out string? reason)
    {
        foreach (var directive in directives)
        {
            if (directive.Name.Value == "deprecated")
            {
                reason = directive.Arguments.FirstOrDefault(a => a.Name.Value == "reason")?.Value is StringValueNode sv
                    ? sv.Value
                    : null;
                return true;
            }
        }

        reason = null;
        return false;
    }

    private static string? GetNamedType(ITypeNode type)
        => type switch
        {
            NamedTypeNode named => named.Name.Value,
            ListTypeNode list => GetNamedType(list.Type),
            NonNullTypeNode nonNull => GetNamedType(nonNull.Type),
            _ => null
        };

    private static IReadOnlyList<ArgumentEntry>? ExtractArguments(IReadOnlyList<InputValueDefinitionNode> args)
    {
        if (args.Count == 0)
        {
            return null;
        }

        return args.Select(a =>
            {
                var isDeprecated = IsDeprecated(a.Directives, out var reason);
                return new ArgumentEntry
                {
                    Name = a.Name.Value,
                    TypeName = TypeNamePrinter.Print(a.Type),
                    Description = a.Description?.Value,
                    DefaultValue = a.DefaultValue?.ToString(),
                    IsDeprecated = isDeprecated,
                    DeprecationReason = reason
                };
            })
            .ToArray();
    }

    private static IReadOnlyList<DirectiveEntry>? ExtractDirectives(IReadOnlyList<DirectiveNode> directives)
    {
        if (directives.Count == 0)
        {
            return null;
        }

        return directives
            .Select(d => new DirectiveEntry
            {
                Name = d.Name.Value,
                Arguments =
                    d.Arguments.Count > 0
                        ? d.Arguments.ToDictionary(a => a.Name.Value, a => a.Value.ToString() ?? string.Empty)
                        : null
            })
            .ToArray();
    }

    private static void AddToNameIndex(Dictionary<string, List<string>> nameIndex, string name, string coordinate)
    {
        if (!nameIndex.TryGetValue(name, out var list))
        {
            list = [];
            nameIndex[name] = list;
        }

        list.Add(coordinate);
    }

    private static void AddToChildren(Dictionary<string, List<string>> typeToChildren, string parent, string child)
    {
        if (!typeToChildren.TryGetValue(parent, out var list))
        {
            list = [];
            typeToChildren[parent] = list;
        }

        list.Add(child);
    }

    private static IReadOnlyList<string>? MergeInterfaces(
        IReadOnlyList<string>? existing,
        IReadOnlyList<NamedTypeNode> additional)
    {
        if (additional.Count == 0)
        {
            return existing;
        }

        var set = new HashSet<string>(existing ?? Array.Empty<string>(), StringComparer.Ordinal);

        foreach (var iface in additional)
        {
            set.Add(iface.Name.Value);
        }

        return set.Count > 0 ? set.ToArray() : null;
    }

    private static IReadOnlyList<DirectiveEntry>? MergeDirectives(
        IReadOnlyList<DirectiveEntry>? existing,
        IReadOnlyList<DirectiveNode> additional)
    {
        var extracted = ExtractDirectives(additional);
        if (existing is null)
        {
            return extracted;
        }

        if (extracted is null)
        {
            return existing;
        }

        var merged = new List<DirectiveEntry>(existing);
        merged.AddRange(extracted);
        return merged;
    }

    private static IReadOnlyList<string>? MergePossibleTypes(
        IReadOnlyList<string>? existing,
        IReadOnlyList<NamedTypeNode> additional)
    {
        if (additional.Count == 0)
        {
            return existing;
        }

        var set = new HashSet<string>(existing ?? Array.Empty<string>(), StringComparer.Ordinal);

        foreach (var type in additional)
        {
            set.Add(type.Name.Value);
        }

        return set.Count > 0 ? set.ToArray() : null;
    }

    private static IReadOnlyList<EnumValueEntry>? MergeEnumValues(
        IReadOnlyList<EnumValueEntry>? existing,
        IReadOnlyList<EnumValueDefinitionNode> additional)
    {
        if (additional.Count == 0)
        {
            return existing;
        }

        var existingNames = new HashSet<string>(StringComparer.Ordinal);
        var merged = new List<EnumValueEntry>();

        if (existing is not null)
        {
            foreach (var entry in existing)
            {
                existingNames.Add(entry.Name);
                merged.Add(entry);
            }
        }

        foreach (var v in additional)
        {
            if (existingNames.Add(v.Name.Value))
            {
                var isDeprecated = IsDeprecated(v.Directives, out var reason);
                merged.Add(
                    new EnumValueEntry
                    {
                        Name = v.Name.Value,
                        Description = v.Description?.Value,
                        IsDeprecated = isDeprecated,
                        DeprecationReason = reason
                    });
            }
        }

        return merged.Count > 0 ? merged : null;
    }
}
