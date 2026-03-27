using System.Collections.Immutable;
using Socitas.ReviewerCop.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;

namespace Socitas.ReviewerCop.CodeFixes;

/// <summary>
/// CC0003 – Quick fix: replace a direct field assignment with a Validate() call.
/// Transforms <c>Rec.Field := Value;</c> into <c>Rec.Validate(Field, Value);</c>.
/// </summary>
[CodeFixProvider(nameof(ValidateFieldAssignmentsFixProvider))]
public sealed class ValidateFieldAssignmentsFixProvider : CodeFixProvider
{
    private const string ValidateMethodName = "Validate";

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

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticIds.ValidateFieldAssignments);

    public sealed override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext ctx)
    {
        var syntaxRoot = await ctx.Document.GetSyntaxRootAsync(ctx.CancellationToken).ConfigureAwait(false);
        if (syntaxRoot is null)
            return;

        var node = syntaxRoot.FindNode(ctx.Span);
        var assignment = node.FirstAncestorOrSelf<AssignmentStatementSyntax>();
        if (assignment is null)
            return;

        ctx.RegisterCodeFix(
            new ReplaceAction(
                ReviewerCopAnalyzers.ValidateFieldAssignmentsCodeAction,
                ct => ConvertToValidateAsync(ctx.Document, assignment, ct),
                nameof(ValidateFieldAssignmentsFixProvider),
                generateFixAll: true),
            ctx.Diagnostics[0]);
    }

    private static async Task<Document> ConvertToValidateAsync(
        Document document, AssignmentStatementSyntax assignment, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
            return document;

        // The target must be a member access (e.g. Rec.Name)
        if (assignment.Target is not MemberAccessExpressionSyntax targetAccess)
            return document;

        var receiver = targetAccess.Expression;  // Rec
        var fieldName = targetAccess.Name;        // Name (IdentifierNameSyntax)
        var value = assignment.Source;            // the RHS value

        // Build: Rec.Validate
        var validateAccess = SyntaxFactory.MemberAccessExpression(
            receiver.WithoutTrivia(),
            SyntaxFactory.Token(EnumProvider.SyntaxKind.DotToken),
            SyntaxFactory.IdentifierName(ValidateMethodName));

        // Build argument list: (Field, Value)
        var args = new SeparatedSyntaxList<CodeExpressionSyntax>()
            .Add(fieldName.WithoutTrivia())
            .Add(value.WithoutTrivia());

        var invocation = SyntaxFactory.InvocationExpression(
            validateAccess,
            SyntaxFactory.ArgumentList(args));

        // Wrap in an expression statement, preserving semicolon and trivia
        var newStatement = SyntaxFactory.ExpressionStatement(invocation, assignment.SemicolonToken)
            .WithLeadingTrivia(assignment.GetLeadingTrivia())
            .WithTrailingTrivia(assignment.GetTrailingTrivia());

        var newRoot = root.ReplaceNode(assignment, newStatement);
        return document.WithSyntaxRoot(newRoot);
    }
}
