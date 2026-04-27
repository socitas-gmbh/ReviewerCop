using System.Collections.Concurrent;
using System.Collections.Immutable;
using Socitas.ReviewerCop.Common.Extensions;
using Socitas.ReviewerCop.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace Socitas.AICop.Analyzers;

/// <summary>
/// CC0014 – RestClient.Initialize() must pass a handler codeunit defined in the current app.
/// Using a custom handler ensures HTTP client configuration is centralized and consistent.
/// </summary>
[DiagnosticAnalyzer]
public sealed class InitializeRestClientWithHandler : DiagnosticAnalyzer
{
    private static readonly ConcurrentDictionary<Compilation, ImmutableHashSet<string>> LocalCodeunitCache = new();

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

        var localCodeunitNames = LocalCodeunitCache.GetOrAdd(ctx.Compilation, CollectLocalCodeunitNames);

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

            // Direct codeunit argument — check it is local.
            if (argType.NavTypeKind == EnumProvider.NavTypeKind.Codeunit)
            {
                if (!string.IsNullOrEmpty(argType.Name) && localCodeunitNames.Contains(argType.Name))
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
                    && !string.IsNullOrEmpty(underlyingType.Name)
                    && localCodeunitNames.Contains(underlyingType.Name))
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

    private static ImmutableHashSet<string> CollectLocalCodeunitNames(Compilation compilation)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var tree in compilation.SyntaxTrees)
        {
            foreach (var node in tree.GetRoot().DescendantNodes())
            {
                if (node.Kind != EnumProvider.SyntaxKind.CodeunitObject)
                    continue;

                var name = ExtractObjectName(node);
                if (!string.IsNullOrEmpty(name))
                    names.Add(name!);
            }
        }

        return names.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static string? ExtractObjectName(SyntaxNode objectNode)
    {
        // Codeunit declaration: codeunit <id> <name> { ... }
        // DescendantTokens is lazy — we break after 3–4 tokens so we never walk the body.
        bool passedId = false;
        foreach (var token in objectNode.DescendantTokens())
        {
            if (token.IsKind(EnumProvider.SyntaxKind.Int32LiteralToken))
            {
                passedId = true;
                continue;
            }

            if (!passedId)
                continue;

            if (token.IsKind(EnumProvider.SyntaxKind.IdentifierToken) ||
                string.Equals(token.Kind.ToString(), "StringLiteralToken", StringComparison.OrdinalIgnoreCase))
            {
                return token.ValueText;
            }

            // Hit a brace or unexpected token — name is missing, bail out.
            break;
        }

        return null;
    }
}
