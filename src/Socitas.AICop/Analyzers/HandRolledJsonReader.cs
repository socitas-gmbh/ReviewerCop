using System.Collections.Immutable;
using Socitas.ReviewerCop.Common.Extensions;
using Socitas.ReviewerCop.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace Socitas.AICop.Analyzers;

/// <summary>
/// AI0010 – Replace hand-rolled JsonObject reader chains with JsonObject.Get&lt;Type&gt;(Key, true).
/// Detects two equivalent shapes:
///
/// Nested form (optionally null-guarded):
///   if JsonObj.Get(Key, Token) then
///       [ if not Token.AsValue().IsNull() then ]
///           Lhs := Token.AsValue().As&lt;T&gt;();
///
/// Guard / early-return form (typical wrapper procedure shape):
///   if not JsonObj.Get(Key, Token) then exit;
///   [ if Token.AsValue().IsNull() then exit; ]
///   exit(Token.AsValue().As&lt;T&gt;());           // or: Lhs := Token.AsValue().As&lt;T&gt;();
///
/// Both also cover container shortcuts via Token.AsObject() / Token.AsArray().
/// </summary>
[DiagnosticAnalyzer]
public sealed class HandRolledJsonReader : DiagnosticAnalyzer
{
    internal static readonly ImmutableDictionary<string, string> PrimitiveAccessorToGetMethod =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["AsText"] = "GetText",
            ["AsBoolean"] = "GetBoolean",
            ["AsInteger"] = "GetInteger",
            ["AsBigInteger"] = "GetBigInteger",
            ["AsDecimal"] = "GetDecimal",
            ["AsByte"] = "GetByte",
            ["AsChar"] = "GetChar",
            ["AsDate"] = "GetDate",
            ["AsDateTime"] = "GetDateTime",
            ["AsTime"] = "GetTime",
            ["AsDuration"] = "GetDuration",
        }.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase);

    internal static readonly ImmutableDictionary<string, string> ContainerAccessorToGetMethod =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["AsObject"] = "GetObject",
            ["AsArray"] = "GetArray",
        }.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase);

    // Token shape checks that the BC JsonObject.Get<Type>(Key, true) call performs
    // internally. When they appear between the Get-anchor and the terminal accessor
    // as early-return guards, they are redundant and may be skipped.
    private static readonly HashSet<string> NegatedTokenShapeChecks =
        new(StringComparer.OrdinalIgnoreCase) { "IsValue", "IsObject", "IsArray" };

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.HandRolledJsonReader);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(
            CheckIfStatement,
            EnumProvider.SyntaxKind.IfStatement);

    private static void CheckIfStatement(SyntaxNodeAnalysisContext ctx)
    {
        if (ctx.IsObsolete())
            return;

        if (ctx.Node is not IfStatementSyntax ifStmt)
            return;

        // An else branch carries independent semantics — refuse to collapse.
        if (ifStmt.ElseStatement is not null)
            return;

        if (ifStmt.Condition is null)
            return;

        // Shape A — nested form: 'if Get(...) then [ if not IsNull then ] <assignment>'
        if (TryMatchGetCall(ifStmt.Condition, out var nestedGet, out var nestedToken) && nestedGet is not null
            && TryFindTerminalAccessor(ifStmt.Statement, nestedToken, out var nestedAccessor))
        {
            var methodName = ResolveGetMethodName(nestedAccessor);
            if (methodName is not null)
                ctx.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.HandRolledJsonReader,
                    nestedGet.GetLocation(),
                    methodName));
            return;
        }

        // Shape B — guard / early-return form: 'if not Get(...) then exit;'
        if (TryMatchGuardForm(ifStmt, out var guardGet, out var guardAccessor) && guardGet is not null)
        {
            var methodName = ResolveGetMethodName(guardAccessor);
            if (methodName is not null)
                ctx.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.HandRolledJsonReader,
                    guardGet.GetLocation(),
                    methodName));
        }
    }

    /// <summary>
    /// Matches the 'if not JsonObj.Get(Key, Token) then exit;' early-return shape,
    /// optionally followed by 'if Token.AsValue().IsNull() then exit;', and ending
    /// with a statement that uses Token.AsValue().As&lt;T&gt;() (or Token.AsObject /
    /// AsArray) as its value — typically 'exit(...)' or an assignment.
    /// </summary>
    private static bool TryMatchGuardForm(
        IfStatementSyntax anchor,
        out InvocationExpressionSyntax? getInvocation,
        out string accessorName)
    {
        getInvocation = null;
        accessorName = string.Empty;

        // Condition must be 'not <getInvocation>'.
        if (anchor.Condition is null)
            return false;
        if (!string.Equals(anchor.Condition.Kind.ToString(), "UnaryNotExpression", StringComparison.Ordinal))
            return false;
        var notOperand = anchor.Condition.ChildNodes().OfType<CodeExpressionSyntax>().FirstOrDefault();
        if (notOperand is null)
            return false;
        if (!TryMatchGetCall(notOperand, out var candidateGet, out var tokenName) || candidateGet is null)
            return false;

        // Body must be 'exit' or 'exit(<defaultLiteral>)'.
        if (!IsExitStatement(anchor.Statement))
            return false;

        // The anchor must sit in a block whose subsequent statements complete the
        // pattern. The parent of the anchor IfStatement is the enclosing block.
        if (anchor.Parent is null)
            return false;

        var siblings = anchor.Parent.ChildNodes().OfType<SyntaxNode>().ToList();
        var anchorIndex = siblings.IndexOf(anchor);
        if (anchorIndex < 0 || anchorIndex + 1 >= siblings.Count)
            return false;

        // Skip any sequence of recognised safety-check early-returns on the same
        // token (IsValue / IsObject / IsArray / IsNull / AsValue().IsNull()) — each
        // one re-checks what JsonObject.Get<Type>(Key, true) already does internally.
        var nextIndex = anchorIndex + 1;
        while (nextIndex < siblings.Count
            && IsRecognisedSafetyGuard(siblings[nextIndex], tokenName))
        {
            nextIndex++;
        }
        if (nextIndex >= siblings.Count)
            return false;
        var next = siblings[nextIndex];

        // The next statement must use Token.AsValue().As<T>() or Token.As{Object,Array}().
        if (!TryFindTerminalAccessorInStatement(next, tokenName, out accessorName))
            return false;

        getInvocation = candidateGet;
        return true;
    }

    /// <summary>
    /// Returns true when the statement is 'exit;' or 'exit(&lt;literal&gt;)'.
    /// Any expression argument is allowed — what matters for the pattern is that
    /// the procedure terminates, not what value it returns.
    /// </summary>
    private static bool IsExitStatement(SyntaxNode? statement)
    {
        if (statement is null)
            return false;
        return string.Equals(statement.Kind.ToString(), "ExitStatement", StringComparison.Ordinal);
    }

    /// <summary>
    /// Returns true when the statement is a single-line early-return guard whose
    /// condition restates a check that JsonObject.Get&lt;Type&gt;(Key, true) already
    /// performs: IsValue / IsObject / IsArray (negated), or IsNull / AsValue().IsNull()
    /// (un-negated). Such guards are safely removable when the chain collapses to
    /// the shortcut Get method.
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

    /// <summary>
    /// Matches the bare-on-token null check '&lt;token&gt;.IsNull()'.
    /// </summary>
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

    /// <summary>
    /// Matches 'not &lt;token&gt;.IsValue()' / 'not &lt;token&gt;.IsObject()' /
    /// 'not &lt;token&gt;.IsArray()'.
    /// </summary>
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

    /// <summary>
    /// Matches the un-negated null-check '&lt;token&gt;.AsValue().IsNull()'.
    /// </summary>
    private static bool IsBareIsNullCondition(CodeExpressionSyntax? condition, string tokenName)
    {
        if (condition is null)
            return false;
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

    /// <summary>
    /// Walks the given statement looking for a Token.AsValue().As&lt;T&gt;() or
    /// Token.As{Object,Array}() invocation. Used by the guard form to inspect
    /// the terminating statement (exit, assignment, etc.).
    /// </summary>
    private static bool TryFindTerminalAccessorInStatement(
        SyntaxNode statement, string tokenName, out string accessorName)
    {
        accessorName = string.Empty;

        foreach (var invocation in statement.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.ArgumentList.Arguments.Count != 0)
                continue;
            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
                continue;

            var asName = memberAccess.Name.Identifier.ValueText;
            if (string.IsNullOrEmpty(asName))
                continue;

            if (ContainerAccessorToGetMethod.ContainsKey(asName)
                && memberAccess.Expression is IdentifierNameSyntax tokenIdent
                && string.Equals(tokenIdent.Identifier.ValueText, tokenName, StringComparison.OrdinalIgnoreCase))
            {
                accessorName = asName;
                return true;
            }

            if (PrimitiveAccessorToGetMethod.ContainsKey(asName)
                && IsAsValueOn(memberAccess.Expression, tokenName))
            {
                accessorName = asName;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Matches the if-condition shape '&lt;jsonObjExpr&gt;.Get(&lt;key&gt;, &lt;tokenIdent&gt;)'.
    /// </summary>
    private static bool TryMatchGetCall(
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

        if (invocation.ArgumentList.Arguments[1] is not IdentifierNameSyntax tokenArg)
            return false;

        getInvocation = invocation;
        tokenName = tokenArg.Identifier.ValueText;
        return !string.IsNullOrWhiteSpace(tokenName);
    }

    /// <summary>
    /// Walks the if-body, optionally through one null-guard nested if, looking for a
    /// final assignment whose RHS is the terminal As&lt;Type&gt; call on tokenName.
    /// Returns the accessor identifier (e.g. "AsText", "AsObject") via accessorName.
    /// </summary>
    private static bool TryFindTerminalAccessor(
        SyntaxNode? body, string tokenName, out string accessorName)
    {
        accessorName = string.Empty;
        if (body is null)
            return false;

        // Allow exactly one level of null-guard nesting:
        //   if not Token.AsValue().IsNull() then <assignment>;
        if (body is IfStatementSyntax guard
            && guard.ElseStatement is null
            && IsNotIsNullGuard(guard.Condition, tokenName))
        {
            body = guard.Statement;
        }

        if (body is not AssignmentStatementSyntax assignment)
            return false;

        if (assignment.Source is not InvocationExpressionSyntax terminalCall)
            return false;

        if (terminalCall.Expression is not MemberAccessExpressionSyntax memberAccess)
            return false;

        var asName = memberAccess.Name.Identifier.ValueText;
        if (string.IsNullOrEmpty(asName))
            return false;

        // Container shortcuts: <tokenIdent>.AsObject() / <tokenIdent>.AsArray()
        if (ContainerAccessorToGetMethod.ContainsKey(asName))
        {
            if (memberAccess.Expression is IdentifierNameSyntax tokenIdent
                && string.Equals(tokenIdent.Identifier.ValueText, tokenName, StringComparison.OrdinalIgnoreCase)
                && terminalCall.ArgumentList.Arguments.Count == 0)
            {
                accessorName = asName;
                return true;
            }
            return false;
        }

        // Primitive shortcuts: <tokenIdent>.AsValue().As<Type>()
        if (PrimitiveAccessorToGetMethod.ContainsKey(asName))
        {
            if (terminalCall.ArgumentList.Arguments.Count != 0)
                return false;
            if (!IsAsValueOn(memberAccess.Expression, tokenName))
                return false;

            accessorName = asName;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Returns true when the expression has the shape '&lt;tokenIdent&gt;.AsValue()'.
    /// </summary>
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

    /// <summary>
    /// Matches 'not &lt;tokenIdent&gt;.AsValue().IsNull()'.
    /// </summary>
    private static bool IsNotIsNullGuard(CodeExpressionSyntax condition, string tokenName)
    {
        // AL parses 'not X' as a UnaryNotExpression with the operand as its first
        // child expression. The Kind is checked by name to avoid a hard dependency
        // on a concrete UnaryNotExpressionSyntax type that may not be exposed.
        if (!string.Equals(condition.Kind.ToString(), "UnaryNotExpression", StringComparison.Ordinal))
            return false;

        var operand = condition.ChildNodes().OfType<CodeExpressionSyntax>().FirstOrDefault();
        if (operand is null)
            return false;

        if (operand is not InvocationExpressionSyntax invocation)
            return false;
        if (invocation.ArgumentList.Arguments.Count != 0)
            return false;
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return false;
        if (!string.Equals(memberAccess.Name.Identifier.ValueText, "IsNull", StringComparison.OrdinalIgnoreCase))
            return false;
        return IsAsValueOn(memberAccess.Expression, tokenName);
    }

    private static string? ResolveGetMethodName(string accessorName)
    {
        if (PrimitiveAccessorToGetMethod.TryGetValue(accessorName, out var primitive))
            return primitive;
        if (ContainerAccessorToGetMethod.TryGetValue(accessorName, out var container))
            return container;
        return null;
    }
}
