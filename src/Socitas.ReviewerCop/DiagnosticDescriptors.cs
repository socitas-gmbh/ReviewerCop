using System.Globalization;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

namespace Socitas.ReviewerCop;

public static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor NoTodoComments = new(
        id: DiagnosticIds.NoTodoComments,
        title: ReviewerCopAnalyzers.NoTodoCommentsTitle,
        messageFormat: ReviewerCopAnalyzers.NoTodoCommentsMessageFormat,
        category: Category.Style,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: ReviewerCopAnalyzers.NoTodoCommentsDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.NoTodoComments));

    public static readonly DiagnosticDescriptor ValidateFieldAssignments = new(
        id: DiagnosticIds.ValidateFieldAssignments,
        title: ReviewerCopAnalyzers.ValidateFieldAssignmentsTitle,
        messageFormat: ReviewerCopAnalyzers.ValidateFieldAssignmentsMessageFormat,
        category: Category.Usage,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: ReviewerCopAnalyzers.ValidateFieldAssignmentsDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.ValidateFieldAssignments));

    public static readonly DiagnosticDescriptor NoTypeOrPrefixInVariableName = new(
        id: DiagnosticIds.NoTypeOrPrefixInVariableName,
        title: ReviewerCopAnalyzers.NoTypeOrPrefixInVariableNameTitle,
        messageFormat: ReviewerCopAnalyzers.NoTypeOrPrefixInVariableNameMessageFormat,
        category: Category.Naming,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: ReviewerCopAnalyzers.NoTypeOrPrefixInVariableNameDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.NoTypeOrPrefixInVariableName));

    public static readonly DiagnosticDescriptor EventSubscriberNamingConvention = new(
        id: DiagnosticIds.EventSubscriberNamingConvention,
        title: ReviewerCopAnalyzers.EventSubscriberNamingConventionTitle,
        messageFormat: ReviewerCopAnalyzers.EventSubscriberNamingConventionMessageFormat,
        category: Category.Naming,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: ReviewerCopAnalyzers.EventSubscriberNamingConventionDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.EventSubscriberNamingConvention));

    public static readonly DiagnosticDescriptor UseSetLoadFields = new(
        id: DiagnosticIds.UseSetLoadFields,
        title: ReviewerCopAnalyzers.UseSetLoadFieldsTitle,
        messageFormat: ReviewerCopAnalyzers.UseSetLoadFieldsMessageFormat,
        category: Category.Performance,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: ReviewerCopAnalyzers.UseSetLoadFieldsDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.UseSetLoadFields));

    public static readonly DiagnosticDescriptor NoModifyInOnValidate = new(
        id: DiagnosticIds.NoModifyInOnValidate,
        title: ReviewerCopAnalyzers.NoModifyInOnValidateTitle,
        messageFormat: ReviewerCopAnalyzers.NoModifyInOnValidateMessageFormat,
        category: Category.Style,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: ReviewerCopAnalyzers.NoModifyInOnValidateDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.NoModifyInOnValidate));

    public static readonly DiagnosticDescriptor DataClassificationOnTable = new(
        id: DiagnosticIds.DataClassificationOnTable,
        title: ReviewerCopAnalyzers.DataClassificationOnTableTitle,
        messageFormat: ReviewerCopAnalyzers.DataClassificationOnTableMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: ReviewerCopAnalyzers.DataClassificationOnTableDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.DataClassificationOnTable));

    public static readonly DiagnosticDescriptor LabelCommentForPlaceholders = new(
        id: DiagnosticIds.LabelCommentForPlaceholders,
        title: ReviewerCopAnalyzers.LabelCommentForPlaceholdersTitle,
        messageFormat: ReviewerCopAnalyzers.LabelCommentForPlaceholdersMessageFormat,
        category: Category.Style,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: ReviewerCopAnalyzers.LabelCommentForPlaceholdersDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.LabelCommentForPlaceholders));

    public static string GetHelpUri(string identifier)
    {
        return string.Format(CultureInfo.InvariantCulture, "https://company.internal/docs/analyzers/reviewercop/{0}/", identifier.ToLower());
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
