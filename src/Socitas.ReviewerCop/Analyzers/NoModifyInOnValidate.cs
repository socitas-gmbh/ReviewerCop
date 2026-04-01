using System.Collections.Immutable;
using Socitas.ReviewerCop.Common.Extensions;
using Socitas.ReviewerCop.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Semantics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace Socitas.ReviewerCop.Analyzers;

/// <summary>
/// CC0008 – Do not call Modify() inside an OnValidate trigger.
/// The calling code is responsible for persisting the record.
/// </summary>
[DiagnosticAnalyzer]
public sealed class NoModifyInOnValidate : DiagnosticAnalyzer
{
    private const string ModifyMethod = "Modify";
    private const string OnValidateTrigger = "OnValidate";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.NoModifyInOnValidate);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterOperationAction(
            CheckModifyInvocation,
            EnumProvider.OperationKind.InvocationExpression);

    private void CheckModifyInvocation(OperationAnalysisContext ctx)
    {
        if (ctx.IsObsolete())
            return;

        if (ctx.Operation is not IInvocationExpression invocation)
            return;

        if (!string.Equals(invocation.TargetMethod.MethodKind.ToString(), "BuiltInMethod", StringComparison.OrdinalIgnoreCase))
            return;

        if (!string.Equals(invocation.TargetMethod.Name, ModifyMethod, StringComparison.OrdinalIgnoreCase))
            return;

        if (!IsInsideOnValidateTrigger(invocation.Syntax))
            return;

        // Only flag Modify() on the current record (Rec).
        // If the receiver is a different variable it is modifying another table, which is fine.
        if (!IsCalledOnRec(invocation))
            return;

        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.NoModifyInOnValidate,
            invocation.Syntax.GetLocation()));
    }

    /// <summary>
    /// Returns true when Modify() is called on Rec — either implicitly (no receiver)
    /// or explicitly (Rec.Modify()).
    /// </summary>
    private static bool IsCalledOnRec(IInvocationExpression invocation)
    {
        // No receiver → implicit Rec call
        if (invocation.Instance is null)
            return true;

        // Explicit receiver — check if it's "Rec" or "this" (both refer to the current record)
        if (invocation.Instance.Syntax is IdentifierNameSyntax identifier)
        {
            var name = identifier.Identifier.ValueText;
            return string.Equals(name, "Rec", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, "this", StringComparison.OrdinalIgnoreCase);
        }

        // Unknown receiver shape → be conservative and skip
        return false;
    }

    private static bool IsInsideOnValidateTrigger(SyntaxNode? node)
    {
        var current = node?.Parent;
        while (current is not null)
        {
            if (IsSyntaxKind(current, "TriggerDeclaration"))
            {
                // The trigger name is the first IdentifierToken in the declaration
                var nameToken = current.DescendantTokens()
                    .FirstOrDefault(t => IsSyntaxKind(t, "IdentifierToken"));
                return IsSyntaxKind(nameToken, "IdentifierToken") &&
                       string.Equals(nameToken.ValueText, OnValidateTrigger, StringComparison.OrdinalIgnoreCase);
            }
            current = current.Parent;
        }
        return false;
    }

    private static bool IsSyntaxKind(SyntaxNode node, string expectedKindName)
    {
        return string.Equals(node.Kind.ToString(), expectedKindName, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSyntaxKind(SyntaxToken token, string expectedKindName)
    {
        return string.Equals(token.Kind.ToString(), expectedKindName, StringComparison.OrdinalIgnoreCase);
    }
}
