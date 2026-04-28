using System.Collections.Immutable;
using Socitas.ReviewerCop.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace Socitas.AICop.Analyzers;

/// <summary>
/// AI0008 / AI0009 – Members declared in extension objects must end with 'SOC'.
/// Non-local procedures, actions, and fields require the suffix; local procedures must not have it.
/// </summary>
[DiagnosticAnalyzer]
public sealed class ExtensionObjectSocSuffix : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.ExtensionMemberMissingSocSuffix,
            DiagnosticDescriptors.LocalProcedureHasSocSuffix);

    public override void Initialize(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(
            CheckExtensionObject,
            EnumProvider.SyntaxKind.TableExtensionObject,
            EnumProvider.SyntaxKind.PageExtensionObject,
            EnumProvider.SyntaxKind.ReportExtensionObject,
            EnumProvider.SyntaxKind.EnumExtensionType);
    }

    private static void CheckExtensionObject(SyntaxNodeAnalysisContext ctx)
    {
        foreach (var node in ctx.Node.DescendantNodes())
        {
            if (node is MethodDeclarationSyntax method)
                CheckMethod(ctx, method);
            else if (node.Kind == EnumProvider.SyntaxKind.PageAction)
                CheckAction(ctx, node);
            else if (node.Kind == EnumProvider.SyntaxKind.Field)
                CheckField(ctx, node);
        }
    }

    private static void CheckMethod(SyntaxNodeAnalysisContext ctx, MethodDeclarationSyntax method)
    {
        var nameToken = method.Name.Identifier;
        if (nameToken.Kind == SyntaxKind.None)
            return;

        var name = nameToken.ValueText ?? string.Empty;
        bool isLocal = IsLocalMethod(method);
        bool hasSoc = name.EndsWith("SOC", StringComparison.Ordinal);

        if (!isLocal && !hasSoc)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ExtensionMemberMissingSocSuffix,
                nameToken.GetLocation(),
                name));
        }
        else if (isLocal && hasSoc)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.LocalProcedureHasSocSuffix,
                nameToken.GetLocation(),
                name));
        }
    }

    private static void CheckAction(SyntaxNodeAnalysisContext ctx, SyntaxNode actionNode)
    {
        var nameToken = GetParenthesizedNameToken(actionNode);
        if (nameToken.Kind == SyntaxKind.None)
            return;

        var name = GetTokenDisplayValue(nameToken);
        if (!HasSocSuffix(name))
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ExtensionMemberMissingSocSuffix,
                nameToken.GetLocation(),
                name));
        }
    }

    private static void CheckField(SyntaxNodeAnalysisContext ctx, SyntaxNode fieldNode)
    {
        var nameToken = GetFieldNameToken(fieldNode);
        if (nameToken.Kind == SyntaxKind.None)
            return;

        var name = GetTokenDisplayValue(nameToken);
        if (!HasSocSuffix(name))
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ExtensionMemberMissingSocSuffix,
                nameToken.GetLocation(),
                name));
        }
    }

    internal static bool HasSocSuffix(string name) =>
        name.EndsWith(" SOC", StringComparison.Ordinal) ||
        name.EndsWith("SOC", StringComparison.Ordinal);

    internal static bool IsLocalMethod(SyntaxNode methodNode)
    {
        foreach (var token in methodNode.DescendantTokens())
        {
            if (token.Kind == EnumProvider.SyntaxKind.LocalKeyword)
                return true;
            if (string.Equals(token.ValueText, "procedure", StringComparison.OrdinalIgnoreCase))
                return false;
        }
        return false;
    }

    // Returns the name token of a parenthesized member (action, field action): first token after '('
    internal static SyntaxToken GetParenthesizedNameToken(SyntaxNode node)
    {
        bool seenOpenParen = false;
        foreach (var token in node.DescendantTokens())
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

    // Returns the name token of a field declaration: second token after '(' (skip ID, semicolon, get name)
    internal static SyntaxToken GetFieldNameToken(SyntaxNode fieldNode)
    {
        bool seenOpenParen = false;
        bool seenSemicolon = false;
        foreach (var token in fieldNode.DescendantTokens())
        {
            if (!seenOpenParen)
            {
                if (token.Kind == EnumProvider.SyntaxKind.OpenParenToken)
                    seenOpenParen = true;
                continue;
            }
            if (!seenSemicolon)
            {
                if (token.ToString().Trim() == ";")
                    seenSemicolon = true;
                continue;
            }
            return token;
        }
        return default;
    }

    internal static string GetTokenDisplayValue(SyntaxToken token)
    {
        var valueText = token.ValueText;
        if (string.IsNullOrEmpty(valueText))
            valueText = token.ToString().Trim();

        // BC SDK includes surrounding double-quotes in ValueText for quoted identifiers
        if (valueText.Length >= 2 && valueText[0] == '"' && valueText[^1] == '"')
            return valueText[1..^1];

        return valueText;
    }
}
