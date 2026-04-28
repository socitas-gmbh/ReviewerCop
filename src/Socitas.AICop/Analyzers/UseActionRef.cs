using System.Collections.Immutable;
using Socitas.ReviewerCop.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace Socitas.AICop.Analyzers;

[DiagnosticAnalyzer]
public sealed class UseActionRef : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.UseActionRef);

    public override void Initialize(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(
            CheckActions,
            EnumProvider.SyntaxKind.PageObject,
            EnumProvider.SyntaxKind.PageExtensionObject);
    }

    private static void CheckActions(SyntaxNodeAnalysisContext ctx)
    {
        foreach (var node in ctx.Node.DescendantNodes())
        {
            if (node.Kind != EnumProvider.SyntaxKind.PageAction)
                continue;

            if (!HasPromotedTrueProperty(node))
                continue;

            var nameToken = GetActionNameToken(node);
            if (nameToken.Kind == SyntaxKind.None)
                continue;

            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.UseActionRef,
                nameToken.GetLocation(),
                nameToken.ValueText ?? nameToken.ToString()));
        }
    }

    private static bool HasPromotedTrueProperty(SyntaxNode actionNode)
    {
        SyntaxToken prev2 = default;
        SyntaxToken prev1 = default;

        foreach (var token in actionNode.DescendantTokens())
        {
            if (prev1.Kind != SyntaxKind.None && prev2.Kind != SyntaxKind.None)
            {
                if (string.Equals(prev2.Kind.ToString(), "IdentifierToken", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(prev2.ValueText, "Promoted", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(prev1.Kind.ToString(), "EqualsToken", StringComparison.OrdinalIgnoreCase) &&
                    token.IsKind(EnumProvider.SyntaxKind.TrueKeyword))
                {
                    return true;
                }
            }

            prev2 = prev1;
            prev1 = token;
        }

        return false;
    }

    internal static SyntaxToken GetActionNameToken(SyntaxNode actionNode)
    {
        bool seenOpenParen = false;
        foreach (var token in actionNode.DescendantTokens())
        {
            if (token.Kind == EnumProvider.SyntaxKind.OpenParenToken)
            {
                seenOpenParen = true;
                continue;
            }

            if (seenOpenParen)
                return token;
        }

        return default;
    }
}
