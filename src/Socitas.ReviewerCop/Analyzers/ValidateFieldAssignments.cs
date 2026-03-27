using System.Collections.Immutable;
using Socitas.ReviewerCop.Common.Extensions;
using Socitas.ReviewerCop.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Semantics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace Socitas.ReviewerCop.Analyzers;

/// <summary>
/// CC0003 – Field assignments on non-temporary records should use Validate().
/// </summary>
[DiagnosticAnalyzer]
public sealed class ValidateFieldAssignments : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.ValidateFieldAssignments);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterOperationAction(
            AnalyzeAssignment,
            EnumProvider.OperationKind.AssignmentStatement);

    private void AnalyzeAssignment(OperationAnalysisContext ctx)
    {
        if (ctx.IsObsolete())
            return;

        if (ctx.Operation is not IAssignmentStatement assignment)
            return;

        // LHS must be a field access (e.g. Rec.MyField)
        if (assignment.Target is not IFieldAccess fieldAccess)
            return;

        var fieldSymbol = fieldAccess.FieldSymbol;

        // The receiver of the field access must be a record type
        if (fieldAccess.Instance?.Type is not IRecordTypeSymbol recordType)
            return;

        // Skip temporary records – Validate is not needed there
        if (recordType.Temporary)
            return;

        // Skip system fields (ID < 0 or >= 2000000000)
        if (fieldSymbol.Id < 0 || fieldSymbol.Id >= 2000000000)
            return;

        var location = fieldAccess.Syntax?.GetIdentifierNameSyntax()?.GetLocation()
                       ?? fieldAccess.Syntax?.GetLocation();

        if (location is null)
            return;

        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.ValidateFieldAssignments,
            location,
            fieldSymbol.Name));
    }
}
