using System.Collections.Immutable;
using System.Text.RegularExpressions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.CompanyCop.Analyzers;

/// <summary>
/// CC0010 – Labels that contain placeholders (%1, %2, #1, etc.) must declare a Comment property
/// that describes what each placeholder represents.
/// </summary>
[DiagnosticAnalyzer]
public sealed class LabelCommentForPlaceholders : DiagnosticAnalyzer
{
    private static readonly Regex PlaceholderPattern =
        new(@"[%#]\d+", RegexOptions.Compiled);

    private const string CommentProperty = "Comment";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.LabelCommentForPlaceholders);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(
            CheckLabelForPlaceholders,
            EnumProvider.SyntaxKind.LabelDataType);

    private void CheckLabelForPlaceholders(SyntaxNodeAnalysisContext ctx)
    {
        var labelNode = ctx.Node;

        // Find the string literal token that holds the label text
        SyntaxToken? stringToken = null;
        foreach (var token in labelNode.DescendantTokens())
        {
            if (token.Kind == SyntaxKind.StringLiteralToken)
            {
                stringToken = token;
                break;
            }
        }

        if (stringToken is null)
            return;

        var labelText = stringToken.Value.ValueText;
        if (labelText is null || !PlaceholderPattern.IsMatch(labelText))
            return;

        if (HasCommentProperty(labelNode))
            return;

        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.LabelCommentForPlaceholders,
            stringToken.Value.GetLocation()));
    }

    private static bool HasCommentProperty(SyntaxNode labelNode)
    {
        foreach (var token in labelNode.DescendantTokens())
        {
            if (token.Kind != SyntaxKind.IdentifierToken)
                continue;

            if (!string.Equals(token.ValueText, CommentProperty, StringComparison.OrdinalIgnoreCase))
                continue;

            var next = token.GetNextToken();
            if (next.Kind == SyntaxKind.EqualsToken)
                return true;
        }
        return false;
    }
}
