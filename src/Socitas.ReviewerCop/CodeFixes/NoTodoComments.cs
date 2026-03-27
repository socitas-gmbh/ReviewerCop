using System.Collections.Immutable;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Text;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;

namespace Socitas.ReviewerCop.CodeFixes;

/// <summary>
/// CC0001 – Quick fix: remove the TODO comment line.
/// </summary>
[CodeFixProvider(nameof(NoTodoCommentsFixProvider))]
public sealed class NoTodoCommentsFixProvider : CodeFixProvider
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
        ImmutableArray.Create(DiagnosticIds.NoTodoComments);

    public sealed override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public override Task RegisterCodeFixesAsync(CodeFixContext ctx)
    {
        ctx.RegisterCodeFix(
            new RemoveAction(
                ReviewerCopAnalyzers.NoTodoCommentsCodeAction,
                ct => RemoveTodoCommentAsync(ctx.Document, ctx.Span, ct),
                nameof(NoTodoCommentsFixProvider),
                generateFixAll: true),
            ctx.Diagnostics[0]);

        return Task.CompletedTask;
    }

    private static async Task<Document> RemoveTodoCommentAsync(
        Document document, TextSpan span, CancellationToken cancellationToken)
    {
        var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var line = sourceText.Lines.GetLineFromPosition(span.Start);

        // If the comment is the only non-whitespace content on this line, remove the whole line.
        // Otherwise (trailing comment), remove from the comment start to end of line.
        var lineText = sourceText.GetSubText(line.Span).ToString();
        var beforeComment = lineText.Substring(0, span.Start - line.Start);

        TextSpan removeSpan = string.IsNullOrWhiteSpace(beforeComment)
            ? line.SpanIncludingLineBreak
            : TextSpan.FromBounds(span.Start, line.End);

        var newText = sourceText.WithChanges(new TextChange(removeSpan, ""));
        return document.WithText(newText);
    }
}
