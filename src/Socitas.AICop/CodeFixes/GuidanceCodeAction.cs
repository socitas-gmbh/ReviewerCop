using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;

namespace Socitas.AICop.CodeFixes;

internal sealed class GuidanceCodeAction : CodeAction.DocumentChangeAction
{
    public override CodeActionKind Kind => CodeActionKind.QuickFix;
    public override bool SupportsFixAll => false;
    public override string? FixAllSingleInstanceTitle => string.Empty;
    public override string? FixAllTitle => Title;

    public GuidanceCodeAction(string title, string equivalenceKey, Document document)
        : base(title, _ => Task.FromResult(document), equivalenceKey) { }
}
