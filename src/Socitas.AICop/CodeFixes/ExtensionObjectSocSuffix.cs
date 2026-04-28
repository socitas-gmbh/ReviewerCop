using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Socitas.AICop.Analyzers;
using Socitas.ReviewerCop.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Text;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;

namespace Socitas.AICop.CodeFixes;

/// <summary>
/// AI0008 – Append 'SOC' suffix to non-local procedures, actions, and fields in extension objects.
/// AI0009 – Remove 'SOC' suffix from local procedures in extension objects.
/// </summary>
[CodeFixProvider(nameof(ExtensionObjectSocSuffixFixProvider))]
public sealed class ExtensionObjectSocSuffixFixProvider : CodeFixProvider
{
    private sealed class RenameAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.QuickFix;
        public override bool SupportsFixAll { get; }
        public override string? FixAllSingleInstanceTitle => string.Empty;
        public override string? FixAllTitle => Title;

        public RenameAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument,
            string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            SupportsFixAll = generateFixAll;
        }
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(
            DiagnosticIds.ExtensionMemberMissingSocSuffix,
            DiagnosticIds.LocalProcedureHasSocSuffix);

    public sealed override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext ctx)
    {
        var root = await ctx.Document.GetSyntaxRootAsync(ctx.CancellationToken).ConfigureAwait(false);
        if (root is null)
            return;

        var diagnosticId = ctx.Diagnostics[0].Id;
        var token = root.FindToken(ctx.Span.Start);

        if (diagnosticId == DiagnosticIds.LocalProcedureHasSocSuffix)
        {
            RegisterRemoveSocFix(ctx, token);
            ctx.RegisterCodeFix(
                new GuidanceCodeAction(
                    string.Format(AICopAnalyzers.LocalProcedureHasSocSuffixGuidanceAction, token.ValueText ?? string.Empty),
                    nameof(ExtensionObjectSocSuffixFixProvider) + "_GuidanceRemove",
                    ctx.Document),
                ctx.Diagnostics[0]);
            return;
        }

        // AI0008: find what kind of member the token belongs to and register the right fix
        var memberName = ExtensionObjectSocSuffix.GetTokenDisplayValue(token);
        var node = token.Parent;
        while (node is not null)
        {
            if (node is MethodDeclarationSyntax)
            {
                RegisterProcedureFix(ctx, token);
                ctx.RegisterCodeFix(
                    new GuidanceCodeAction(
                        string.Format(AICopAnalyzers.ExtensionMemberMissingSocSuffixGuidanceAction, memberName),
                        nameof(ExtensionObjectSocSuffixFixProvider) + "_GuidanceAdd",
                        ctx.Document),
                    ctx.Diagnostics[0]);
                return;
            }
            if (node.Kind == EnumProvider.SyntaxKind.PageAction)
            {
                RegisterActionFix(ctx, token);
                ctx.RegisterCodeFix(
                    new GuidanceCodeAction(
                        string.Format(AICopAnalyzers.ExtensionMemberMissingSocSuffixGuidanceAction, memberName),
                        nameof(ExtensionObjectSocSuffixFixProvider) + "_GuidanceAdd",
                        ctx.Document),
                    ctx.Diagnostics[0]);
                return;
            }
            if (node.Kind == EnumProvider.SyntaxKind.Field)
            {
                RegisterFieldFix(ctx, token);
                ctx.RegisterCodeFix(
                    new GuidanceCodeAction(
                        string.Format(AICopAnalyzers.ExtensionMemberMissingSocSuffixGuidanceAction, memberName),
                        nameof(ExtensionObjectSocSuffixFixProvider) + "_GuidanceAdd",
                        ctx.Document),
                    ctx.Diagnostics[0]);
                return;
            }
            node = node.Parent;
        }
    }

    // ── AI0008: procedure ────────────────────────────────────────────────────

    private static void RegisterProcedureFix(CodeFixContext ctx, SyntaxToken nameToken)
    {
        var currentName = nameToken.ValueText ?? string.Empty;
        var newName = currentName + "SOC";

        ctx.RegisterCodeFix(
            new RenameAction(
                string.Format(AICopAnalyzers.ExtensionMemberMissingSocSuffixCodeAction, currentName),
                ct => ApplyProcedureFixAsync(ctx.Document, nameToken, newName, ct),
                nameof(ExtensionObjectSocSuffixFixProvider) + "_AddSocProcedure",
                generateFixAll: true),
            ctx.Diagnostics[0]);
    }

    private static async Task<Document> ApplyProcedureFixAsync(
        Document document, SyntaxToken nameToken, string newName, CancellationToken ct)
    {
        var root = await document.GetSyntaxRootAsync(ct).ConfigureAwait(false);
        if (root is null)
            return document;

        var newRoot = root.ReplaceToken(nameToken, SyntaxFactory.Identifier(newName));
        return document.WithSyntaxRoot(newRoot);
    }

    // ── AI0008: action ───────────────────────────────────────────────────────

    private static void RegisterActionFix(CodeFixContext ctx, SyntaxToken nameToken)
    {
        var currentName = ExtensionObjectSocSuffix.GetTokenDisplayValue(nameToken);
        var isStringLiteral = nameToken.ToString().Trim().StartsWith("\"");
        var newName = isStringLiteral ? currentName + " SOC" : currentName + "SOC";

        ctx.RegisterCodeFix(
            new RenameAction(
                string.Format(AICopAnalyzers.ExtensionMemberMissingSocSuffixCodeAction, currentName),
                ct => ApplyStringTokenFixAsync(ctx.Document, nameToken, newName, ct),
                nameof(ExtensionObjectSocSuffixFixProvider) + "_AddSocAction",
                generateFixAll: true),
            ctx.Diagnostics[0]);
    }

    // ── AI0008: field (+ rename all references in same document) ────────────

    private static void RegisterFieldFix(CodeFixContext ctx, SyntaxToken nameToken)
    {
        var currentName = ExtensionObjectSocSuffix.GetTokenDisplayValue(nameToken);
        if (currentName.Length + " SOC".Length > 30)
            return; // Cannot append without exceeding the 30-character field name limit

        var newName = currentName + " SOC";

        ctx.RegisterCodeFix(
            new RenameAction(
                string.Format(AICopAnalyzers.ExtensionMemberMissingSocSuffixCodeAction, currentName),
                ct => ApplyFieldFixAsync(ctx.Document, currentName, newName, ct),
                nameof(ExtensionObjectSocSuffixFixProvider) + "_AddSocField",
                generateFixAll: true),
            ctx.Diagnostics[0]);
    }

    private static async Task<Document> ApplyFieldFixAsync(
        Document document, string oldName, string newName, CancellationToken ct)
    {
        var root = await document.GetSyntaxRootAsync(ct).ConfigureAwait(false);
        var sourceText = await document.GetTextAsync(ct).ConfigureAwait(false);
        if (root is null)
            return document;

        var changes = new List<TextChange>();
        foreach (var token in root.DescendantTokens())
        {
            var tokenValue = ExtensionObjectSocSuffix.GetTokenDisplayValue(token);
            if (!string.Equals(tokenValue, oldName, StringComparison.OrdinalIgnoreCase))
                continue;

            var rawToken = token.ToString().Trim();
            var newText = rawToken.StartsWith("\"")
                ? $"\"{newName}\""
                : newName.Contains(' ') ? $"\"{newName}\"" : newName;

            changes.Add(new TextChange(token.Span, newText));
        }

        if (changes.Count == 0)
            return document;

        return document.WithText(sourceText.WithChanges(changes.OrderBy(c => c.Span.Start)));
    }

    // ── AI0009: remove SOC from local procedure ──────────────────────────────

    private static void RegisterRemoveSocFix(CodeFixContext ctx, SyntaxToken nameToken)
    {
        var currentName = nameToken.ValueText ?? string.Empty;

        // Strip " SOC" or "SOC" from the end
        string newName;
        if (currentName.EndsWith(" SOC", StringComparison.Ordinal))
            newName = currentName[..^4];
        else if (currentName.EndsWith("SOC", StringComparison.Ordinal))
            newName = currentName[..^3];
        else
            return;

        ctx.RegisterCodeFix(
            new RenameAction(
                string.Format(AICopAnalyzers.LocalProcedureHasSocSuffixCodeAction, currentName),
                ct => ApplyProcedureFixAsync(ctx.Document, nameToken, newName, ct),
                nameof(ExtensionObjectSocSuffixFixProvider) + "_RemoveSocProcedure",
                generateFixAll: true),
            ctx.Diagnostics[0]);
    }

    // ── shared ────────────────────────────────────────────────────────────────

    private static async Task<Document> ApplyStringTokenFixAsync(
        Document document, SyntaxToken nameToken, string newName, CancellationToken ct)
    {
        var sourceText = await document.GetTextAsync(ct).ConfigureAwait(false);

        var rawToken = nameToken.ToString().Trim();
        var newText = rawToken.StartsWith("\"") ? $"\"{newName}\"" : newName;

        var change = new TextChange(nameToken.Span, newText);
        return document.WithText(sourceText.WithChanges(change));
    }
}
