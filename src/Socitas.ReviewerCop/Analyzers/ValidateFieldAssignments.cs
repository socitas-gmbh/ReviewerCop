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

        // A comment adjacent to the assignment (inline or immediately preceding line)
        // serves as an explanation for why Validate is intentionally omitted.
        if (HasAdjacentComment(assignment.Syntax))
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

    private static bool HasAdjacentComment(SyntaxNode? syntax)
    {
        if (syntax is null)
            return false;

        // Inline comment on the same line as the assignment
        foreach (var trivia in syntax.GetLastToken().TrailingTrivia)
        {
            if (trivia.IsKind(EnumProvider.SyntaxKind.LineCommentTrivia))
                return true;
        }

        // Comment on the immediately preceding line (no blank line between)
        return IsPrecededByAdjacentComment(syntax.GetFirstToken().LeadingTrivia);
    }

    private static bool IsPrecededByAdjacentComment(SyntaxTriviaList leadingTrivia)
    {
        var commentKind = EnumProvider.SyntaxKind.LineCommentTrivia;
        var endOfLineKind = EnumProvider.SyntaxKind.EndOfLineTrivia;
        int eolCount = 0;
        for (int i = leadingTrivia.Count - 1; i >= 0; i--)
        {
            var trivia = leadingTrivia[i];
            if (trivia.IsKind(commentKind))
                return eolCount <= 1;
            if (trivia.IsKind(endOfLineKind))
                eolCount++;
        }
        return false;
    }
}
