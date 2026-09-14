using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// One possible type's contribution to a <see cref="CollectedFieldGroup"/>.
/// </summary>
/// <param name="ParentType">
/// The possible type the field is selected on.
/// </param>
/// <param name="Field">
/// The field definition <see cref="ParentType"/> resolves the selection to.
/// </param>
[Experimental(CostExperiments.AnalysisAlgebra)]
public readonly record struct CollectedFieldGroupMember(IComplexTypeDefinition ParentType, IOutputFieldDefinition Field);
