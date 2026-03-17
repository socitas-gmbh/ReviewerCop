using System.Collections.Immutable;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;

namespace ALCops.CompanyCop.CodeFixes;

/// <summary>
/// CC0005 – Quick fix: append the subscribed event name to the procedure name so it
/// follows the FunctionName+EventName convention.
/// </summary>
[CodeFixProvider(nameof(EventSubscriberNamingConventionFixProvider))]
public sealed class EventSubscriberNamingConventionFixProvider : CodeFixProvider
{
    private const string EventSubscriberAttributeName = "EventSubscriber";
    private const int EventNameArgIndex = 2;

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
        ImmutableArray.Create(DiagnosticIds.EventSubscriberNamingConvention);

    public sealed override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext ctx)
    {
        var syntaxRoot = await ctx.Document.GetSyntaxRootAsync(ctx.CancellationToken).ConfigureAwait(false);
        if (syntaxRoot is null)
            return;

        var token = syntaxRoot.FindToken(ctx.Span.Start);
        var method = token.Parent?.FirstAncestorOrSelf<MethodDeclarationSyntax>();
        if (method is null)
            return;

        var eventName = GetEventName(method);
        if (string.IsNullOrEmpty(eventName))
            return;

        var currentName = method.Name.Identifier.ValueText;
        var newName = currentName + eventName;

        ctx.RegisterCodeFix(
            new RenameAction(
                string.Format(CompanyCopAnalyzers.EventSubscriberNamingConventionCodeAction, newName),
                ct => RenameMethod(ctx.Document, method, newName, ct),
                nameof(EventSubscriberNamingConventionFixProvider),
                generateFixAll: true),
            ctx.Diagnostics[0]);
    }

    private static string? GetEventName(MethodDeclarationSyntax method)
    {
        var attribute = method.Attributes.FirstOrDefault(attr =>
        {
            var name = attr.GetIdentifierOrLiteralValue();
            return name is not null && SemanticFacts.IsSameName(name, EventSubscriberAttributeName);
        });

        var argList = attribute?.ArgumentList;
        if (argList is null || argList.Arguments.Count <= EventNameArgIndex)
            return null;

        return argList.Arguments[EventNameArgIndex].GetIdentifierOrLiteralValue();
    }

    private static async Task<Document> RenameMethod(
        Document document, MethodDeclarationSyntax method, string newName, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
            return document;

        var newRoot = root.ReplaceToken(
            method.Name.Identifier,
            SyntaxFactory.Identifier(newName));

        return document.WithSyntaxRoot(newRoot);
    }
}
