using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace Socitas.ReviewerCop.Analyzers;

/// <summary>
/// CC0009 – DataClassification should be set as a table-level property, not repeated per field.
/// When DataClassification is set at the table level it applies to all fields and repeating it
/// on individual field declarations is redundant.
/// </summary>
[DiagnosticAnalyzer]
public sealed class DataClassificationOnTable : DiagnosticAnalyzer
{
    private const string DataClassificationProperty = "DataClassification";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.DataClassificationOnTable);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterSyntaxTreeAction(CheckTableDataClassification);

    private void CheckTableDataClassification(SyntaxTreeAnalysisContext ctx)
    {
        var root = ctx.Tree.GetRoot(ctx.CancellationToken);
        foreach (var tableNode in root.DescendantNodes().Where(n => IsSyntaxKind(n, "TableObject")))
        {
            string? tableLevelValue = null;
            var fieldLevelTokens = new List<SyntaxToken>();

            foreach (var token in tableNode.DescendantTokens())
            {
                if (!IsSyntaxKind(token, "IdentifierToken"))
                    continue;

                if (!string.Equals(token.ValueText, DataClassificationProperty, StringComparison.OrdinalIgnoreCase))
                    continue;

                // Must be a property assignment (identifier followed by '=')
                var next = token.GetNextToken();
                if (!IsSyntaxKind(next, "EqualsToken"))
                    continue;

                if (IsInsideFieldNode(token))
                    fieldLevelTokens.Add(token);
                else
                    tableLevelValue = next.GetNextToken().ValueText;
            }

            if (tableLevelValue is null || fieldLevelTokens.Count == 0)
                continue;

            foreach (var token in fieldLevelTokens)
            {
                // Re-derive the value: DataClassification = <value>
                var eqToken = token.GetNextToken();    // '='
                var fieldValue = eqToken.GetNextToken().ValueText;

                // Only report when the field-level value is identical to the table-level value.
                // A different value is an intentional override and is allowed.
                if (!string.Equals(fieldValue, tableLevelValue, StringComparison.OrdinalIgnoreCase))
                    continue;

                ctx.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.DataClassificationOnTable,
                    token.GetLocation()));
            }
        }
    }

    private static bool IsInsideFieldNode(SyntaxToken token)
    {
        var node = token.Parent;
        while (node is not null)
        {
            if (IsSyntaxKind(node, "Field"))
                return true;
            if (IsSyntaxKind(node, "TableObject"))
                return false;
            node = node.Parent;
        }
        return false;
    }

    private static bool IsSyntaxKind(SyntaxNode node, string expectedKindName)
    {
        return string.Equals(node.Kind.ToString(), expectedKindName, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSyntaxKind(SyntaxToken token, string expectedKindName)
    {
        return string.Equals(token.Kind.ToString(), expectedKindName, StringComparison.OrdinalIgnoreCase);
    }
}
