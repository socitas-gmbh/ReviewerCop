using System.Collections.Immutable;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;

namespace Socitas.AICop.CodeFixes;

[CodeFixProvider(nameof(SetLoadFieldsOnFlowFieldGuidanceProvider))]
public sealed class SetLoadFieldsOnFlowFieldGuidanceProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticIds.SetLoadFieldsOnFlowField);

    public sealed override FixAllProvider? GetFixAllProvider() => null;

    public override async Task RegisterCodeFixesAsync(CodeFixContext ctx)
    {
        var root = await ctx.Document.GetSyntaxRootAsync(ctx.CancellationToken).ConfigureAwait(false);
        if (root is null)
            return;

        ctx.RegisterCodeFix(
            new GuidanceCodeAction(
                AICopAnalyzers.SetLoadFieldsOnFlowFieldGuidanceAction,
                nameof(SetLoadFieldsOnFlowFieldGuidanceProvider),
                ctx.Document),
            ctx.Diagnostics[0]);
    }
}
