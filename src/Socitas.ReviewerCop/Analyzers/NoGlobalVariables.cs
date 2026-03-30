using System.Collections.Immutable;
using Socitas.ReviewerCop.Common.Extensions;
using Socitas.ReviewerCop.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace Socitas.ReviewerCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class NoGlobalVariables : DiagnosticAnalyzer
{
    // Compiler-injected implicit variables — not declared by the user
    private static readonly ImmutableHashSet<string> ImplicitSystemVariables =
        ImmutableHashSet.Create(StringComparer.OrdinalIgnoreCase,
            "Rec", "xRec", "CurrFieldNo", "CurrPage",
            "CurrReport", "CurrXMLPort", "CurrQuery");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.NoGlobalVariables);

    public override void Initialize(AnalysisContext context)
    {
        // All object types are handled via syntax node actions so we can inspect
        // how global variables are used: field/column source expressions and
        // cross-scope (state-dependent) references are exempted.
        context.RegisterSyntaxNodeAction(
            CheckGlobalVariables,
            EnumProvider.SyntaxKind.CodeunitObject,
            EnumProvider.SyntaxKind.PageObject,
            EnumProvider.SyntaxKind.PageExtensionObject,
            EnumProvider.SyntaxKind.ReportObject,
            EnumProvider.SyntaxKind.ReportExtensionObject,
            EnumProvider.SyntaxKind.XmlPortObject,
            EnumProvider.SyntaxKind.QueryObject,
            EnumProvider.SyntaxKind.TableExtensionObject);
    }

    private void CheckGlobalVariables(SyntaxNodeAnalysisContext ctx)
    {
        if (ctx.IsObsolete())
            return;

        if (ctx.SemanticModel.GetDeclaredSymbol(ctx.Node) is not IApplicationObjectTypeSymbol appObject)
            return;

        // SingleInstance codeunits use global variables to hold state across calls by design
        if (IsSingleInstanceCodeunit(ctx.Node))
            return;

        var exemptNames = CollectExemptVariableNames(ctx.Node);

        foreach (var member in appObject.GetMembers())
        {
            if (member.Kind != EnumProvider.SymbolKind.GlobalVariable)
                continue;

            if (ImplicitSystemVariables.Contains(member.Name))
                continue;

            if (exemptNames.Contains(member.Name))
                continue;

            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.NoGlobalVariables,
                member.GetLocation(),
                member.Name));
        }
    }

    /// <summary>
    /// Returns true if the object node is a codeunit with SingleInstance = true.
    /// </summary>
    private static bool IsSingleInstanceCodeunit(SyntaxNode objectNode)
    {
        if (objectNode.Kind != EnumProvider.SyntaxKind.CodeunitObject)
            return false;

        foreach (var token in objectNode.DescendantTokens())
        {
            if (!string.Equals(token.Kind.ToString(), "IdentifierToken", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!string.Equals(token.ValueText, "SingleInstance", StringComparison.OrdinalIgnoreCase))
                continue;

            var next = token.GetNextToken();
            if (!string.Equals(next.Kind.ToString(), "EqualsToken", StringComparison.OrdinalIgnoreCase))
                continue;

            var valueToken = next.GetNextToken();
            return valueToken.IsKind(EnumProvider.SyntaxKind.TrueKeyword);
        }

        return false;
    }

    /// <summary>
    /// Collects variable names that are exempt from CC0002 because they must be global:
    /// 1. Variables used as field/column source expressions (pages/reports)
    /// 2. Variables used as property value expressions on controls/actions (Enabled, Visible, etc.)
    /// 3. Variables referenced in 2+ distinct method/trigger scopes (state-dependent)
    /// </summary>
    private static ImmutableHashSet<string> CollectExemptVariableNames(SyntaxNode objectNode)
    {
        var builder = ImmutableHashSet.CreateBuilder<string>(StringComparer.OrdinalIgnoreCase);

        // Exempt variables used as page field or report column source expressions
        CollectFieldSourceNames(objectNode, "PageField", builder);
        CollectFieldSourceNames(objectNode, "ReportColumn", builder);

        // Exempt variables used as property value expressions (e.g. Enabled = MyVar)
        CollectPropertyValueNames(objectNode, builder);

        // Exempt variables that are referenced in 2+ method/trigger scopes (state-dependent).
        // A variable used across multiple scopes cannot be local to any single one.
        CollectMultiScopeVariables(objectNode, builder);

        return builder.ToImmutable();
    }

    /// <summary>
    /// Adds identifier names from source expressions of field/column declarations.
    /// Both report columns and page fields use: name ( FieldName ; SourceExpression ) { ... }
    /// </summary>
    private static void CollectFieldSourceNames(
        SyntaxNode rootNode, string fieldKindName, ImmutableHashSet<string>.Builder builder)
    {
        foreach (var node in rootNode.DescendantNodes())
        {
            if (!string.Equals(node.Kind.ToString(), fieldKindName, StringComparison.OrdinalIgnoreCase))
                continue;

            bool afterSemicolon = false;
            foreach (var token in node.DescendantTokens())
            {
                if (!afterSemicolon)
                {
                    if (string.Equals(token.Kind.ToString(), "SemicolonToken", StringComparison.OrdinalIgnoreCase))
                        afterSemicolon = true;
                    continue;
                }

                if (string.Equals(token.Kind.ToString(), "IdentifierToken", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrEmpty(token.ValueText))
                {
                    builder.Add(token.ValueText);
                }
            }
        }
    }

    /// <summary>
    /// Collects identifier names used as property value expressions on controls/actions.
    /// Properties like Enabled = MyVar, Visible = MyVar require the variable to be global
    /// because they are evaluated by the page runtime, not inside a procedure.
    /// Scans for Property nodes whose value is a bare identifier (not a keyword or literal).
    /// </summary>
    private static void CollectPropertyValueNames(
        SyntaxNode objectNode, ImmutableHashSet<string>.Builder builder)
    {
        foreach (var node in objectNode.DescendantNodes())
        {
            if (!string.Equals(node.Kind.ToString(), "Property", StringComparison.OrdinalIgnoreCase))
                continue;

            // Skip properties inside method/trigger bodies
            if (IsInsideMethodOrTrigger(node))
                continue;

            // Look for: PropertyName = IdentifierToken ;
            bool afterEquals = false;
            foreach (var token in node.DescendantTokens())
            {
                if (!afterEquals)
                {
                    if (string.Equals(token.Kind.ToString(), "EqualsToken", StringComparison.OrdinalIgnoreCase))
                        afterEquals = true;
                    continue;
                }

                if (string.Equals(token.Kind.ToString(), "IdentifierToken", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrEmpty(token.ValueText))
                {
                    builder.Add(token.ValueText);
                }

                break; // Only check the first token after '='
            }
        }
    }

    private static bool IsInsideMethodOrTrigger(SyntaxNode node)
    {
        var parent = node.Parent;
        while (parent is not null)
        {
            if (parent.Kind == EnumProvider.SyntaxKind.MethodDeclaration ||
                parent.Kind == EnumProvider.SyntaxKind.TriggerDeclaration)
                return true;
            parent = parent.Parent;
        }
        return false;
    }

    /// <summary>
    /// Finds variables referenced in 2+ distinct method/trigger bodies and adds them to the builder.
    /// A variable used across multiple scopes is state-dependent and must remain global.
    /// </summary>
    private static void CollectMultiScopeVariables(
        SyntaxNode objectNode, ImmutableHashSet<string>.Builder builder)
    {
        // Collect method/trigger body nodes
        var scopeNodes = new List<SyntaxNode>();
        foreach (var node in objectNode.DescendantNodes())
        {
            if (node.Kind == EnumProvider.SyntaxKind.MethodDeclaration ||
                node.Kind == EnumProvider.SyntaxKind.TriggerDeclaration)
            {
                scopeNodes.Add(node);
            }
        }

        if (scopeNodes.Count < 2)
            return; // With 0-1 scopes, no variable can be multi-scope

        // For each scope, collect the set of identifier names used within it
        var identifiersByScope = new List<HashSet<string>>(scopeNodes.Count);
        foreach (var scope in scopeNodes)
        {
            var identifiers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var token in scope.DescendantTokens())
            {
                if (string.Equals(token.Kind.ToString(), "IdentifierToken", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrEmpty(token.ValueText))
                {
                    identifiers.Add(token.ValueText);
                }
            }
            identifiersByScope.Add(identifiers);
        }

        // Count how many scopes reference each identifier.
        // Only track identifiers seen in the first scope to avoid tracking every token.
        var scopeCount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var identifiers in identifiersByScope)
        {
            foreach (var name in identifiers)
            {
                if (!scopeCount.TryGetValue(name, out int count))
                    scopeCount[name] = 1;
                else
                    scopeCount[name] = count + 1;
            }
        }

        foreach (var (name, count) in scopeCount)
        {
            if (count >= 2)
                builder.Add(name);
        }
    }
}
