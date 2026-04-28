using System.Collections.Immutable;
using Socitas.ReviewerCop.Common.Extensions;
using Socitas.ReviewerCop.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace Socitas.AICop.Analyzers;

/// <summary>
/// AI0005 – RestClient.Initialize() must pass a handler codeunit defined in the current app.
/// Using a custom handler ensures HTTP client configuration is centralized and consistent.
/// </summary>
[DiagnosticAnalyzer]
public sealed class InitializeRestClientWithHandler : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.InitializeRestClientWithHandler);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterOperationAction(
            CheckInitializeCall,
            EnumProvider.OperationKind.InvocationExpression);

    private static void CheckInitializeCall(OperationAnalysisContext ctx)
    {
        if (ctx.IsObsolete())
            return;

        if (ctx.Operation is not IInvocationExpression invocation)
            return;

        if (!string.Equals(invocation.TargetMethod.Name, "Initialize", StringComparison.OrdinalIgnoreCase))
            return;

        // Must be called on a codeunit named "Rest Client"
        var receiverType = invocation.Instance?.Type;
        if (receiverType is null || receiverType.NavTypeKind != EnumProvider.NavTypeKind.Codeunit)
            return;
        if (!string.Equals(receiverType.Name, "Rest Client", StringComparison.OrdinalIgnoreCase))
            return;

        // Check if any argument is a codeunit from the current app.
        // When a codeunit is passed to an Interface parameter, the resolved argument
        // type may be the interface itself (NavTypeKind.Interface) rather than the
        // concrete codeunit.  In that case, unwrap the conversion (if present) to
        // recover the underlying codeunit type.
        foreach (var arg in invocation.Arguments)
        {
            var argType = arg.GetTypeSymbol() ?? arg.Value?.Type;
            if (argType is null)
                continue;

            // Direct codeunit argument — check it is defined in the current project's source.
            if (argType.NavTypeKind == EnumProvider.NavTypeKind.Codeunit)
            {
                if (argType.IsDefinedInSource())
                    return;
                continue;
            }

            // Interface argument — the underlying variable may still be a local
            // codeunit that implements the interface.  Try the conversion operand
            // first; fall back to the value's operand type if available.
            if (argType.NavTypeKind == EnumProvider.NavTypeKind.Interface)
            {
                ITypeSymbol? underlyingType = null;
                if (arg.Value is IConversionExpression conv)
                    underlyingType = conv.Operand.Type;

                if (underlyingType is not null
                    && underlyingType.NavTypeKind == EnumProvider.NavTypeKind.Codeunit
                    && underlyingType.IsDefinedInSource())
                    return;

                // If we cannot unwrap to a concrete codeunit, the caller is still
                // explicitly passing a handler implementation — honour the intent.
                return;
            }
        }

        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.InitializeRestClientWithHandler,
            invocation.Syntax.GetLocation()));
    }
}
