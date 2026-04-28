using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Socitas.ReviewerCop.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Text;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;

namespace Socitas.AICop.CodeFixes;

[CodeFixProvider(nameof(UseActionRefFixProvider))]
public sealed class UseActionRefFixProvider : CodeFixProvider
{
    private sealed class RefactorAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.QuickFix;
        public override bool SupportsFixAll { get; }
        public override string? FixAllSingleInstanceTitle => string.Empty;
        public override string? FixAllTitle => Title;

        public RefactorAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument,
            string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            SupportsFixAll = generateFixAll;
        }
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticIds.UseActionRef);

    public sealed override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext ctx)
    {
        var root = await ctx.Document.GetSyntaxRootAsync(ctx.CancellationToken).ConfigureAwait(false);
        if (root is null)
            return;

        // Walk up from the diagnostic token to the PageAction node
        var node = root.FindNode(ctx.Span);
        while (node != null && node.Kind != EnumProvider.SyntaxKind.PageAction)
            node = node.Parent;
        if (node is null)
            return;

        var actionNode = node;
        ctx.RegisterCodeFix(
            new RefactorAction(
                AICopAnalyzers.UseActionRefCodeAction,
                ct => ApplyFixAsync(ctx.Document, actionNode, ct),
                nameof(UseActionRefFixProvider),
                generateFixAll: true),
            ctx.Diagnostics[0]);

        var actionName = GetActionName(actionNode) ?? string.Empty;
        ctx.RegisterCodeFix(
            new GuidanceCodeAction(
                string.Format(AICopAnalyzers.UseActionRefGuidanceAction, actionName),
                nameof(UseActionRefFixProvider) + "_Guidance",
                ctx.Document),
            ctx.Diagnostics[0]);
    }

    private static async Task<Document> ApplyFixAsync(
        Document document, SyntaxNode actionNode, CancellationToken ct)
    {
        var sourceText = await document.GetTextAsync(ct).ConfigureAwait(false);
        var text = sourceText.ToString();
        var newline = text.Contains("\r\n") ? "\r\n" : "\n";

        var actionName = GetActionName(actionNode);
        if (actionName is null)
            return document;

        // ── 1. Collect Property lines to remove ───────────────────────────────
        var changes = new List<TextChange>();
        foreach (var descendant in actionNode.DescendantNodes())
        {
            if (!string.Equals(descendant.Kind.ToString(), "Property", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!IsPromotedRelatedProperty(descendant))
                continue;

            changes.Add(new TextChange(ExpandToFullLine(text, descendant.Span), ""));
        }

        // ── 2. Find containing area and its parent (the actions container) ────
        var containingArea = FindContainingArea(actionNode);
        var areasContainer = containingArea?.Parent;
        if (areasContainer is null)
            return ApplyChangesIfAny(document, sourceText, changes);

        var areaIndent = containingArea is not null
            ? GetLeadingIndentation(text, containingArea.SpanStart)
            : "        ";
        var actionIndent = GetLeadingIndentation(text, actionNode.SpanStart);
        var actionrefLine = $"{actionIndent}actionref({actionName}_Promoted; {actionName}) {{ }}{newline}";

        // ── 3. Check for an existing area(Promoted) sibling ───────────────────
        SyntaxNode? promotedArea = null;
        foreach (var sibling in areasContainer.DescendantNodes())
        {
            if (sibling.Kind != EnumProvider.SyntaxKind.PageActionArea)
                continue;
            if (sibling.Parent != areasContainer)
                continue;
            if (IsPromotedArea(sibling))
            {
                promotedArea = sibling;
                break;
            }
        }

        // ── 4. Build insertion change ─────────────────────────────────────────
        if (promotedArea is not null)
        {
            // Insert actionref before the closing brace of the existing area(Promoted)
            var closeBrace = FindLastCloseBrace(promotedArea);
            if (closeBrace.Kind != SyntaxKind.None)
            {
                var insertPos = GetLineStart(text, closeBrace.SpanStart);
                changes.Add(new TextChange(new TextSpan(insertPos, 0), actionrefLine));
            }
        }
        else
        {
            // Insert a new area(Promoted) block before the closing brace of the actions container
            var closeBrace = FindLastCloseBrace(areasContainer);
            if (closeBrace.Kind != SyntaxKind.None)
            {
                var insertPos = GetLineStart(text, closeBrace.SpanStart);
                var sb = new StringBuilder();
                sb.Append(areaIndent).Append("area(Promoted)").Append(newline);
                sb.Append(areaIndent).Append('{').Append(newline);
                sb.Append(actionrefLine);
                sb.Append(areaIndent).Append('}').Append(newline);
                changes.Add(new TextChange(new TextSpan(insertPos, 0), sb.ToString()));
            }
        }

        return ApplyChangesIfAny(document, sourceText, changes);
    }

    private static Document ApplyChangesIfAny(
        Document document, SourceText sourceText, List<TextChange> changes)
    {
        if (changes.Count == 0)
            return document;
        return document.WithText(sourceText.WithChanges(changes.OrderBy(c => c.Span.Start)));
    }

    private static string? GetActionName(SyntaxNode actionNode)
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
                return string.IsNullOrEmpty(token.ValueText) ? token.ToString().Trim('"') : token.ValueText;
        }
        return null;
    }

    private static bool IsPromotedRelatedProperty(SyntaxNode propertyNode)
    {
        foreach (var token in propertyNode.DescendantTokens())
        {
            if (!string.Equals(token.Kind.ToString(), "IdentifierToken", StringComparison.OrdinalIgnoreCase))
                continue;

            return string.Equals(token.ValueText, "Promoted", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(token.ValueText, "PromotedOnly", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(token.ValueText, "PromotedCategory", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(token.ValueText, "PromotedIsBig", StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    private static SyntaxNode? FindContainingArea(SyntaxNode actionNode)
    {
        var current = actionNode.Parent;
        while (current is not null)
        {
            if (current.Kind == EnumProvider.SyntaxKind.PageActionArea)
                return current;
            current = current.Parent;
        }
        return null;
    }

    private static bool IsPromotedArea(SyntaxNode areaNode)
    {
        bool seenOpenParen = false;
        foreach (var token in areaNode.DescendantTokens())
        {
            if (token.Kind == EnumProvider.SyntaxKind.OpenParenToken)
            {
                seenOpenParen = true;
                continue;
            }

            if (seenOpenParen)
                return string.Equals(token.ValueText, "Promoted", StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    private static SyntaxToken FindLastCloseBrace(SyntaxNode node)
    {
        SyntaxToken last = default;
        foreach (var token in node.DescendantTokens())
        {
            if (token.Kind == EnumProvider.SyntaxKind.CloseBraceToken)
                last = token;
        }
        return last;
    }

    private static TextSpan ExpandToFullLine(string text, TextSpan span)
    {
        var start = span.Start;
        while (start > 0 && text[start - 1] != '\n')
            start--;

        var end = span.End;
        while (end < text.Length && text[end] != '\r' && text[end] != '\n')
            end++;
        if (end < text.Length)
        {
            if (text[end] == '\r' && end + 1 < text.Length && text[end + 1] == '\n')
                end += 2;
            else
                end++;
        }

        return TextSpan.FromBounds(start, end);
    }

    private static int GetLineStart(string text, int position)
    {
        while (position > 0 && text[position - 1] != '\n')
            position--;
        return position;
    }

    private static string GetLeadingIndentation(string text, int tokenStart)
    {
        var lineStart = tokenStart;
        while (lineStart > 0 && text[lineStart - 1] != '\n')
            lineStart--;

        var sb = new StringBuilder();
        var pos = lineStart;
        while (pos < tokenStart && (text[pos] == ' ' || text[pos] == '\t'))
            sb.Append(text[pos++]);
        return sb.ToString();
    }
}
