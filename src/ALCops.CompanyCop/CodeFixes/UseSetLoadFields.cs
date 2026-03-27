using System.Collections.Immutable;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Text;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;

namespace ALCops.CompanyCop.CodeFixes;

/// <summary>
/// CC0006 - Quick fix: insert SetLoadFields before Find* or update an existing SetLoadFields call.
/// </summary>
[CodeFixProvider(nameof(UseSetLoadFieldsFixProvider))]
public sealed class UseSetLoadFieldsFixProvider : CodeFixProvider
{
    private static readonly ImmutableHashSet<string> FindMethods =
        ImmutableHashSet.Create(StringComparer.OrdinalIgnoreCase,
            "FindSet", "FindFirst", "FindLast", "Find");

    private const string SetLoadFieldsMethod = "SetLoadFields";
    private const string CodeActionTitle = "ALCops: Add or update SetLoadFields()";

    private sealed class ReplaceAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.QuickFix;
        public override bool SupportsFixAll { get; }
        public override string? FixAllSingleInstanceTitle => string.Empty;
        public override string? FixAllTitle => Title;

        public ReplaceAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument,
            string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            SupportsFixAll = generateFixAll;
        }
    }

    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticIds.UseSetLoadFields);

    public sealed override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext ctx)
    {
        var root = await ctx.Document.GetSyntaxRootAsync(ctx.CancellationToken).ConfigureAwait(false);
        if (root is null)
            return;

        var node = root.FindNode(ctx.Span);
        var findInvocation = node.FirstAncestorOrSelf<InvocationExpressionSyntax>();
        if (findInvocation is null)
            return;

        if (!TryGetFindReceiverName(findInvocation, out var receiverName))
            return;

        if (!FindMethods.Contains(GetMemberName(findInvocation)))
            return;

        ctx.RegisterCodeFix(
            new ReplaceAction(
                CodeActionTitle,
                ct => AddOrUpdateSetLoadFieldsAsync(ctx.Document, findInvocation, receiverName!, ct),
                nameof(UseSetLoadFieldsFixProvider),
                generateFixAll: true),
            ctx.Diagnostics[0]);
    }

    private static async Task<Document> AddOrUpdateSetLoadFieldsAsync(
        Document document,
        InvocationExpressionSyntax findInvocation,
        string receiverName,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
            return document;

        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

        var enclosingRoutine = GetEnclosingRoutine(findInvocation);
        if (enclosingRoutine is null)
            return document;

        var usedFields = GetUsedFieldsAfterFind(enclosingRoutine, findInvocation, receiverName);
        if (usedFields.Count == 0)
            return document;

        if (TryGetSetLoadFieldsInvocation(enclosingRoutine, findInvocation, receiverName, out var setLoadFieldsInvocation))
        {
            var mergedFields = MergeWithExistingFields(setLoadFieldsInvocation!, usedFields);
            var replacementInvocationText = $"{receiverName}.{SetLoadFieldsMethod}({string.Join(", ", mergedFields)});";

            var statement = setLoadFieldsInvocation!.FirstAncestorOrSelf<ExpressionStatementSyntax>();
            if (statement is not null)
            {
                var change = new TextChange(statement.Span, replacementInvocationText);
                return document.WithText(text.WithChanges(change));
            }

            var fallbackChange = new TextChange(setLoadFieldsInvocation.Span, $"{receiverName}.{SetLoadFieldsMethod}({string.Join(", ", mergedFields)})");
            return document.WithText(text.WithChanges(fallbackChange));
        }

        var line = text.Lines.GetLineFromPosition(findInvocation.Span.Start);
        var lineText = text.GetSubText(line.Span).ToString();
        var indentLength = lineText.TakeWhile(char.IsWhiteSpace).Count();
        var indent = lineText.Substring(0, indentLength);
        var newCall = $"{indent}{receiverName}.{SetLoadFieldsMethod}({string.Join(", ", usedFields)});{Environment.NewLine}";

        var insertChange = new TextChange(new TextSpan(line.Start, 0), newCall);
        return document.WithText(text.WithChanges(insertChange));
    }

    private static SyntaxNode? GetEnclosingRoutine(SyntaxNode node)
    {
        var current = node;
        while (current is not null &&
               !IsSyntaxKind(current, "TriggerDeclaration") &&
               !IsSyntaxKind(current, "MethodDeclaration"))
        {
            current = current.Parent;
        }

        return current;
    }

    private static bool IsSyntaxKind(SyntaxNode node, string expectedKindName) =>
        string.Equals(node.Kind.ToString(), expectedKindName, StringComparison.OrdinalIgnoreCase);

    private static bool TryGetFindReceiverName(InvocationExpressionSyntax invocation, out string? receiverName)
    {
        receiverName = null;

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return false;

        if (memberAccess.Expression is not IdentifierNameSyntax receiver)
            return false;

        receiverName = receiver.Identifier.ValueText;
        return !string.IsNullOrWhiteSpace(receiverName);
    }

    private static string GetMemberName(InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return string.Empty;

        return memberAccess.Name.Identifier.ValueText ?? string.Empty;
    }

    private static List<string> GetUsedFieldsAfterFind(
        SyntaxNode enclosingRoutine,
        InvocationExpressionSyntax findInvocation,
        string receiverName)
    {
        var fieldsByNormalized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var access in enclosingRoutine.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            if (access.SpanStart <= findInvocation.SpanStart)
                continue;

            if (access.Expression is not IdentifierNameSyntax identifier ||
                !string.Equals(identifier.Identifier.ValueText, receiverName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (access.Parent is InvocationExpressionSyntax invocation && ReferenceEquals(invocation.Expression, access))
                continue;

            var fieldText = access.Name.ToFullString().Trim();
            if (string.IsNullOrWhiteSpace(fieldText))
                continue;

            var normalized = NormalizeFieldName(fieldText);
            if (!fieldsByNormalized.ContainsKey(normalized))
                fieldsByNormalized[normalized] = fieldText;
        }

        return fieldsByNormalized.Values.ToList();
    }

    private static bool TryGetSetLoadFieldsInvocation(
        SyntaxNode enclosingRoutine,
        InvocationExpressionSyntax findInvocation,
        string receiverName,
        out InvocationExpressionSyntax? setLoadFieldsInvocation)
    {
        setLoadFieldsInvocation = null;

        foreach (var invocation in enclosingRoutine.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.SpanStart >= findInvocation.SpanStart)
                continue;

            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
                continue;

            if (memberAccess.Expression is not IdentifierNameSyntax callReceiver ||
                !string.Equals(callReceiver.Identifier.ValueText, receiverName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!string.Equals(memberAccess.Name.Identifier.ValueText, SetLoadFieldsMethod, StringComparison.OrdinalIgnoreCase))
                continue;

            if (setLoadFieldsInvocation is null || invocation.SpanStart > setLoadFieldsInvocation.SpanStart)
                setLoadFieldsInvocation = invocation;
        }

        return setLoadFieldsInvocation is not null;
    }

    private static List<string> MergeWithExistingFields(
        InvocationExpressionSyntax setLoadFieldsInvocation,
        IReadOnlyCollection<string> usedFields)
    {
        var merged = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var arg in setLoadFieldsInvocation.ArgumentList.Arguments)
        {
            var fieldText = arg.ToFullString().Trim();
            if (string.IsNullOrWhiteSpace(fieldText))
                continue;

            var normalized = NormalizeFieldName(fieldText);
            if (seen.Add(normalized))
                merged.Add(fieldText);
        }

        foreach (var field in usedFields)
        {
            var normalized = NormalizeFieldName(field);
            if (seen.Add(normalized))
                merged.Add(field);
        }

        return merged;
    }

    private static string NormalizeFieldName(string text)
    {
        var normalized = text.Trim();
        if (normalized.StartsWith("\"", StringComparison.Ordinal) &&
            normalized.EndsWith("\"", StringComparison.Ordinal) &&
            normalized.Length > 1)
        {
            normalized = normalized.Substring(1, normalized.Length - 2);
        }

        return normalized;
    }
}
