using System.Collections.Immutable;
using Socitas.ReviewerCop.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace Socitas.ReviewerCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class NoTodoComments : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.NoTodoComments);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(
            CheckForTodoComments,
            EnumProvider.SyntaxKind.CodeunitObject,
            EnumProvider.SyntaxKind.TableObject,
            EnumProvider.SyntaxKind.PageObject,
            EnumProvider.SyntaxKind.ReportObject,
            EnumProvider.SyntaxKind.XmlPortObject,
            EnumProvider.SyntaxKind.QueryObject,
            EnumProvider.SyntaxKind.EnumType,
            EnumProvider.SyntaxKind.Interface,
            EnumProvider.SyntaxKind.TableExtensionObject,
            EnumProvider.SyntaxKind.PageExtensionObject,
            EnumProvider.SyntaxKind.EnumExtensionType,
            EnumProvider.SyntaxKind.ReportExtensionObject);

    private void CheckForTodoComments(SyntaxNodeAnalysisContext ctx)
    {
        foreach (var token in ctx.Node.DescendantTokens())
        {
            CheckTriviaList(ctx, token.LeadingTrivia);
            CheckTriviaList(ctx, token.TrailingTrivia);
        }
    }

    private static void CheckTriviaList(SyntaxNodeAnalysisContext ctx, SyntaxTriviaList triviaList)
    {
        foreach (var trivia in triviaList)
        {
            if (!trivia.IsKind(EnumProvider.SyntaxKind.LineCommentTrivia))
                continue;

            var text = trivia.ToFullString();
            if (text.IndexOf("TODO", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.NoTodoComments,
                    trivia.GetLocation()));
            }
        }
    }
}
