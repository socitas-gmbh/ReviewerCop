using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;

namespace ALCops.CompanyCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class NoGlobalVariables : DiagnosticAnalyzer
{
    // Compiler-injected implicit variables — not declared by the user
    private static readonly ImmutableHashSet<string> ImplicitSystemVariables =
        ImmutableHashSet.Create(StringComparer.OrdinalIgnoreCase,
            "Rec", "xRec", "CurrFieldNo", "CurrPage");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.NoGlobalVariables);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterSymbolAction(
            CheckForGlobalVariables,
            EnumProvider.SymbolKind.Codeunit,
            EnumProvider.SymbolKind.Page,
            EnumProvider.SymbolKind.Report,
            EnumProvider.SymbolKind.XmlPort,
            EnumProvider.SymbolKind.Query,
            EnumProvider.SymbolKind.PageExtension,
            EnumProvider.SymbolKind.TableExtension,
            EnumProvider.SymbolKind.ReportExtension);

    private void CheckForGlobalVariables(SymbolAnalysisContext context)
    {
        if (context.IsObsolete())
            return;

        var applicationObject = (IApplicationObjectTypeSymbol)context.Symbol;

        foreach (var member in applicationObject.GetMembers())
        {
            if (member.Kind != EnumProvider.SymbolKind.GlobalVariable)
                continue;

            if (ImplicitSystemVariables.Contains(member.Name))
                continue;

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.NoGlobalVariables,
                member.GetLocation(),
                member.Name));
        }
    }
}
