using System.Collections.Immutable;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;

namespace Socitas.AICop.CodeFixes;

[CodeFixProvider(nameof(UseRestClientGuidanceProvider))]
public sealed class UseRestClientGuidanceProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticIds.UseRestClient);

    public sealed override FixAllProvider? GetFixAllProvider() => null;

    public override async Task RegisterCodeFixesAsync(CodeFixContext ctx)
    {
        var root = await ctx.Document.GetSyntaxRootAsync(ctx.CancellationToken).ConfigureAwait(false);
        if (root is null)
            return;

        ctx.RegisterCodeFix(
            new GuidanceCodeAction(
                AICopAnalyzers.UseRestClientGuidanceAction,
                nameof(UseRestClientGuidanceProvider),
                ctx.Document),
            ctx.Diagnostics[0]);
    }
}
