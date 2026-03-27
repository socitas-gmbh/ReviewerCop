using System.Collections.Immutable;
using Socitas.ReviewerCop.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace Socitas.ReviewerCop.Analyzers;

/// <summary>
/// CC0005 – Event subscriber procedures must follow the FunctionName+EventName convention
/// (e.g. SetRefOrderTypeInitValueOnBeforeInsertEvent).
/// </summary>
[DiagnosticAnalyzer]
public sealed class EventSubscriberNamingConvention : DiagnosticAnalyzer
{
    private const string EventSubscriberAttributeName = "EventSubscriber";
    private const int EventNameArgIndex = 2;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.EventSubscriberNamingConvention);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterCodeBlockAction(CheckEventSubscriberNaming);

    private void CheckEventSubscriberNaming(CodeBlockAnalysisContext ctx)
    {
        if (ctx.CodeBlock is not MethodDeclarationSyntax method)
            return;

        var attributes = method.Attributes;
        if (attributes.Count == 0)
            return;

        var eventSubscriberAttribute = attributes.FirstOrDefault(attr =>
        {
            var nameText = attr.GetIdentifierOrLiteralValue();
            return nameText is not null &&
                   SemanticFacts.IsSameName(nameText, EventSubscriberAttributeName);
        });

        if (eventSubscriberAttribute is null)
            return;

        var argList = eventSubscriberAttribute.ArgumentList;
        if (argList is null || argList.Arguments.Count <= EventNameArgIndex)
            return;

        var eventName = argList.Arguments[EventNameArgIndex].GetIdentifierOrLiteralValue();
        if (string.IsNullOrEmpty(eventName))
            return;

        var methodName = method.Name.Identifier.ValueText;
        if (string.IsNullOrEmpty(methodName))
            return;

        if (!methodName.EndsWith(eventName, StringComparison.OrdinalIgnoreCase))
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.EventSubscriberNamingConvention,
                method.Name.GetLocation(),
                methodName,
                eventName));
        }
    }
}
