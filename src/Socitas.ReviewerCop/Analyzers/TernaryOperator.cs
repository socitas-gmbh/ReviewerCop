using System.Collections.Immutable;
using Socitas.ReviewerCop.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace Socitas.ReviewerCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class TernaryOperator : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.TernaryOperator);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(CheckIfStatement, EnumProvider.SyntaxKind.IfStatement);

    private static readonly HashSet<string> StringAlterationFunctions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "CopyStr", "DelChr", "PadStr", "UpperCase", "LowerCase", "ConvertStr", "IncStr",
        };

    private static void CheckIfStatement(SyntaxNodeAnalysisContext ctx)
    {
        var ifStmt = (IfStatementSyntax)ctx.Node;

        if (ifStmt.ElseKeywordToken.IsMissing)
            return;

        if (ifStmt.Statement is not AssignmentStatementSyntax thenAssign)
            return;

        if (ifStmt.ElseStatement is not AssignmentStatementSyntax elseAssign)
            return;

        var thenTarget = thenAssign.Target.ToString().Trim();
        var elseTarget = elseAssign.Target.ToString().Trim();

        if (!string.Equals(thenTarget, elseTarget, StringComparison.Ordinal))
            return;

        if (!IsSimpleCondition(ifStmt.Condition))
            return;

        if (RhsContainsStringAlterationCall(thenAssign) || RhsContainsStringAlterationCall(elseAssign))
            return;

        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.TernaryOperator,
            ifStmt.IfKeywordToken.GetLocation(),
            thenTarget));
    }

    private static bool RhsContainsStringAlterationCall(AssignmentStatementSyntax assign)
    {
        var text = assign.ToString();
        var colonEq = text.IndexOf(":=", StringComparison.Ordinal);
        if (colonEq < 0)
            return false;
        var rhs = text[(colonEq + 2)..];
        foreach (var fn in StringAlterationFunctions)
            if (rhs.Contains(fn + "(", StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    internal static bool IsSimpleCondition(SyntaxNode condition)
    {
        foreach (var token in condition.DescendantTokens())
        {
            var kind = token.Kind.ToString();
            if (kind == "AndKeyword" || kind == "OrKeyword")
                return false;
        }

        foreach (var node in condition.DescendantNodes())
        {
            if (node.Kind.ToString() == "IfStatement")
                return false;
        }

        return true;
    }
}
