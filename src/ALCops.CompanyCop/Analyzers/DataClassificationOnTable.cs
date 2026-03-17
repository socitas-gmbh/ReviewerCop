using System.Collections.Generic;
using System.Collections.Immutable;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.CompanyCop.Analyzers;

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
        context.RegisterSyntaxNodeAction(
            CheckTableDataClassification,
            EnumProvider.SyntaxKind.TableObject);

    private void CheckTableDataClassification(SyntaxNodeAnalysisContext ctx)
    {
        var tableNode = ctx.Node;
        bool hasTableLevelDataClassification = false;
        var fieldLevelTokens = new List<SyntaxToken>();

        foreach (var token in tableNode.DescendantTokens())
        {
            if (token.Kind != SyntaxKind.IdentifierToken)
                continue;

            if (!string.Equals(token.ValueText, DataClassificationProperty, StringComparison.OrdinalIgnoreCase))
                continue;

            // Must be a property assignment (identifier followed by '=')
            var next = token.GetNextToken();
            if (next.Kind != SyntaxKind.EqualsToken)
                continue;

            if (IsInsideFieldNode(token))
                fieldLevelTokens.Add(token);
            else
                hasTableLevelDataClassification = true;
        }

        if (!hasTableLevelDataClassification || fieldLevelTokens.Count == 0)
            return;

        foreach (var token in fieldLevelTokens)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.DataClassificationOnTable,
                token.GetLocation()));
        }
    }

    private static bool IsInsideFieldNode(SyntaxToken token)
    {
        var node = token.Parent;
        while (node is not null)
        {
            if (node.Kind == SyntaxKind.Field)
                return true;
            if (node.Kind == SyntaxKind.TableObject)
                return false;
            node = node.Parent;
        }
        return false;
    }
}
