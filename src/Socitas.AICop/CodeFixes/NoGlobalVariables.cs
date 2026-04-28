using System.Collections.Immutable;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;

namespace Socitas.AICop.CodeFixes;

[CodeFixProvider(nameof(NoGlobalVariablesGuidanceProvider))]
public sealed class NoGlobalVariablesGuidanceProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticIds.NoGlobalVariables);

    public sealed override FixAllProvider? GetFixAllProvider() => null;

    public override async Task RegisterCodeFixesAsync(CodeFixContext ctx)
    {
        var root = await ctx.Document.GetSyntaxRootAsync(ctx.CancellationToken).ConfigureAwait(false);
        if (root is null)
            return;

        var varName = root.FindNode(ctx.Span).ToString().Trim('"');

        ctx.RegisterCodeFix(
            new GuidanceCodeAction(
                string.Format(AICopAnalyzers.NoGlobalVariablesGuidanceAction, varName),
                nameof(NoGlobalVariablesGuidanceProvider),
                ctx.Document),
            ctx.Diagnostics[0]);
    }
}
