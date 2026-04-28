using System.Collections.Immutable;
using Socitas.ReviewerCop.Common.Extensions;
using Socitas.ReviewerCop.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace Socitas.ReviewerCop.Analyzers;

/// <summary>
/// CC0006 – Use SetLoadFields before Find operations for performance.
/// Reports when FindSet/FindFirst/FindLast/Find is called on a non-temporary record
/// without a preceding SetLoadFields call on the same variable in the same code block.
/// </summary>
[DiagnosticAnalyzer]
public sealed class UseSetLoadFields : DiagnosticAnalyzer
{
    private static readonly ImmutableHashSet<string> FindMethods =
        ImmutableHashSet.Create(StringComparer.OrdinalIgnoreCase,
            "FindSet", "FindFirst", "FindLast", "Find");

    private const string OnFindRecordTrigger = "OnFindRecord";

    private const string SetLoadFieldsMethod = "SetLoadFields";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.UseSetLoadFields);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterOperationAction(
            AnalyzeFindInvocation,
            EnumProvider.OperationKind.InvocationExpression);

    private void AnalyzeFindInvocation(OperationAnalysisContext ctx)
    {
        if (ctx.IsObsolete())
            return;

        if (ctx.Operation is not IInvocationExpression invocation)
            return;

        if (!string.Equals(invocation.TargetMethod.MethodKind.ToString(), "BuiltInMethod", StringComparison.OrdinalIgnoreCase))
            return;

        if (!FindMethods.Contains(invocation.TargetMethod.Name))
            return;

        // Must be called on a record type
        if (invocation.Instance?.Type is not IRecordTypeSymbol recordType)
            return;

        // Skip temporary records
        if (recordType.Temporary)
            return;

        // Try to determine the receiver variable name so we can check for SetLoadFields
        var receiverName = GetReceiverName(invocation);
        if (receiverName is null)
            return;

        var enclosingRoutine = GetEnclosingRoutine(ctx.Operation.Syntax);
        if (enclosingRoutine is null)
            return;

        // OnFindRecord uses Find(Which) as the standard record-lookup mechanism;
        // the platform drives field loading here, so SetLoadFields is unnecessary.
        if (IsOnFindRecordTrigger(enclosingRoutine))
            return;

        // If the record is passed to another function, skip to avoid false positives.
        // The called function may manage SetLoadFields or field access.
        if (IsPassedToAnotherFunction(enclosingRoutine, invocation.Syntax, receiverName))
            return;

        // Skip Find('-') used in an existence check: Record.Find('-') and (Record.Next() <> 0)
        if (IsExistenceCheckPattern(invocation))
            return;

        var usedFields = GetUsedFieldNames(enclosingRoutine, invocation.Syntax, receiverName);

        // Report when SetLoadFields is missing or does not include all used fields.
        if (!TryGetSetLoadFieldsInvocation(enclosingRoutine, invocation.Syntax, receiverName, out var setLoadFieldsInvocation) ||
            (usedFields.Count > 0 && !ContainsAllUsedFields(setLoadFieldsInvocation!, usedFields)))
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.UseSetLoadFields,
                invocation.Syntax.GetLocation(),
                invocation.TargetMethod.Name));
        }
    }

    private static string? GetReceiverName(IInvocationExpression invocation)
    {
        if (invocation.Instance?.Syntax is IdentifierNameSyntax identifier)
            return identifier.Identifier.ValueText;

        return null;
    }

    /// <summary>
    /// Returns true for the established existence-check pattern:
    ///   Record.Find('-') and (Record.Next() &lt;&gt; 0)
    /// This pattern loads no fields and SetLoadFields is unnecessary.
    /// </summary>
    private static bool IsExistenceCheckPattern(IInvocationExpression invocation)
    {
        if (!string.Equals(invocation.TargetMethod.Name, "Find", StringComparison.OrdinalIgnoreCase))
            return false;

        // The single argument must be the string literal '-'
        if (invocation.Arguments.Length != 1)
            return false;

        var argText = invocation.Arguments[0].Syntax.ToFullString().Trim().Trim('\'');
        if (argText != "-")
            return false;

        // The Find call's parent expression must contain a Next() call on the same receiver
        var parentExpr = invocation.Syntax.Parent;
         while (parentExpr is not null &&
             !IsSyntaxKind(parentExpr, "LogicalAndExpression") &&
             !IsSyntaxKind(parentExpr, "IfStatement"))
        {
            parentExpr = parentExpr.Parent;
        }

        if (parentExpr is null)
            return false;

        var receiverName = GetReceiverName(invocation);
        if (receiverName is null)
            return false;

        // Look for a Next() call on the same receiver within the same expression
        foreach (var token in parentExpr.DescendantTokens())
        {
            if (!IsSyntaxKind(token, "IdentifierToken"))
                continue;
            if (!string.Equals(token.ValueText, "Next", StringComparison.OrdinalIgnoreCase))
                continue;

            var prevDot = token.GetPreviousToken();
            if (!IsSyntaxKind(prevDot, "DotToken"))
                continue;

            var receiver = prevDot.GetPreviousToken();
            if (string.Equals(receiver.ValueText, receiverName, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static SyntaxNode? GetEnclosingRoutine(SyntaxNode node)
    {
        var current = node;
        while (current is not null &&
               !IsSyntaxKind(current, "TriggerDeclaration") &&
               !IsSyntaxKind(current, "MethodDeclaration"))
        {
            current = current.Parent;
        }

        return current;
    }

    private static bool IsOnFindRecordTrigger(SyntaxNode routine)
    {
        if (!IsSyntaxKind(routine, "TriggerDeclaration"))
            return false;

        var nameToken = routine.DescendantTokens()
            .FirstOrDefault(t => IsSyntaxKind(t, "IdentifierToken"));
        return string.Equals(nameToken.ValueText, OnFindRecordTrigger, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPassedToAnotherFunction(SyntaxNode enclosingRoutine, SyntaxNode findCallSyntax, string receiverName)
    {
        foreach (var invocation in enclosingRoutine.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.SpanStart >= findCallSyntax.SpanStart)
                continue;

            // Skip direct calls on the same record variable (e.g. MyRec.SetRange(...)).
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Expression is IdentifierNameSyntax callReceiver &&
                string.Equals(callReceiver.Identifier.ValueText, receiverName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var arg in invocation.ArgumentList.Arguments)
            {
                if (arg is IdentifierNameSyntax identifier &&
                    string.Equals(identifier.Identifier.ValueText, receiverName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (arg.DescendantTokens().Any(token =>
                    IsSyntaxKind(token, "IdentifierToken") &&
                    string.Equals(token.ValueText, receiverName, StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsSyntaxKind(SyntaxNode node, string expectedKindName)
    {
        return string.Equals(node.Kind.ToString(), expectedKindName, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSyntaxKind(SyntaxToken token, string expectedKindName)
    {
        return string.Equals(token.Kind.ToString(), expectedKindName, StringComparison.OrdinalIgnoreCase);
    }

    private static ImmutableHashSet<string> GetUsedFieldNames(
        SyntaxNode enclosingRoutine,
        SyntaxNode findCallSyntax,
        string receiverName)
    {
        var fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var access in enclosingRoutine.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            if (access.SpanStart <= findCallSyntax.SpanStart)
                continue;

            if (access.Expression is not IdentifierNameSyntax identifier ||
                !string.Equals(identifier.Identifier.ValueText, receiverName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Method call: receiver.Member(...). We only collect field accesses.
            if (access.Parent is InvocationExpressionSyntax invoked && ReferenceEquals(invoked.Expression, access))
                continue;

            var fieldText = access.Name.ToFullString().Trim();
            if (!string.IsNullOrWhiteSpace(fieldText))
                fields.Add(NormalizeFieldName(fieldText));
        }

        return fields.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static bool TryGetSetLoadFieldsInvocation(
        SyntaxNode enclosingRoutine,
        SyntaxNode findCallSyntax,
        string receiverName,
        out InvocationExpressionSyntax? setLoadFieldsInvocation)
    {
        setLoadFieldsInvocation = null;

        foreach (var invocation in enclosingRoutine.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.SpanStart >= findCallSyntax.SpanStart)
                continue;

            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
                continue;

            if (memberAccess.Expression is not IdentifierNameSyntax callReceiver ||
                !string.Equals(callReceiver.Identifier.ValueText, receiverName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!string.Equals(memberAccess.Name.Identifier.ValueText, SetLoadFieldsMethod, StringComparison.OrdinalIgnoreCase))
                continue;

            // Keep the closest preceding SetLoadFields invocation.
            if (setLoadFieldsInvocation is null || invocation.SpanStart > setLoadFieldsInvocation.SpanStart)
                setLoadFieldsInvocation = invocation;
        }

        return setLoadFieldsInvocation is not null;
    }

    private static bool ContainsAllUsedFields(InvocationExpressionSyntax setLoadFieldsInvocation, ImmutableHashSet<string> usedFields)
    {
        var configuredFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var arg in setLoadFieldsInvocation.ArgumentList.Arguments)
        {
            var fieldName = NormalizeFieldName(arg.ToFullString());
            if (!string.IsNullOrWhiteSpace(fieldName))
                configuredFields.Add(fieldName);
        }

        if (configuredFields.Count == 0)
            return false;

        return usedFields.All(configuredFields.Contains);
    }

    private static string NormalizeFieldName(string text)
    {
        var normalized = text.Trim();
        if (normalized.StartsWith("\"", StringComparison.Ordinal) &&
            normalized.EndsWith("\"", StringComparison.Ordinal) &&
            normalized.Length > 1)
        {
            normalized = normalized.Substring(1, normalized.Length - 2);
        }

        return normalized;
    }
}
