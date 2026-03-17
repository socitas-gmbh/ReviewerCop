using System.Collections.Immutable;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;

namespace ALCops.CompanyCop.CodeFixes;

/// <summary>
/// CC0008 – Quick fix: remove the Modify() call from inside an OnValidate trigger.
/// </summary>
[CodeFixProvider(nameof(NoModifyInOnValidateFixProvider))]
public sealed class NoModifyInOnValidateFixProvider : CodeFixProvider
{
    private sealed class RemoveAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.QuickFix;
        public override bool SupportsFixAll { get; }
        public override string? FixAllSingleInstanceTitle => string.Empty;
        public override string? FixAllTitle => Title;

        public RemoveAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument,
            string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            SupportsFixAll = generateFixAll;
        }
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticIds.NoModifyInOnValidate);

    public sealed override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext ctx)
    {
        var syntaxRoot = await ctx.Document.GetSyntaxRootAsync(ctx.CancellationToken).ConfigureAwait(false);
        if (syntaxRoot is null)
            return;

        var node = syntaxRoot.FindNode(ctx.Span);
        // The diagnostic is on the InvocationExpression; find the containing statement
        var statement = node.FirstAncestorOrSelf<ExpressionStatementSyntax>();
        if (statement is null)
            return;

        ctx.RegisterCodeFix(
            new RemoveAction(
                CompanyCopAnalyzers.NoModifyInOnValidateCodeAction,
                ct => RemoveStatementAsync(ctx.Document, statement, ct),
                nameof(NoModifyInOnValidateFixProvider),
                generateFixAll: true),
            ctx.Diagnostics[0]);
    }

    private static async Task<Document> RemoveStatementAsync(
        Document document, ExpressionStatementSyntax statement, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
            return document;

        var newRoot = root.RemoveNode(statement, SyntaxRemoveOptions.KeepNoTrivia);
        if (newRoot is null)
            return document;

        return document.WithSyntaxRoot(newRoot);
    }
}
