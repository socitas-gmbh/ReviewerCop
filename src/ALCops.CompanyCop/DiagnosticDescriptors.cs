using System.Globalization;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

namespace ALCops.CompanyCop;

public static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor NoTodoComments = new(
        id: DiagnosticIds.NoTodoComments,
        title: CompanyCopAnalyzers.NoTodoCommentsTitle,
        messageFormat: CompanyCopAnalyzers.NoTodoCommentsMessageFormat,
        category: Category.Style,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: CompanyCopAnalyzers.NoTodoCommentsDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.NoTodoComments));

    public static readonly DiagnosticDescriptor NoGlobalVariables = new(
        id: DiagnosticIds.NoGlobalVariables,
        title: CompanyCopAnalyzers.NoGlobalVariablesTitle,
        messageFormat: CompanyCopAnalyzers.NoGlobalVariablesMessageFormat,
        category: Category.Style,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: CompanyCopAnalyzers.NoGlobalVariablesDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.NoGlobalVariables));

    public static readonly DiagnosticDescriptor ValidateFieldAssignments = new(
        id: DiagnosticIds.ValidateFieldAssignments,
        title: CompanyCopAnalyzers.ValidateFieldAssignmentsTitle,
        messageFormat: CompanyCopAnalyzers.ValidateFieldAssignmentsMessageFormat,
        category: Category.Usage,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: CompanyCopAnalyzers.ValidateFieldAssignmentsDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.ValidateFieldAssignments));

    public static readonly DiagnosticDescriptor NoTypeOrPrefixInVariableName = new(
        id: DiagnosticIds.NoTypeOrPrefixInVariableName,
        title: CompanyCopAnalyzers.NoTypeOrPrefixInVariableNameTitle,
        messageFormat: CompanyCopAnalyzers.NoTypeOrPrefixInVariableNameMessageFormat,
        category: Category.Naming,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: CompanyCopAnalyzers.NoTypeOrPrefixInVariableNameDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.NoTypeOrPrefixInVariableName));

    public static readonly DiagnosticDescriptor EventSubscriberNamingConvention = new(
        id: DiagnosticIds.EventSubscriberNamingConvention,
        title: CompanyCopAnalyzers.EventSubscriberNamingConventionTitle,
        messageFormat: CompanyCopAnalyzers.EventSubscriberNamingConventionMessageFormat,
        category: Category.Naming,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: CompanyCopAnalyzers.EventSubscriberNamingConventionDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.EventSubscriberNamingConvention));

    public static readonly DiagnosticDescriptor UseSetLoadFields = new(
        id: DiagnosticIds.UseSetLoadFields,
        title: CompanyCopAnalyzers.UseSetLoadFieldsTitle,
        messageFormat: CompanyCopAnalyzers.UseSetLoadFieldsMessageFormat,
        category: Category.Performance,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: CompanyCopAnalyzers.UseSetLoadFieldsDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.UseSetLoadFields));

    public static readonly DiagnosticDescriptor NoModifyInOnValidate = new(
        id: DiagnosticIds.NoModifyInOnValidate,
        title: CompanyCopAnalyzers.NoModifyInOnValidateTitle,
        messageFormat: CompanyCopAnalyzers.NoModifyInOnValidateMessageFormat,
        category: Category.Style,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: CompanyCopAnalyzers.NoModifyInOnValidateDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.NoModifyInOnValidate));

    public static readonly DiagnosticDescriptor DataClassificationOnTable = new(
        id: DiagnosticIds.DataClassificationOnTable,
        title: CompanyCopAnalyzers.DataClassificationOnTableTitle,
        messageFormat: CompanyCopAnalyzers.DataClassificationOnTableMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: CompanyCopAnalyzers.DataClassificationOnTableDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.DataClassificationOnTable));

    public static readonly DiagnosticDescriptor LabelCommentForPlaceholders = new(
        id: DiagnosticIds.LabelCommentForPlaceholders,
        title: CompanyCopAnalyzers.LabelCommentForPlaceholdersTitle,
        messageFormat: CompanyCopAnalyzers.LabelCommentForPlaceholdersMessageFormat,
        category: Category.Style,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: CompanyCopAnalyzers.LabelCommentForPlaceholdersDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.LabelCommentForPlaceholders));

    public static string GetHelpUri(string identifier)
    {
        return string.Format(CultureInfo.InvariantCulture, "https://company.internal/docs/analyzers/companycop/{0}/", identifier.ToLower());
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
