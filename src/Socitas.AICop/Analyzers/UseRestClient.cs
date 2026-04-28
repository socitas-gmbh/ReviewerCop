using System.Collections.Immutable;
using Socitas.ReviewerCop.Common.Extensions;
using Socitas.ReviewerCop.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace Socitas.AICop.Analyzers;

/// <summary>
/// AI0004 – Use the "Rest Client" codeunit instead of raw HttpClient types.
/// Flags variables declared as HttpClient, HttpRequestMessage, HttpResponseMessage,
/// HttpContent, or HttpHeaders — the "Rest Client" codeunit wraps all of these.
/// Exception: codeunits that implement "Http Client Handler" must use HttpClient directly.
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

        // Codeunits that implement "Http Client Handler" must use HttpClient directly.
        if (IsInsideHttpClientHandlerImpl(ctx.Node))
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

    /// <summary>
    /// Returns true when the given var section node is inside a codeunit that implements
    /// "Http Client Handler". Those codeunits must use HttpClient directly in their Send method.
    /// </summary>
    private static bool IsInsideHttpClientHandlerImpl(SyntaxNode varSectionNode)
    {
        var node = varSectionNode.Parent;
        while (node is not null)
        {
            if (node.Kind == EnumProvider.SyntaxKind.CodeunitObject)
            {
                // Scan the declaration header (tokens before the opening brace) for
                // the "Http Client Handler" interface name in the implements clause.
                foreach (var token in node.DescendantTokens())
                {
                    if (token.IsKind(EnumProvider.SyntaxKind.OpenBraceToken))
                        break;

                    var valueText = token.ValueText?.Trim('"');
                    if (string.Equals(valueText, "Http Client Handler", StringComparison.OrdinalIgnoreCase))
                        return true;
                }
                return false;
            }

            node = node.Parent;
        }

        return false;
    }
}
