using System.Collections.Immutable;
using System.Reflection;
using Socitas.ReviewerCop.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace Socitas.ReviewerCop.Analyzers;

/// <summary>
/// CC0011 – Caption and ToolTip should be defined on the table field, not on the page field.
/// Reports when a page or page extension field control has a Caption or ToolTip property
/// but the underlying table field does not, indicating the property should be moved to the table.
/// </summary>
[DiagnosticAnalyzer]
public sealed class CaptionTooltipOnPage : DiagnosticAnalyzer
{
    private static readonly ImmutableHashSet<string> TargetProperties =
        ImmutableHashSet.Create(StringComparer.OrdinalIgnoreCase, "Caption", "ToolTip");

    // Reflection helpers for accessing symbol names from BC's concrete field symbol types
    private static readonly string[] _containingPropNames =
        ["ContainingObject", "ContainingType", "ContainingSymbol"];

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.CaptionTooltipOnPage);

    public override void Initialize(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(CheckPageFields, EnumProvider.SyntaxKind.PageObject);
        context.RegisterSyntaxNodeAction(CheckPageFields, EnumProvider.SyntaxKind.PageExtensionObject);
    }

    private static void CheckPageFields(SyntaxNodeAnalysisContext ctx)
    {
        var pageNode = ctx.Node;

        foreach (var token in pageNode.DescendantTokens())
        {
            if (token.Kind != SyntaxKind.IdentifierToken)
                continue;

            var tokenText = token.ValueText;
            if (tokenText is null || !TargetProperties.Contains(tokenText))
                continue;

            // Must be a property assignment (identifier followed by '=')
            var next = token.GetNextToken();
            if (next.Kind != SyntaxKind.EqualsToken)
                continue;

            // Must be inside a PageField node
            var pageFieldNode = GetContainingPageFieldNode(token);
            if (pageFieldNode is null)
                continue;

            // Skip when there is no related table field (global var / expression), or when the
            // table field already has the property (redundant on page, not missing from table).
            if (ShouldSkip(ctx, pageFieldNode, tokenText))
                continue;

            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.CaptionTooltipOnPage,
                token.GetLocation(),
                tokenText));
        }
    }

    internal static SyntaxNode? GetContainingPageFieldNode(SyntaxToken token)
    {
        var node = token.Parent;
        while (node is not null)
        {
            // Explicit fast-path for known page field kind
            if (node.Kind == EnumProvider.SyntaxKind.PageField)
                return node;

            // Stop at page boundaries
            if (node.Kind == EnumProvider.SyntaxKind.PageObject ||
                node.Kind == EnumProvider.SyntaxKind.PageExtensionObject)
                return null;

            // Generic path: the parent of a PropertyListSyntax is the field/control node.
            // This handles page extension modify(...) blocks whose SyntaxKind differs from PageField.
            if (node is PropertyListSyntax)
            {
                var parent = node.Parent;
                if (parent is null)
                    return null;

                // Skip if the parent is the page/extension itself (top-level page properties)
                if (parent.Kind == EnumProvider.SyntaxKind.PageObject ||
                    parent.Kind == EnumProvider.SyntaxKind.PageExtensionObject)
                    return null;

                // Skip non-field page controls: groups, actions, areas, parts, views, etc.
                // Caption/ToolTip on these are not backed by a table field, so CC0011 does not apply.
                if (IsNonFieldPageControl(parent.Kind))
                    return null;

                return parent;
            }

            node = node.Parent;
        }
        return null;
    }

    /// <summary>
    /// Returns true when we should suppress the diagnostic — either because there is no
    /// related table field (e.g. the page field is bound to a global variable or an expression,
    /// so Caption/ToolTip on the page is the right place), or because the table field already
    /// has the property.
    /// </summary>
    private static bool ShouldSkip(
        SyntaxNodeAnalysisContext ctx,
        SyntaxNode pageFieldNode,
        string propertyName)
    {
        var controlSymbol = ctx.SemanticModel.GetDeclaredSymbol(pageFieldNode) as IControlSymbol;
        if (controlSymbol is null)
        {
            // Page extension modify(...) blocks don't produce an IControlSymbol.
            // Fall back to a name-based syntax search so we can still check the table field.
            if (pageFieldNode.Kind != EnumProvider.SyntaxKind.PageField)
                return ShouldSkipViaNameSearch(ctx, pageFieldNode, propertyName);

            return true; // regular page field, can't resolve → be conservative
        }

        var relatedField = controlSymbol.RelatedFieldSymbol;
        if (relatedField is null)
            return true; // no table field (global var, expression, …) → Caption/ToolTip is correct here

        // Try to locate the table field's syntax node via its source location
        // or by searching the compilation's syntax trees.
        var tableFieldNode = GetFieldSyntaxNode(
            relatedField, ctx.SemanticModel.Compilation, ctx.CancellationToken);

        if (tableFieldNode is null)
        {
            // Field is from an external package (no source in this compilation).
            // Only report if the containing table can actually be extended (i.e. it is Public).
            // If the table has Internal (or Local) access, creating a table extension is not
            // possible → suppress the diagnostic.
            return IsContainingTableInternal(relatedField);
        }

        // Whether or not the table field has the property, the page field should not have it.
        // If the table already has it, the page copy is redundant; if not, it should be moved there.
        return false;
    }

    /// <summary>
    /// Fallback for page extension modify blocks where GetDeclaredSymbol returns null.
    /// Extracts the field name from the modify block's argument and searches the compilation.
    /// </summary>
    private static bool ShouldSkipViaNameSearch(
        SyntaxNodeAnalysisContext ctx,
        SyntaxNode modifyBlockNode,
        string propertyName)
    {
        var fieldName = TryGetModifyBlockFieldName(modifyBlockNode);
        if (string.IsNullOrEmpty(fieldName))
            return true; // can't extract field name → be conservative

        // Search all tables in the compilation (no table name filter — we don't know
        // which table the page extension's base page sources from without the semantic model).
        var tableFieldNode = FindFieldNodeInCompilation(
            ctx.SemanticModel.Compilation, fieldName, tableName: null, ctx.CancellationToken);

        if (tableFieldNode is null)
            return false; // field is from an external package; report so the user can move to a table extension

        // Whether or not the table field has the property, the page field should not have it.
        return false;
    }

    /// <summary>
    /// Extracts the field name from a page extension modify block: modify(FieldName) or modify("Field Name").
    /// Returns the first meaningful token after '(' and before ')' or '{'.
    /// </summary>
    private static string? TryGetModifyBlockFieldName(SyntaxNode modifyBlockNode)
    {
        bool afterOpenParen = false;
        foreach (var token in modifyBlockNode.DescendantTokens())
        {
            var text = token.ValueText;
            if (text is null) continue;
            if (text == "{" || text == ")") break;
            if (!afterOpenParen)
            {
                if (text == "(") afterOpenParen = true;
                continue;
            }
            if (!string.IsNullOrWhiteSpace(text))
                return text;
        }
        return null;
    }

    // ── Field syntax node resolution ──────────────────────────────────────────

    /// <summary>
    /// Locates the syntax node for the given table field symbol.
    /// First tries the symbol's declared source locations; if that fails,
    /// falls back to a name-based search across the compilation's syntax trees.
    /// Returns null when the field is from an external reference package (no source available).
    /// </summary>
    internal static SyntaxNode? GetFieldSyntaxNode(
        IFieldSymbol fieldSymbol,
        Compilation? compilation,
        CancellationToken ct = default)
    {
        // Strategy 1: use the symbol's Locations to find the field in source
        var nodeViaLocation = TryGetNodeViaLocations(fieldSymbol, ct);
        if (nodeViaLocation is not null)
            return nodeViaLocation;

        // Strategy 2: name-based search in the compilation's source trees
        if (compilation is not null)
        {
            var fieldName = TryGetName(fieldSymbol);
            if (!string.IsNullOrEmpty(fieldName))
            {
                var tableName = TryGetContainingName(fieldSymbol);
                return FindFieldNodeInCompilation(compilation, fieldName, tableName, ct);
            }
        }

        return null;
    }

    private static SyntaxNode? TryGetNodeViaLocations(IFieldSymbol fieldSymbol, CancellationToken ct)
    {
        try
        {
            // Access Locations via reflection (ISymbol.Locations)
            var locProp = GetPublicPropertyFromTypeOrInterfaces(fieldSymbol, "Locations");
            if (locProp is null)
                return null;

            var locations = locProp.GetValue(fieldSymbol) as System.Collections.IEnumerable;
            if (locations is null)
                return null;

            foreach (var loc in locations)
            {
                if (loc is null)
                    continue;

                var locType = loc.GetType();

                var isInSource = locType.GetProperty("IsInSource")?.GetValue(loc) as bool?;
                if (isInSource != true)
                    continue;

                var sourceTree = locType.GetProperty("SourceTree")?.GetValue(loc) as SyntaxTree;
                if (sourceTree is null)
                    continue;

                var spanObj = locType.GetProperty("SourceSpan")?.GetValue(loc);
                if (spanObj is null)
                    continue;

                var start = spanObj.GetType().GetProperty("Start")?.GetValue(spanObj) as int?;
                if (start is null)
                    continue;

                var root = sourceTree.GetRoot(ct);
                var node = root.FindToken(start.Value).Parent;
                while (node is not null)
                {
                    if (node.Kind == EnumProvider.SyntaxKind.Field)
                        return node;
                    if (node.Kind == EnumProvider.SyntaxKind.TableObject)
                        break;
                    node = node.Parent;
                }
            }
        }
        catch { }

        return null;
    }

    private static PropertyInfo? GetPublicPropertyFromTypeOrInterfaces(object obj, string propertyName)
    {
        var type = obj.GetType();

        // Direct lookup first
        var prop = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (prop is not null)
            return prop;

        // Search interfaces
        foreach (var iface in type.GetInterfaces())
        {
            prop = iface.GetProperty(propertyName);
            if (prop is not null)
                return prop;
        }

        return null;
    }

    // ── Name extraction via reflection ────────────────────────────────────────

    internal static string? TryGetName(IFieldSymbol fieldSymbol)
    {
        try
        {
            var prop = GetPublicPropertyFromTypeOrInterfaces(fieldSymbol, "Name");
            return prop?.GetValue(fieldSymbol) as string;
        }
        catch { return null; }
    }

    internal static string? TryGetContainingName(IFieldSymbol fieldSymbol)
    {
        try
        {
            var type = fieldSymbol.GetType();
            foreach (var propName in _containingPropNames)
            {
                var prop = type.GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
                if (prop is null)
                    continue;

                var containing = prop.GetValue(fieldSymbol);
                if (containing is null)
                    continue;

                var name = GetPublicPropertyFromTypeOrInterfaces(containing, "Name")
                    ?.GetValue(containing) as string;

                if (!string.IsNullOrEmpty(name))
                    return name;
            }

            return null;
        }
        catch { return null; }
    }

    // ── Compilation-wide field search ─────────────────────────────────────────

    /// <summary>
    /// Searches all source syntax trees in the compilation for a table field declaration
    /// matching the given field name (and optionally the table name).
    /// </summary>
    internal static SyntaxNode? FindFieldNodeInCompilation(
        Compilation compilation,
        string fieldName,
        string? tableName,
        CancellationToken ct)
    {
        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            var root = syntaxTree.GetRoot(ct);

            foreach (var tableNode in root.DescendantNodesAndSelf())
            {
                if (tableNode.Kind != EnumProvider.SyntaxKind.TableObject)
                    continue;

                // Optionally filter by table name to avoid false matches on common field names
                if (tableName is not null)
                {
                    var nodeName = ExtractHeaderTokenAtIndex(tableNode, 2);
                    if (!string.Equals(
                            StripQuotes(nodeName ?? string.Empty),
                            StripQuotes(tableName),
                            StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                foreach (var node in tableNode.DescendantNodes())
                {
                    if (node.Kind != EnumProvider.SyntaxKind.Field)
                        continue;

                    var nodeFName = GetFieldNameFromNode(node);
                    if (string.Equals(
                            StripQuotes(nodeFName ?? string.Empty),
                            StripQuotes(fieldName),
                            StringComparison.OrdinalIgnoreCase))
                        return node;
                }
            }
        }

        return null;
    }

    // ── Syntax helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Extracts the field name from a field declaration: field(ID; FieldName; Type).
    /// Returns the first identifier after the first semicolon inside the parentheses.
    /// </summary>
    private static string? GetFieldNameFromNode(SyntaxNode fieldNode)
    {
        bool afterFirstSemi = false;
        foreach (var token in fieldNode.DescendantTokens())
        {
            var text = token.ValueText;
            if (text is null)
                continue;

            if (!afterFirstSemi)
            {
                if (text == ";")
                    afterFirstSemi = true;
                continue;
            }

            if (text == ";")
                return null; // hit second semicolon before finding identifier
            if (token.Kind == SyntaxKind.IdentifierToken)
                return text;
        }
        return null;
    }

    /// <summary>
    /// Returns the token ValueText at the given index among tokens before the first '{'.
    /// Used to extract the object name from "keyword ID Name { ... }".
    /// </summary>
    private static string? ExtractHeaderTokenAtIndex(SyntaxNode node, int index)
    {
        int i = 0;
        foreach (var token in node.DescendantTokens())
        {
            var text = token.ValueText;
            if (text is null)
                continue;
            if (text == "{")
                break;
            if (i == index)
                return text;
            i++;
        }
        return null;
    }

    // ── Non-field page control filter ─────────────────────────────────────────

    /// <summary>
    /// Returns true for page control kinds that never have an underlying table field
    /// (groups, actions, areas, parts, views, …). CC0011 does not apply to these.
    /// </summary>
    private static bool IsNonFieldPageControl(SyntaxKind kind) =>
        kind == EnumProvider.SyntaxKind.PageGroup ||
        kind == EnumProvider.SyntaxKind.PageAction ||
        kind == EnumProvider.SyntaxKind.PageActionGroup ||
        kind == EnumProvider.SyntaxKind.PageActionArea ||
        kind == EnumProvider.SyntaxKind.PageActionSeparator ||
        kind == EnumProvider.SyntaxKind.PageArea ||
        kind == EnumProvider.SyntaxKind.PageCustomAction ||
        kind == EnumProvider.SyntaxKind.PageSystemAction ||
        kind == EnumProvider.SyntaxKind.PageSystemPart ||
        kind == EnumProvider.SyntaxKind.PagePart ||
        kind == EnumProvider.SyntaxKind.PageView;

    // ── Internal table check ──────────────────────────────────────────────────

    /// <summary>
    /// Returns true when the table that contains the given field has Internal (or more restrictive)
    /// declared accessibility, meaning a table extension cannot be created for it in a downstream app.
    /// In that case CC0011 should be suppressed: there is nowhere to move the property.
    /// </summary>
    private static bool IsContainingTableInternal(IFieldSymbol fieldSymbol)
    {
        try
        {
            var fieldType = fieldSymbol.GetType();
            foreach (var propName in _containingPropNames)
            {
                var prop = fieldType.GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
                if (prop is null)
                    continue;

                var containing = prop.GetValue(fieldSymbol);
                if (containing is null)
                    continue;

                var accessProp = GetPublicPropertyFromTypeOrInterfaces(containing, "DeclaredAccessibility");
                if (accessProp is null)
                    continue;

                var accessValue = accessProp.GetValue(containing);
                if (accessValue is null)
                    continue;

                // In BC's symbol model the type is NavCodeAnalysis.Symbols.Accessibility.
                // Internal (or Local) means the table cannot be extended from outside the app.
                return Equals(accessValue, EnumProvider.Accessibility.Internal) ||
                       Equals(accessValue, EnumProvider.Accessibility.Local);
            }

            return false;
        }
        catch { return false; }
    }

    private static string StripQuotes(string name) =>
        name.Length >= 2 && name[0] == '"' && name[^1] == '"'
            ? name[1..^1]
            : name;

    internal static bool FieldNodeHasProperty(SyntaxNode fieldNode, string propertyName)
    {
        foreach (var token in fieldNode.DescendantTokens())
        {
            if (token.Kind != SyntaxKind.IdentifierToken)
                continue;

            if (!string.Equals(token.ValueText, propertyName, StringComparison.OrdinalIgnoreCase))
                continue;

            var next = token.GetNextToken();
            if (next.Kind == SyntaxKind.EqualsToken)
                return true;
        }
        return false;
    }
}
