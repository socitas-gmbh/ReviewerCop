using System.Collections.Immutable;
using Socitas.ReviewerCop.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace Socitas.ReviewerCop.Analyzers;

/// <summary>
/// CC0012 – Opening brace must be on the same line as the declaration.
/// Reports when a page field, report column, or report dataitem has the opening
/// brace '{' on a new line instead of the same line as the closing parenthesis ')'.
/// </summary>
[DiagnosticAnalyzer]
public sealed class OpenBraceOnSameLine : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.OpenBraceOnSameLine);

    public override void Initialize(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(CheckNodes,
            EnumProvider.SyntaxKind.PageObject,
            EnumProvider.SyntaxKind.PageExtensionObject,
            EnumProvider.SyntaxKind.ReportObject,
            EnumProvider.SyntaxKind.ReportExtensionObject);
    }

    private static void CheckNodes(SyntaxNodeAnalysisContext ctx)
    {
        foreach (var node in ctx.Node.DescendantNodes())
        {
            if (!IsTargetKind(node.Kind))
                continue;

            CheckOpenBracePlacement(ctx, node);
        }
    }

    private static bool IsTargetKind(SyntaxKind kind) =>
        kind == EnumProvider.SyntaxKind.PageField ||
        kind == EnumProvider.SyntaxKind.PageGroup ||
        kind == EnumProvider.SyntaxKind.PageAction ||
        kind == EnumProvider.SyntaxKind.PageActionGroup ||
        kind == EnumProvider.SyntaxKind.PageActionArea ||
        kind == EnumProvider.SyntaxKind.PageArea ||
        kind == EnumProvider.SyntaxKind.PagePart ||
        kind == EnumProvider.SyntaxKind.ReportColumn ||
        kind == EnumProvider.SyntaxKind.ReportDataItem;

    private static bool HasContentBetweenBraces(SyntaxNode node, SyntaxToken openBrace)
    {
        var foundOpenBrace = false;
        foreach (var token in node.DescendantTokens())
        {
            if (token == openBrace)
            {
                foundOpenBrace = true;
                continue;
            }

            if (!foundOpenBrace)
                continue;

            // The first meaningful token after the open brace:
            // if it's the closing brace, the block is empty
            return token.Kind != EnumProvider.SyntaxKind.CloseBraceToken;
        }

        return false;
    }

    private static void CheckOpenBracePlacement(SyntaxNodeAnalysisContext ctx, SyntaxNode node)
    {
        SyntaxToken closeParen = default;
        SyntaxToken openBrace = default;

        foreach (var token in node.DescendantTokens())
        {
            if (token.Kind == EnumProvider.SyntaxKind.CloseParenToken)
                closeParen = token;

            if (token.Kind == EnumProvider.SyntaxKind.OpenBraceToken)
            {
                openBrace = token;
                break;
            }
        }

        // Both tokens must exist and the close paren must come before the open brace
        if (closeParen.Kind == SyntaxKind.None || openBrace.Kind == SyntaxKind.None)
            return;

        var closeParenLine = closeParen.GetLocation().GetLineSpan().StartLinePosition.Line;
        var openBraceLine = openBrace.GetLocation().GetLineSpan().StartLinePosition.Line;

        if (openBraceLine != closeParenLine && !HasContentBetweenBraces(node, openBrace))
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.OpenBraceOnSameLine,
                openBrace.GetLocation()));
        }
    }
}
