using System.Globalization;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

namespace Socitas.AICop;

public static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor NoGlobalVariables = new(
        id: DiagnosticIds.NoGlobalVariables,
        title: AICopAnalyzers.NoGlobalVariablesTitle,
        messageFormat: AICopAnalyzers.NoGlobalVariablesMessageFormat,
        category: Category.Style,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: AICopAnalyzers.NoGlobalVariablesDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.NoGlobalVariables));

    public static readonly DiagnosticDescriptor CaptionTooltipOnPage = new(
        id: DiagnosticIds.CaptionTooltipOnPage,
        title: AICopAnalyzers.CaptionTooltipOnPageTitle,
        messageFormat: AICopAnalyzers.CaptionTooltipOnPageMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: AICopAnalyzers.CaptionTooltipOnPageDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.CaptionTooltipOnPage));

    public static readonly DiagnosticDescriptor OpenBraceOnSameLine = new(
        id: DiagnosticIds.OpenBraceOnSameLine,
        title: AICopAnalyzers.OpenBraceOnSameLineTitle,
        messageFormat: AICopAnalyzers.OpenBraceOnSameLineMessageFormat,
        category: Category.Style,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: AICopAnalyzers.OpenBraceOnSameLineDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.OpenBraceOnSameLine));

    public static readonly DiagnosticDescriptor UseRestClient = new(
        id: DiagnosticIds.UseRestClient,
        title: AICopAnalyzers.UseRestClientTitle,
        messageFormat: AICopAnalyzers.UseRestClientMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: AICopAnalyzers.UseRestClientDescription,
        helpLinkUri: "https://learn.microsoft.com/en-us/dynamics365/business-central/application/system-application/codeunit/system.restclient.rest-client");

    public static readonly DiagnosticDescriptor InitializeRestClientWithHandler = new(
        id: DiagnosticIds.InitializeRestClientWithHandler,
        title: AICopAnalyzers.InitializeRestClientWithHandlerTitle,
        messageFormat: AICopAnalyzers.InitializeRestClientWithHandlerMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: AICopAnalyzers.InitializeRestClientWithHandlerDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.InitializeRestClientWithHandler));

    public static readonly DiagnosticDescriptor NoExitWithDefaultValue = new(
        id: DiagnosticIds.NoExitWithDefaultValue,
        title: AICopAnalyzers.NoExitWithDefaultValueTitle,
        messageFormat: AICopAnalyzers.NoExitWithDefaultValueMessageFormat,
        category: Category.Style,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: AICopAnalyzers.NoExitWithDefaultValueDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.NoExitWithDefaultValue));

    public static readonly DiagnosticDescriptor UseActionRef = new(
        id: DiagnosticIds.UseActionRef,
        title: AICopAnalyzers.UseActionRefTitle,
        messageFormat: AICopAnalyzers.UseActionRefMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: AICopAnalyzers.UseActionRefDescription,
        helpLinkUri: "https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/devenv-promoted-actions#actionref-syntax-example");

    public static readonly DiagnosticDescriptor ExtensionMemberMissingSocSuffix = new(
        id: DiagnosticIds.ExtensionMemberMissingSocSuffix,
        title: AICopAnalyzers.ExtensionMemberMissingSocSuffixTitle,
        messageFormat: AICopAnalyzers.ExtensionMemberMissingSocSuffixMessageFormat,
        category: Category.Naming,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: AICopAnalyzers.ExtensionMemberMissingSocSuffixDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.ExtensionMemberMissingSocSuffix));

    public static readonly DiagnosticDescriptor LocalProcedureHasSocSuffix = new(
        id: DiagnosticIds.LocalProcedureHasSocSuffix,
        title: AICopAnalyzers.LocalProcedureHasSocSuffixTitle,
        messageFormat: AICopAnalyzers.LocalProcedureHasSocSuffixMessageFormat,
        category: Category.Naming,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: AICopAnalyzers.LocalProcedureHasSocSuffixDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.LocalProcedureHasSocSuffix));

    public static string GetHelpUri(string identifier)
    {
        return string.Format(CultureInfo.InvariantCulture, "https://company.internal/docs/analyzers/aicop/{0}/", identifier.ToLower());
    }

    internal static class Category
    {
        public const string Design = "Design";
        public const string Naming = "Naming";
        public const string Style = "Style";
        public const string Usage = "Usage";
        public const string Performance = "Performance";
        public const string Security = "Security";
    }
}
