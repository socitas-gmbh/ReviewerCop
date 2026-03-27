using System.Collections.Immutable;
using Socitas.ReviewerCop.Common.Extensions;
using Socitas.ReviewerCop.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

namespace Socitas.ReviewerCop.Analyzers;

/// <summary>
/// CC0004 – Variable names must not start with lrec/grec or embed AL type names.
/// </summary>
[DiagnosticAnalyzer]
public sealed class NoTypeOrPrefixInVariableName : DiagnosticAnalyzer
{
    private static readonly string[] ForbiddenPrefixes =
    [
        "lrec",
        "grec",
        "grec",
        "lvar",
        "gvar",
    ];

    private static readonly string[] ForbiddenTypeSubstrings =
    [
        "integer",
        "boolean",
        "decimal",
        "datetime",
        "date",
        "time",
        "text",
        "code",
        "record",
        "codeunit",
        "page",
        "report",
        "xmlport",
        "query",
        "option",
        "enum",
        "list",
        "dictionary",
        "array",
    ];

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.NoTypeOrPrefixInVariableName);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterSymbolAction(
            CheckVariableName,
            EnumProvider.SymbolKind.LocalVariable,
            EnumProvider.SymbolKind.GlobalVariable);

    private void CheckVariableName(SymbolAnalysisContext context)
    {
        if (context.IsObsolete())
            return;

        var name = context.Symbol.Name;
        if (string.IsNullOrEmpty(name))
            return;

        var nameLower = name.ToLowerInvariant();

        // Check forbidden prefixes
        foreach (var prefix in ForbiddenPrefixes)
        {
            if (nameLower.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.NoTypeOrPrefixInVariableName,
                    context.Symbol.GetLocation(),
                    name));
                return;
            }
        }

        // Check if the name IS exactly a type keyword (case-insensitive)
        // e.g. variable named "Integer", "Boolean", "Text"
        foreach (var typeName in ForbiddenTypeSubstrings)
        {
            if (nameLower == typeName)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.NoTypeOrPrefixInVariableName,
                    context.Symbol.GetLocation(),
                    name));
                return;
            }
        }
    }
}
