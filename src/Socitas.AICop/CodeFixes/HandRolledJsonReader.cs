using System.Collections.Immutable;
using Socitas.AICop.Analyzers;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Text;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;

namespace Socitas.AICop.CodeFixes;

/// <summary>
/// AI0010 – Quick fix: collapse the hand-rolled JsonObject reader chain into
/// a single JsonObject.Get&lt;Type&gt;(Key, true) statement. Covers both the
/// nested 'if Get then assign' shape and the guard 'if not Get then exit; ...' shape.
/// </summary>
[CodeFixProvider(nameof(HandRolledJsonReaderFixProvider))]
public sealed class HandRolledJsonReaderFixProvider : CodeFixProvider
{
    private sealed class CollapseAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.QuickFix;
        public override bool SupportsFixAll { get; }
        public override string? FixAllSingleInstanceTitle => string.Empty;
        public override string? FixAllTitle => Title;

        public CollapseAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument,
            string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            SupportsFixAll = generateFixAll;
        }
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticIds.HandRolledJsonReader);

    public sealed override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext ctx)
    {
        var root = await ctx.Document.GetSyntaxRootAsync(ctx.CancellationToken).ConfigureAwait(false);
        if (root is null)
            return;

        if (!TryExtractPattern(root, ctx.Span, out var pattern) || pattern is null)
            return;

        var actionTitle = string.Format(AICopAnalyzers.HandRolledJsonReaderCodeAction, pattern.GetMethodName);

        ctx.RegisterCodeFix(
            new CollapseAction(
                actionTitle,
                ct => CollapseAsync(ctx.Document, ctx.Span, ct),
                nameof(HandRolledJsonReaderFixProvider),
                generateFixAll: true),
            ctx.Diagnostics[0]);

        ctx.RegisterCodeFix(
            new GuidanceCodeAction(
                string.Format(AICopAnalyzers.HandRolledJsonReaderGuidanceAction, pattern.GetMethodName),
                nameof(HandRolledJsonReaderFixProvider) + "_Guidance",
                ctx.Document),
            ctx.Diagnostics[0]);
    }

    private static async Task<Document> CollapseAsync(
        Document document, TextSpan diagnosticSpan, CancellationToken ct)
    {
        var root = await document.GetSyntaxRootAsync(ct).ConfigureAwait(false);
        if (root is null)
            return document;

        if (!TryExtractPattern(root, diagnosticSpan, out var pattern) || pattern is null)
            return document;

        var sourceText = await document.GetTextAsync(ct).ConfigureAwait(false);

        // Preserve the indentation of the first line of the replaced region.
        var firstLine = sourceText.Lines.GetLineFromPosition(pattern.ReplacementSpan.Start);
        var firstLineText = sourceText.GetSubText(firstLine.Span).ToString();
        var indent = new string(firstLineText.TakeWhile(char.IsWhiteSpace).ToArray());

        // Replace whole lines so the collapse doesn't leave orphan blank lines.
        var fullSpan = TextSpan.FromBounds(
            firstLine.Start,
            sourceText.Lines.GetLineFromPosition(pattern.ReplacementSpan.End).EndIncludingLineBreak);

        var replacement = indent + pattern.ReplacementText + Environment.NewLine;

        return document.WithText(sourceText.WithChanges(new TextChange(fullSpan, replacement)));
    }

    private static bool TryExtractPattern(
        SyntaxNode root,
        TextSpan diagnosticSpan,
        out CollapsePattern? pattern)
    {
        pattern = null;

        var node = root.FindNode(diagnosticSpan);
        var anchorIf = node.FirstAncestorOrSelf<IfStatementSyntax>();
        if (anchorIf is null)
            return false;

        if (anchorIf.ElseStatement is not null)
            return false;
        if (anchorIf.Condition is null)
            return false;

        // Try Shape A — nested form: 'if Get(...) then [ if not IsNull then ] <assignment>'
        if (TryGetCallFromCondition(anchorIf.Condition, out var nestedGet, out var nestedToken)
            && nestedGet is not null
            && TryUnwrapToAssignment(anchorIf.Statement, nestedToken, out var nestedAssignment)
            && nestedAssignment is not null
            && TryReadAccessorFromExpression(nestedAssignment.Source, nestedToken, out var nestedAccessor))
        {
            var getMethodName = ResolveGetMethodName(nestedAccessor);
            if (getMethodName is null)
                return false;

            var jsonObjReceiver = ExtractReceiver(nestedGet);
            var keyArgument = nestedGet.ArgumentList.Arguments[0].ToString().Trim();
            var lhs = nestedAssignment.Target.ToString().Trim();

            pattern = new CollapsePattern(
                anchorIf.Span,
                $"{lhs} := {jsonObjReceiver}.{getMethodName}({keyArgument}, true);",
                getMethodName);
            return true;
        }

        // Try Shape B — guard form: 'if not Get(...) then exit;'
        if (TryMatchGuardShape(anchorIf, out var guardGet, out var guardToken, out var terminalStmt)
            && guardGet is not null
            && terminalStmt is not null)
        {
            if (!TryComposeGuardReplacement(guardGet, guardToken, terminalStmt, out var replacementText, out var getMethodName))
                return false;

            var span = TextSpan.FromBounds(anchorIf.Span.Start, terminalStmt.Span.End);
            pattern = new CollapsePattern(span, replacementText, getMethodName);
            return true;
        }

        return false;
    }

    private static bool TryGetCallFromCondition(
        CodeExpressionSyntax condition,
        out InvocationExpressionSyntax? getInvocation,
        out string tokenName)
    {
        getInvocation = null;
        tokenName = string.Empty;

        if (condition is not InvocationExpressionSyntax invocation)
            return false;
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return false;
        if (!string.Equals(memberAccess.Name.Identifier.ValueText, "Get", StringComparison.OrdinalIgnoreCase))
            return false;
        if (invocation.ArgumentList.Arguments.Count != 2)
            return false;
        if (invocation.ArgumentList.Arguments[1] is not IdentifierNameSyntax tokenIdentArg)
            return false;

        getInvocation = invocation;
        tokenName = tokenIdentArg.Identifier.ValueText;
        return !string.IsNullOrEmpty(tokenName);
    }

    private static bool TryUnwrapToAssignment(
        SyntaxNode? body, string tokenName, out AssignmentStatementSyntax? assignment)
    {
        assignment = null;
        if (body is null)
            return false;

        if (body is IfStatementSyntax guard
            && guard.ElseStatement is null
            && IsNotIsNullCondition(guard.Condition, tokenName))
        {
            assignment = guard.Statement as AssignmentStatementSyntax;
            return assignment is not null;
        }

        assignment = body as AssignmentStatementSyntax;
        return assignment is not null;
    }

    private static bool TryMatchGuardShape(
        IfStatementSyntax anchor,
        out InvocationExpressionSyntax? getInvocation,
        out string tokenName,
        out SyntaxNode? terminalStatement)
    {
        getInvocation = null;
        tokenName = string.Empty;
        terminalStatement = null;

        if (anchor.Condition is null
            || !string.Equals(anchor.Condition.Kind.ToString(), "UnaryNotExpression", StringComparison.Ordinal))
            return false;

        var notOperand = anchor.Condition.ChildNodes().OfType<CodeExpressionSyntax>().FirstOrDefault();
        if (notOperand is null)
            return false;
        if (!TryGetCallFromCondition(notOperand, out var candidateGet, out var candidateToken) || candidateGet is null)
            return false;
        if (!IsExitStatement(anchor.Statement))
            return false;
        if (anchor.Parent is null)
            return false;

        var siblings = anchor.Parent.ChildNodes().OfType<SyntaxNode>().ToList();
        var anchorIndex = siblings.IndexOf(anchor);
        if (anchorIndex < 0 || anchorIndex + 1 >= siblings.Count)
            return false;

        // Skip any sequence of safety-check early-returns (IsValue / IsObject /
        // IsArray / IsNull / AsValue().IsNull()) on the same token — Get<Type>(Key, true)
        // already performs those checks internally.
        var nextIndex = anchorIndex + 1;
        while (nextIndex < siblings.Count
            && IsRecognisedSafetyGuard(siblings[nextIndex], candidateToken))
        {
            nextIndex++;
        }
        if (nextIndex >= siblings.Count)
            return false;

        getInvocation = candidateGet;
        tokenName = candidateToken;
        terminalStatement = siblings[nextIndex];
        return true;
    }

    /// <summary>
    /// Returns true when the statement is a single-line early-return guard whose
    /// condition restates a check that JsonObject.Get&lt;Type&gt;(Key, true) already
    /// performs. Kept in sync with HandRolledJsonReader.IsRecognisedSafetyGuard.
    /// </summary>
    private static bool IsRecognisedSafetyGuard(SyntaxNode statement, string tokenName)
    {
        if (statement is not IfStatementSyntax guard)
            return false;
        if (guard.ElseStatement is not null)
            return false;
        if (!IsExitStatement(guard.Statement))
            return false;
        if (guard.Condition is null)
            return false;

        if (IsBareIsNullCondition(guard.Condition, tokenName))
            return true;
        if (IsBareIsNullOnToken(guard.Condition, tokenName))
            return true;
        return IsNegatedTokenShapeCheck(guard.Condition, tokenName);
    }

    private static bool IsBareIsNullOnToken(CodeExpressionSyntax condition, string tokenName)
    {
        if (condition is not InvocationExpressionSyntax invocation)
            return false;
        if (invocation.ArgumentList.Arguments.Count != 0)
            return false;
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return false;
        if (!string.Equals(memberAccess.Name.Identifier.ValueText, "IsNull", StringComparison.OrdinalIgnoreCase))
            return false;
        if (memberAccess.Expression is not IdentifierNameSyntax tokenIdent)
            return false;
        return string.Equals(tokenIdent.Identifier.ValueText, tokenName, StringComparison.OrdinalIgnoreCase);
    }

    private static readonly HashSet<string> NegatedTokenShapeChecks =
        new(StringComparer.OrdinalIgnoreCase) { "IsValue", "IsObject", "IsArray" };

    private static bool IsNegatedTokenShapeCheck(CodeExpressionSyntax condition, string tokenName)
    {
        if (!string.Equals(condition.Kind.ToString(), "UnaryNotExpression", StringComparison.Ordinal))
            return false;
        var operand = condition.ChildNodes().OfType<CodeExpressionSyntax>().FirstOrDefault();
        if (operand is not InvocationExpressionSyntax invocation)
            return false;
        if (invocation.ArgumentList.Arguments.Count != 0)
            return false;
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return false;
        var methodName = memberAccess.Name.Identifier.ValueText;
        if (string.IsNullOrEmpty(methodName) || !NegatedTokenShapeChecks.Contains(methodName))
            return false;
        if (memberAccess.Expression is not IdentifierNameSyntax tokenIdent)
            return false;
        return string.Equals(tokenIdent.Identifier.ValueText, tokenName, StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryComposeGuardReplacement(
        InvocationExpressionSyntax getInvocation,
        string tokenName,
        SyntaxNode terminalStatement,
        out string replacementText,
        out string getMethodName)
    {
        replacementText = string.Empty;
        getMethodName = string.Empty;

        var jsonObjReceiver = ExtractReceiver(getInvocation);
        var keyArgument = getInvocation.ArgumentList.Arguments[0].ToString().Trim();

        // exit(<accessor-chain>) — replace with: exit(<jsonObj>.<getMethod>(<key>, true));
        if (string.Equals(terminalStatement.Kind.ToString(), "ExitStatement", StringComparison.Ordinal))
        {
            var argument = terminalStatement.ChildNodes().OfType<CodeExpressionSyntax>().FirstOrDefault();
            if (argument is null)
                return false;
            if (!TryReadAccessorFromExpression(argument, tokenName, out var accessor))
                return false;
            var methodName = ResolveGetMethodName(accessor);
            if (methodName is null)
                return false;

            getMethodName = methodName;
            replacementText = $"exit({jsonObjReceiver}.{methodName}({keyArgument}, true));";
            return true;
        }

        // <lhs> := <accessor-chain>; — replace with: <lhs> := <jsonObj>.<getMethod>(<key>, true);
        if (terminalStatement is AssignmentStatementSyntax assignment)
        {
            if (!TryReadAccessorFromExpression(assignment.Source, tokenName, out var accessor))
                return false;
            var methodName = ResolveGetMethodName(accessor);
            if (methodName is null)
                return false;

            getMethodName = methodName;
            var lhs = assignment.Target.ToString().Trim();
            replacementText = $"{lhs} := {jsonObjReceiver}.{methodName}({keyArgument}, true);";
            return true;
        }

        return false;
    }

    private static bool TryReadAccessorFromExpression(
        CodeExpressionSyntax? expression, string tokenName, out string accessorName)
    {
        accessorName = string.Empty;
        if (expression is not InvocationExpressionSyntax terminalCall)
            return false;
        if (terminalCall.ArgumentList.Arguments.Count != 0)
            return false;
        if (terminalCall.Expression is not MemberAccessExpressionSyntax terminalMember)
            return false;

        var asName = terminalMember.Name.Identifier.ValueText;
        if (string.IsNullOrEmpty(asName))
            return false;

        if (HandRolledJsonReader.ContainerAccessorToGetMethod.ContainsKey(asName)
            && terminalMember.Expression is IdentifierNameSyntax tokenIdent
            && string.Equals(tokenIdent.Identifier.ValueText, tokenName, StringComparison.OrdinalIgnoreCase))
        {
            accessorName = asName;
            return true;
        }

        if (HandRolledJsonReader.PrimitiveAccessorToGetMethod.ContainsKey(asName)
            && IsAsValueOn(terminalMember.Expression, tokenName))
        {
            accessorName = asName;
            return true;
        }

        return false;
    }

    private static string ExtractReceiver(InvocationExpressionSyntax getInvocation)
    {
        if (getInvocation.Expression is MemberAccessExpressionSyntax memberAccess)
            return memberAccess.Expression.ToString().Trim();
        return getInvocation.Expression.ToString().Trim();
    }

    private static bool IsExitStatement(SyntaxNode? statement)
    {
        if (statement is null)
            return false;
        return string.Equals(statement.Kind.ToString(), "ExitStatement", StringComparison.Ordinal);
    }

    private static bool IsAsValueOn(CodeExpressionSyntax expr, string tokenName)
    {
        if (expr is not InvocationExpressionSyntax invocation)
            return false;
        if (invocation.ArgumentList.Arguments.Count != 0)
            return false;
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return false;
        if (!string.Equals(memberAccess.Name.Identifier.ValueText, "AsValue", StringComparison.OrdinalIgnoreCase))
            return false;
        if (memberAccess.Expression is not IdentifierNameSyntax tokenIdent)
            return false;
        return string.Equals(tokenIdent.Identifier.ValueText, tokenName, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsNotIsNullCondition(CodeExpressionSyntax? condition, string tokenName)
    {
        if (condition is null)
            return false;
        if (!string.Equals(condition.Kind.ToString(), "UnaryNotExpression", StringComparison.Ordinal))
            return false;
        var operand = condition.ChildNodes().OfType<CodeExpressionSyntax>().FirstOrDefault();
        return operand is not null && IsBareIsNullCondition(operand, tokenName);
    }

    private static bool IsBareIsNullCondition(CodeExpressionSyntax? condition, string tokenName)
    {
        if (condition is not InvocationExpressionSyntax invocation)
            return false;
        if (invocation.ArgumentList.Arguments.Count != 0)
            return false;
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return false;
        if (!string.Equals(memberAccess.Name.Identifier.ValueText, "IsNull", StringComparison.OrdinalIgnoreCase))
            return false;
        return IsAsValueOn(memberAccess.Expression, tokenName);
    }

    private static string? ResolveGetMethodName(string accessor)
    {
        if (HandRolledJsonReader.PrimitiveAccessorToGetMethod.TryGetValue(accessor, out var primitive))
            return primitive;
        if (HandRolledJsonReader.ContainerAccessorToGetMethod.TryGetValue(accessor, out var container))
            return container;
        return null;
    }

    private sealed record CollapsePattern(
        TextSpan ReplacementSpan,
        string ReplacementText,
        string GetMethodName);
}
