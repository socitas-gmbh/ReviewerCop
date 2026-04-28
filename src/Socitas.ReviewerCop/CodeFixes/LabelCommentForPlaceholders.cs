using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Text;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;

namespace Socitas.ReviewerCop.CodeFixes;

/// <summary>
/// RC0008 – Quick fix: insert a Comment property on a label that contains placeholders but lacks one.
/// Analyses call sites in the document (Message, Error, StrSubstNo, SetFilter, …) to determine what
/// fills each placeholder and builds a descriptive comment automatically.
/// </summary>
[CodeFixProvider(nameof(LabelCommentForPlaceholdersFixProvider))]
public sealed class LabelCommentForPlaceholdersFixProvider : CodeFixProvider
{
    private static readonly Regex PlaceholderPattern = new(@"[%#](\d+)", RegexOptions.Compiled);

    private sealed class AddCommentAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.QuickFix;
        public override bool SupportsFixAll { get; }
        public override string? FixAllSingleInstanceTitle => string.Empty;
        public override string? FixAllTitle => Title;

        public AddCommentAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument,
            string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            SupportsFixAll = generateFixAll;
        }
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticIds.LabelCommentForPlaceholders);

    public sealed override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public override Task RegisterCodeFixesAsync(CodeFixContext ctx)
    {
        ctx.RegisterCodeFix(
            new AddCommentAction(
                ReviewerCopAnalyzers.LabelCommentForPlaceholdersCodeAction,
                ct => AddCommentPropertyAsync(ctx.Document, ctx.Span, ct),
                nameof(LabelCommentForPlaceholdersFixProvider),
                generateFixAll: true),
            ctx.Diagnostics[0]);

        return Task.CompletedTask;
    }

    private static async Task<Document> AddCommentPropertyAsync(
        Document document, TextSpan stringTokenSpan, CancellationToken ct)
    {
        var sourceText = await document.GetTextAsync(ct).ConfigureAwait(false);
        var root = await document.GetSyntaxRootAsync(ct).ConfigureAwait(false);

        var commentValue = root is not null
            ? BuildComment(root, stringTokenSpan)
            : string.Empty;

        var newText = sourceText.WithChanges(
            new TextChange(new TextSpan(stringTokenSpan.End, 0), $", Comment = '{commentValue}'"));
        return document.WithText(newText);
    }

    // ── comment builder ─────────────────────────────────────────────────────

    private static string BuildComment(SyntaxNode root, TextSpan stringTokenSpan)
    {
        // Locate the string literal token by its span start position.
        var stringToken = root.DescendantTokens()
            .FirstOrDefault(t => t.Span.Start == stringTokenSpan.Start);

        var labelText = stringToken.ValueText ?? string.Empty;
        var maxIndex = GetMaxPlaceholderIndex(labelText);
        if (maxIndex == 0)
            return string.Empty;

        var labelVarName = FindLabelVariableName(stringToken.Parent);
        if (labelVarName is null)
            return BuildFallbackComment(maxIndex);

        var placeholderArgs = FindPlaceholderArguments(root, labelVarName);
        if (placeholderArgs.Count == 0)
            return BuildFallbackComment(maxIndex);

        var parts = new List<string>();
        for (var i = 1; i <= maxIndex; i++)
        {
            var caption = i <= placeholderArgs.Count
                ? ExtractCaption(placeholderArgs[i - 1])
                : "[description]";
            parts.Add($"%{i} = {caption}");
        }
        return string.Join(", ", parts);
    }

    private static int GetMaxPlaceholderIndex(string text)
    {
        var max = 0;
        foreach (Match m in PlaceholderPattern.Matches(text))
            max = Math.Max(max, int.Parse(m.Groups[1].Value));
        return max;
    }

    private static string BuildFallbackComment(int maxIndex) =>
        string.Join(", ", Enumerable.Range(1, maxIndex).Select(i => $"%{i} = [description]"));

    // ── label variable name ──────────────────────────────────────────────────

    /// <summary>
    /// Walks up from the LabelDataType node to find the enclosing variable or parameter
    /// declaration and returns the identifier name.
    /// </summary>
    private static string? FindLabelVariableName(SyntaxNode? node)
    {
        var current = node;
        while (current is not null)
        {
            var kind = current.Kind.ToString();
            if (kind.Contains("VariableDeclaration") || kind.Contains("Parameter"))
            {
                foreach (var token in current.DescendantTokens())
                {
                    if (token.Kind == SyntaxKind.IdentifierToken && !string.IsNullOrEmpty(token.ValueText))
                        return token.ValueText;
                }
            }
            current = current.Parent;
        }
        return null;
    }

    // ── call-site search ─────────────────────────────────────────────────────

    /// <summary>
    /// Scans the document for the first invocation that passes the label variable as its
    /// format-string argument and returns the subsequent (placeholder) argument nodes.
    ///
    /// Supported patterns:
    ///   Message / Error / StrSubstNo / Warning / …  → label is arg[0]
    ///   SetFilter                                    → label is arg[1] (field is arg[0])
    /// </summary>
    private static List<SyntaxNode> FindPlaceholderArguments(SyntaxNode root, string labelVarName)
    {
        foreach (var node in root.DescendantNodes())
        {
            if (node.Kind.ToString() != "InvocationExpression")
                continue;

            var args = GetArguments(node);
            if (args.Count == 0)
                continue;

            // Label as first argument: Message, Error, StrSubstNo, etc.
            if (MatchesLabel(args[0], labelVarName))
                return args.Skip(1).ToList();

            // Label as second argument: SetFilter(Field, Label, Arg1, Arg2, …)
            if (args.Count > 1 && MatchesLabel(args[1], labelVarName))
            {
                var funcName = LastSegment(GetFunctionText(node));
                if (string.Equals(funcName, "SetFilter", StringComparison.OrdinalIgnoreCase))
                    return args.Skip(2).ToList();
            }
        }
        return [];
    }

    /// <summary>
    /// Returns the child nodes of the argument list of an invocation expression.
    /// The argument list is identified as the child whose text starts with '('.
    /// </summary>
    private static List<SyntaxNode> GetArguments(SyntaxNode invocationNode)
    {
        foreach (var child in invocationNode.ChildNodes())
        {
            if (child.ToString().TrimStart().StartsWith('('))
                return [.. child.ChildNodes()];
        }
        return [];
    }

    private static bool MatchesLabel(SyntaxNode argNode, string labelVarName) =>
        string.Equals(argNode.ToString().Trim().Trim('"'), labelVarName, StringComparison.OrdinalIgnoreCase);

    private static string GetFunctionText(SyntaxNode invocationNode) =>
        invocationNode.ChildNodes().FirstOrDefault()?.ToString().Trim() ?? string.Empty;

    private static string LastSegment(string text)
    {
        var dot = text.LastIndexOf('.');
        return dot >= 0 ? text[(dot + 1)..] : text;
    }

    // ── caption extraction ───────────────────────────────────────────────────

    /// <summary>
    /// Derives a human-readable caption from a placeholder argument node.
    ///   Rec.FieldName        → "FieldName"
    ///   Rec."Field Name"     → "Field Name"
    ///   SomeVariable         → "SomeVariable"
    /// </summary>
    private static string ExtractCaption(SyntaxNode argNode)
    {
        var text = argNode.ToString().Trim();

        var dot = text.IndexOf('.');
        if (dot >= 0)
        {
            var fieldPart = text[(dot + 1)..].Trim();
            // Strip double-quote delimiters used for identifiers with spaces: "Field Name"
            if (fieldPart.StartsWith('"') && fieldPart.EndsWith('"'))
                fieldPart = fieldPart[1..^1];
            return fieldPart;
        }

        // Simple identifier — strip quotes if present
        if (text.StartsWith('"') && text.EndsWith('"'))
            text = text[1..^1];
        return text;
    }
}
