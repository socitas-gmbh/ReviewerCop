using System.Collections.Immutable;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;

namespace Socitas.ReviewerCop.CodeFixes;

/// <summary>
/// CC0009 – Quick fix: remove the redundant field-level DataClassification property.
/// </summary>
[CodeFixProvider(nameof(DataClassificationOnTableFixProvider))]
public sealed class DataClassificationOnTableFixProvider : CodeFixProvider
{
    private const string DataClassificationPropertyName = "DataClassification";

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
        ImmutableArray.Create(DiagnosticIds.DataClassificationOnTable);

    public sealed override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext ctx)
    {
        var syntaxRoot = await ctx.Document.GetSyntaxRootAsync(ctx.CancellationToken).ConfigureAwait(false);
        if (syntaxRoot is null)
            return;

        // The diagnostic is on the DataClassification identifier token; navigate up to the PropertySyntax
        var token = syntaxRoot.FindToken(ctx.Span.Start);
        var propertySyntax = token.Parent?.FirstAncestorOrSelf<PropertySyntax>();
        if (propertySyntax is null)
            return;

        ctx.RegisterCodeFix(
            new RemoveAction(
                ReviewerCopAnalyzers.DataClassificationOnTableCodeAction,
                ct => RemoveFieldDataClassificationAsync(ctx.Document, propertySyntax, ct),
                nameof(DataClassificationOnTableFixProvider),
                generateFixAll: true),
            ctx.Diagnostics[0]);
    }

    private static async Task<Document> RemoveFieldDataClassificationAsync(
        Document document, PropertySyntax propertySyntax, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
            return document;

        if (propertySyntax.Parent is not PropertyListSyntax propertyList)
            return document;

        var dataClassificationProperty = propertyList.GetProperty(DataClassificationPropertyName);
        if (dataClassificationProperty is null)
            return document;

        var newProperties = propertyList.Properties.Remove(dataClassificationProperty);
        var newPropertyList = propertyList.WithProperties(newProperties);
        var newRoot = root.ReplaceNode(propertyList, newPropertyList);
        return document.WithSyntaxRoot(newRoot);
    }
}
