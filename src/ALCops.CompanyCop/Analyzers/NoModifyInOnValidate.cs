using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Semantics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.CompanyCop.Analyzers;

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

        if (invocation.TargetMethod.MethodKind != EnumProvider.MethodKind.BuiltInMethod)
            return;

        if (!string.Equals(invocation.TargetMethod.Name, ModifyMethod, StringComparison.OrdinalIgnoreCase))
            return;

        if (!IsInsideOnValidateTrigger(invocation.Syntax))
            return;

        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.NoModifyInOnValidate,
            invocation.Syntax.GetLocation()));
    }

    private static bool IsInsideOnValidateTrigger(SyntaxNode? node)
    {
        var current = node?.Parent;
        while (current is not null)
        {
            if (current.Kind == SyntaxKind.TriggerDeclaration)
            {
                // The trigger name is the first IdentifierToken in the declaration
                var nameToken = current.DescendantTokens()
                    .FirstOrDefault(t => t.Kind == SyntaxKind.IdentifierToken);
                return nameToken.Kind == SyntaxKind.IdentifierToken &&
                       string.Equals(nameToken.ValueText, OnValidateTrigger, StringComparison.OrdinalIgnoreCase);
            }
            current = current.Parent;
        }
        return false;
    }
}
