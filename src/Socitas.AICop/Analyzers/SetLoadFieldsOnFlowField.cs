using System.Collections.Immutable;
using Socitas.ReviewerCop.Common.Extensions;
using Socitas.ReviewerCop.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Semantics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;

namespace Socitas.AICop.Analyzers;

/// <summary>
/// AI0012 – FlowFields must not be passed to SetLoadFields/AddLoadFields.
/// Reports every FlowField argument; the value has to be calculated with SetAutoCalcFields
/// before or CalcFields after retrieving the record instead.
/// </summary>
[DiagnosticAnalyzer]
public sealed class SetLoadFieldsOnFlowField : DiagnosticAnalyzer
{
    private static readonly ImmutableHashSet<string> LoadFieldsMethods =
        ImmutableHashSet.Create(StringComparer.OrdinalIgnoreCase, "SetLoadFields", "AddLoadFields");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.SetLoadFieldsOnFlowField);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterOperationAction(
            AnalyzeLoadFieldsInvocation,
            EnumProvider.OperationKind.InvocationExpression);

    private void AnalyzeLoadFieldsInvocation(OperationAnalysisContext ctx)
    {
        if (ctx.IsObsolete())
            return;

        if (ctx.Operation is not IInvocationExpression invocation)
            return;

        if (!string.Equals(invocation.TargetMethod.MethodKind.ToString(), "BuiltInMethod", StringComparison.OrdinalIgnoreCase))
            return;

        if (!LoadFieldsMethods.Contains(invocation.TargetMethod.Name))
            return;

        foreach (var argument in invocation.Arguments)
        {
            var fieldSymbol = GetFieldSymbol(argument.Value);
            if (fieldSymbol is null || fieldSymbol.FieldClass != EnumProvider.FieldClassKind.FlowField)
                continue;

            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.SetLoadFieldsOnFlowField,
                argument.Syntax.GetLocation(),
                fieldSymbol.Name,
                invocation.TargetMethod.Name));
        }
    }

    private static IFieldSymbol? GetFieldSymbol(IOperation? operation) =>
        operation switch
        {
            IFieldAccess fieldAccess => fieldAccess.FieldSymbol,
            IConversionExpression conversion => GetFieldSymbol(conversion.Operand),
            _ => null
        };
}
