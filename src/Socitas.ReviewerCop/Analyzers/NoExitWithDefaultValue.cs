using System.Collections.Immutable;
using Socitas.ReviewerCop.Common.Extensions;
using Socitas.ReviewerCop.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace Socitas.ReviewerCop.Analyzers;

/// <summary>
/// CC0015 – Do not use exit() with an explicit default value.
/// Flags exit(false), exit(0), exit('') etc. — a bare exit; is cleaner
/// because AL already returns the type's default when no value is given.
/// </summary>
[DiagnosticAnalyzer]
public sealed class NoExitWithDefaultValue : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.NoExitWithDefaultValue);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(
            CheckExitStatement,
            EnumProvider.SyntaxKind.ExitStatement);

    private void CheckExitStatement(SyntaxNodeAnalysisContext ctx)
    {
        if (ctx.IsObsolete())
            return;

        var exitNode = ctx.Node;

        // The exit argument is a direct LiteralExpression child of ExitStatement.
        // A bare "exit;" has no such child.
        var argument = exitNode.ChildNodes().FirstOrDefault();
        if (argument is null || argument.Kind.ToString() != "LiteralExpression")
            return;

        if (!IsDefaultLiteral(argument))
            return;

        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.NoExitWithDefaultValue,
            exitNode.GetLocation(),
            argument.ToString().Trim()));
    }

    /// <summary>
    /// Returns true when the LiteralExpression contains a default value:
    /// false, 0, or '' (empty string).
    /// </summary>
    private static bool IsDefaultLiteral(SyntaxNode literal)
    {
        var token = literal.DescendantTokens().FirstOrDefault();

        // false
        if (token.IsKind(EnumProvider.SyntaxKind.FalseKeyword))
            return true;

        // 0
        if (token.IsKind(EnumProvider.SyntaxKind.Int32LiteralToken)
            && token.ValueText == "0")
            return true;

        // '' — represented as a single StringLiteralToken with ValueText "''"
        if (token.IsKind(EnumProvider.SyntaxKind.StringLiteralToken)
            && (token.ValueText == "''" || token.ValueText == "" || token.ToString().Trim() == "''"))
            return true;

        return false;
    }
}
