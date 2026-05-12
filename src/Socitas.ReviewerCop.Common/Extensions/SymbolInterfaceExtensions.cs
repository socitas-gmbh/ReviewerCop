using System.Reflection;
using Socitas.ReviewerCop.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;

namespace Socitas.ReviewerCop.Common.Extensions;

public static class SymbolInterfaceExtensions
{
    /// <summary>
    /// Gets the QualifiedName of the ContainingNamespace for a symbol using reflection.
    /// This method handles breaking changes between versions where ContainingNamespace
    /// may not exist in older versions of Microsoft.Dynamics.Nav.CodeAnalysis.
    /// </summary>
    /// <param name="symbol">The symbol to get the containing namespace qualified name from.</param>
    /// <returns>The qualified name of the containing namespace, or null if not available.</returns>
    public static string? GetContainingNamespaceQualifiedNameWithReflection(this ISymbol? symbol)
        => SymbolHelper.GetContainingNamespaceQualifiedName(symbol);

    public static IPageTypeSymbol? GetPageTypeSymbol(this ISymbol symbol)
    {
        var declaredType = (symbol.OriginalDefinition ?? symbol).GetTypeSymbol();
        declaredType = (declaredType?.OriginalDefinition as ITypeSymbol) ?? declaredType;
        return declaredType as IPageTypeSymbol;
    }

    public static IEnumerable<IControlSymbol>? GetFlattenedControls(this ISymbol? symbol) =>
        symbol switch
        {
            IPageBaseTypeSymbol page => page.FlattenedControls,
            IPageExtensionBaseTypeSymbol pageExtension => pageExtension.AddedControlsFlattened,
            _ => null
        };

    public static string GetFullyQualifiedObjectName(this ISymbol symbol, bool quoteIdentifierIfNeeded = false)
    {
        var symbolName = quoteIdentifierIfNeeded
               ? symbol.Name.QuoteIdentifierIfNeededWithReflection()
               : symbol.Name;

        var containingNamespace = symbol.GetContainingNamespaceQualifiedNameWithReflection();
        if (string.IsNullOrEmpty(containingNamespace))
            return symbolName;

        return $"{containingNamespace}.{symbolName}";
    }

    /// <summary>
    /// Returns true when the symbol is defined in the current compilation's source rather than
    /// in a dependency or system module. Uses reflection because the BC SDK surface differs from
    /// Roslyn's: ISymbol exposes a single <c>Location</c> (singular), and concrete symbols such as
    /// ApplicationObjectTypeSymbol expose a direct <c>IsInSource</c> boolean.
    /// </summary>
    public static bool IsDefinedInSource(this ISymbol symbol)
    {
        try
        {
            // Strategy 1: direct IsInSource bool on the symbol (present on BC concrete symbol types).
            var directIsInSource = GetPublicPropertyFromTypeOrInterfaces(symbol, "IsInSource")?.GetValue(symbol) as bool?;
            if (directIsInSource.HasValue)
                return directIsInSource.Value;

            // Strategy 2: BC SDK's singular ISymbol.Location property.
            var singleLocProp = GetPublicPropertyFromTypeOrInterfaces(symbol, "Location");
            if (singleLocProp is not null)
            {
                var loc = singleLocProp.GetValue(symbol);
                var isInSource = loc?.GetType().GetProperty("IsInSource")?.GetValue(loc) as bool?;
                if (isInSource.HasValue)
                    return isInSource.Value;
            }

            // Strategy 3: Roslyn-style plural Locations (kept for forward compatibility with future SDKs).
            var locProp = GetPublicPropertyFromTypeOrInterfaces(symbol, "Locations");
            if (locProp?.GetValue(symbol) is System.Collections.IEnumerable locations)
            {
                foreach (var loc in locations)
                {
                    if (loc is null)
                        continue;
                    var isInSource = loc.GetType().GetProperty("IsInSource")?.GetValue(loc) as bool?;
                    if (isInSource == true)
                        return true;
                }
            }
        }
        catch { }

        return false;
    }

    private static PropertyInfo? GetPublicPropertyFromTypeOrInterfaces(object obj, string propertyName)
    {
        var type = obj.GetType();
        var prop = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (prop is not null)
            return prop;

        foreach (var iface in type.GetInterfaces())
        {
            prop = iface.GetProperty(propertyName);
            if (prop is not null)
                return prop;
        }

        return null;
    }

    #region Obsolete Extension Methods
    private static readonly Lazy<PropertyInfo?> _isObsoletePendingMoveProperty =
        new(() => typeof(ISymbol).GetProperty("IsObsoletePendingMove"));

    private static readonly Lazy<PropertyInfo?> _isObsoleteMovedProperty =
        new(() => typeof(ISymbol).GetProperty("IsObsoleteMoved"));

    private static bool GetObsoletePropertyValue(ISymbol symbol, PropertyInfo? property) =>
        property?.GetValue(symbol) as bool? ?? false;

    public static bool IsObsolete(this ISymbol symbol)
    {
        // Check the "always available" properties first
        if (symbol.IsObsoletePending || symbol.IsObsoleteRemoved)
        {
            return true;
        }

        // Use reflection to check properties that are not available in older versions
        if (GetObsoletePropertyValue(symbol, _isObsoleteMovedProperty.Value))
        {
            return true;
        }

        if (GetObsoletePropertyValue(symbol, _isObsoletePendingMoveProperty.Value))
        {
            return true;
        }

        return false;
    }
    #endregion
}