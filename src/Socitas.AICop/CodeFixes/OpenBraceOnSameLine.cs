using System.Collections.Immutable;
using Socitas.ReviewerCop.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;

namespace Socitas.AICop.CodeFixes;

/// <summary>
/// CC0012 – Quick fix: move the opening brace to the same line as the declaration.
/// </summary>
[CodeFixProvider(nameof(OpenBraceOnSameLineFixProvider))]
public sealed class OpenBraceOnSameLineFixProvider : CodeFixProvider
{
    private sealed class FixAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.QuickFix;
        public override bool SupportsFixAll { get; }
        public override string? FixAllSingleInstanceTitle => string.Empty;
        public override string? FixAllTitle => Title;

        public FixAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument,
            string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            SupportsFixAll = generateFixAll;
        }
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticIds.OpenBraceOnSameLine);

    public sealed override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext ctx)
    {
        var syntaxRoot = await ctx.Document.GetSyntaxRootAsync(ctx.CancellationToken).ConfigureAwait(false);
        if (syntaxRoot is null)
            return;

        var token = syntaxRoot.FindToken(ctx.Span.Start);
        if (token.Kind != EnumProvider.SyntaxKind.OpenBraceToken)
            return;

        ctx.RegisterCodeFix(
            new FixAction(
                AICopAnalyzers.OpenBraceOnSameLineCodeAction,
                ct => MoveOpenBraceAsync(ctx.Document, token, ct),
                nameof(OpenBraceOnSameLineFixProvider),
                generateFixAll: true),
            ctx.Diagnostics[0]);

        ctx.RegisterCodeFix(
            new GuidanceCodeAction(
                AICopAnalyzers.OpenBraceOnSameLineGuidanceAction,
                nameof(OpenBraceOnSameLineFixProvider) + "_Guidance",
                ctx.Document),
            ctx.Diagnostics[0]);
    }

    private static async Task<Document> MoveOpenBraceAsync(
        Document document, SyntaxToken openBrace, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
            return document;

        // Find the close paren token before the open brace
        var prevToken = openBrace.GetPreviousToken();
        if (prevToken.Kind != EnumProvider.SyntaxKind.CloseParenToken)
            return document;

        // Replace the close paren's trailing trivia and the open brace's leading trivia
        // to collapse the newline into a single space
        var newCloseParen = prevToken.WithTrailingTrivia(SyntaxFactory.Space);
        var newOpenBrace = openBrace.WithLeadingTrivia();

        var newRoot = root
            .ReplaceToken(prevToken, newCloseParen);

        // Re-find the open brace in the new tree (position may have shifted)
        var updatedOpenBrace = newRoot.FindToken(openBrace.Span.Start);
        if (updatedOpenBrace.Kind == EnumProvider.SyntaxKind.OpenBraceToken)
            newRoot = newRoot.ReplaceToken(updatedOpenBrace, newOpenBrace);

        return document.WithSyntaxRoot(newRoot);
    }
}
