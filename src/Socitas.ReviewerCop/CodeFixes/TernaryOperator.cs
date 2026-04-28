using System.Collections.Immutable;
using Socitas.ReviewerCop.Analyzers;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Text;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;

namespace Socitas.ReviewerCop.CodeFixes;

/// <summary>
/// RC0009 – Quick fix: rewrite a simple if-then-else assignment as a ternary expression.
/// Only offered when the condition contains no logical operators (and/or).
/// </summary>
[CodeFixProvider(nameof(TernaryOperatorFixProvider))]
public sealed class TernaryOperatorFixProvider : CodeFixProvider
{
    private sealed class RewriteAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.QuickFix;
        public override bool SupportsFixAll { get; }
        public override string? FixAllSingleInstanceTitle => string.Empty;
        public override string? FixAllTitle => Title;

        public RewriteAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument,
            string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            SupportsFixAll = generateFixAll;
        }
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticIds.TernaryOperator);

    public sealed override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext ctx)
    {
        var root = await ctx.Document.GetSyntaxRootAsync(ctx.CancellationToken).ConfigureAwait(false);

        // The diagnostic is reported on the 'if' keyword token; find the enclosing IfStatement
        var ifKeyword = root?.FindToken(ctx.Span.Start);
        var ifStmt = ifKeyword?.Parent as IfStatementSyntax;
        if (ifStmt is null)
            return;

        if (ifStmt.Statement is not AssignmentStatementSyntax thenAssign)
            return;
        if (ifStmt.ElseStatement is not AssignmentStatementSyntax elseAssign)
            return;

        // The analyzer already guarantees a simple condition, but guard again for safety
        if (!TernaryOperator.IsSimpleCondition(ifStmt.Condition))
            return;

        ctx.RegisterCodeFix(
            new RewriteAction(
                ReviewerCopAnalyzers.TernaryOperatorCodeAction,
                ct => RewriteAsync(ctx.Document, ctx.Span, ct),
                nameof(TernaryOperatorFixProvider),
                generateFixAll: true),
            ctx.Diagnostics[0]);
    }

    private static async Task<Document> RewriteAsync(
        Document document, TextSpan diagnosticSpan, CancellationToken ct)
    {
        var sourceText = await document.GetTextAsync(ct).ConfigureAwait(false);
        var root = await document.GetSyntaxRootAsync(ct).ConfigureAwait(false);

        var ifKeyword = root?.FindToken(diagnosticSpan.Start);
        if (ifKeyword is not { } kw || kw.Parent is not IfStatementSyntax ifStmt)
            return document;

        if (ifStmt.Statement is not AssignmentStatementSyntax thenAssign)
            return document;
        if (ifStmt.ElseStatement is not AssignmentStatementSyntax elseAssign)
            return document;

        var condition = ifStmt.Condition.ToString().Trim();
        var target = thenAssign.Target.ToString().Trim();
        var thenValue = thenAssign.Source.ToString().Trim();
        var elseValue = elseAssign.Source.ToString().Trim();

        var replacement = $"{target} := {condition} ? {thenValue} : {elseValue};";

        return document.WithText(sourceText.WithChanges(
            new TextChange(ifStmt.Span, replacement)));
    }
}
