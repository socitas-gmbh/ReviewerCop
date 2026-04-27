using System.Collections.Immutable;
using Socitas.ReviewerCop.Common.Extensions;
using Socitas.ReviewerCop.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace Socitas.AICop.Analyzers;

/// <summary>
/// CC0013 – Use the "Rest Client" codeunit instead of raw HttpClient types.
/// Flags variables declared as HttpClient, HttpRequestMessage, HttpResponseMessage,
/// HttpContent, or HttpHeaders — the "Rest Client" codeunit wraps all of these.
/// </summary>
[DiagnosticAnalyzer]
public sealed class UseRestClient : DiagnosticAnalyzer
{
    private static readonly ImmutableHashSet<string> ForbiddenHttpTypeNames =
        ImmutableHashSet.Create(StringComparer.OrdinalIgnoreCase,
            "HttpClient", "HttpRequestMessage", "HttpResponseMessage",
            "HttpContent", "HttpHeaders");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.UseRestClient);

    public override void Initialize(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(
            CheckVarSection,
            EnumProvider.SyntaxKind.VarSection,
            EnumProvider.SyntaxKind.GlobalVarSection);
    }

    private void CheckVarSection(SyntaxNodeAnalysisContext ctx)
    {
        if (ctx.IsObsolete())
            return;

        foreach (var token in ctx.Node.DescendantTokens())
        {
            if (!token.IsKind(EnumProvider.SyntaxKind.IdentifierToken))
                continue;

            if (string.IsNullOrEmpty(token.ValueText) || !ForbiddenHttpTypeNames.Contains(token.ValueText))
                continue;

            // Verify this identifier follows a colon (i.e. it is a type name, not a variable name).
            var prev = token.GetPreviousToken();
            if (!prev.IsKind(EnumProvider.SyntaxKind.ColonToken))
                continue;

            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.UseRestClient,
                token.GetLocation(),
                token.ValueText));
        }
    }
}
