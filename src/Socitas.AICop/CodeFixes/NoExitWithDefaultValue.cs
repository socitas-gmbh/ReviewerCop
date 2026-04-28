using System.Collections.Immutable;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Text;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;

namespace Socitas.AICop.CodeFixes;

/// <summary>
/// AI0006 – Quick fix: remove the redundant default argument from exit(value), leaving a bare exit.
/// </summary>
[CodeFixProvider(nameof(NoExitWithDefaultValueFixProvider))]
public sealed class NoExitWithDefaultValueFixProvider : CodeFixProvider
{
    private sealed class StripArgumentAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.QuickFix;
        public override bool SupportsFixAll { get; }
        public override string? FixAllSingleInstanceTitle => string.Empty;
        public override string? FixAllTitle => Title;

        public StripArgumentAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument,
            string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            SupportsFixAll = generateFixAll;
        }
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticIds.NoExitWithDefaultValue);

    public sealed override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext ctx)
    {
        var root = await ctx.Document.GetSyntaxRootAsync(ctx.CancellationToken).ConfigureAwait(false);
        if (root is null)
            return;

        ctx.RegisterCodeFix(
            new StripArgumentAction(
                AICopAnalyzers.NoExitWithDefaultValueCodeAction,
                ct => StripExitArgumentAsync(ctx.Document, ctx.Span, ct),
                nameof(NoExitWithDefaultValueFixProvider),
                generateFixAll: true),
            ctx.Diagnostics[0]);

        ctx.RegisterCodeFix(
            new GuidanceCodeAction(
                AICopAnalyzers.NoExitWithDefaultValueGuidanceAction,
                nameof(NoExitWithDefaultValueFixProvider) + "_Guidance",
                ctx.Document),
            ctx.Diagnostics[0]);
    }

    private static async Task<Document> StripExitArgumentAsync(
        Document document, TextSpan diagnosticSpan, CancellationToken ct)
    {
        var sourceText = await document.GetTextAsync(ct).ConfigureAwait(false);
        var root = await document.GetSyntaxRootAsync(ct).ConfigureAwait(false);
        if (root is null)
            return document;

        // Walk up to the ExitStatement node — FindNode may land on a child.
        var node = root.FindNode(diagnosticSpan);
        while (node != null && node.Kind.ToString() != "ExitStatement")
            node = node.Parent;
        if (node is null)
            return document;

        var nodeText = node.ToString();
        var parenStart = nodeText.IndexOf('(');
        if (parenStart < 0)
            return document;
        var parenEnd = nodeText.LastIndexOf(')');
        if (parenEnd < 0)
            return document;

        var removeSpan = TextSpan.FromBounds(
            node.Span.Start + parenStart,
            node.Span.Start + parenEnd + 1);

        return document.WithText(sourceText.WithChanges(new TextChange(removeSpan, "")));
    }
}
