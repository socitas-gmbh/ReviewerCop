using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using Socitas.ReviewerCop.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Text;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;

namespace Socitas.AICop.CodeFixes;

/// <summary>
/// CC0011 – Quick fix: remove Caption/ToolTip from the page field, or move it to the table field
/// (same project) or to a table extension (cross-project).
/// </summary>
[CodeFixProvider(nameof(CaptionTooltipOnPageFixProvider))]
public sealed class CaptionTooltipOnPageFixProvider : CodeFixProvider
{
    private sealed class RemoveAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.QuickFix;
        public override bool SupportsFixAll { get; }
        public override string? FixAllSingleInstanceTitle => string.Empty;
        public override string? FixAllTitle => Title;

        public RemoveAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument,
            string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            SupportsFixAll = generateFixAll;
        }
    }

    private sealed class MoveToTableAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.QuickFix;
        public override bool SupportsFixAll => false;
        public override string? FixAllSingleInstanceTitle => string.Empty;
        public override string? FixAllTitle => Title;

        public MoveToTableAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument,
            string equivalenceKey)
            : base(title, createChangedDocument, equivalenceKey)
        {
        }
    }

    private sealed class MoveToTableExtensionAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.QuickFix;
        public override bool SupportsFixAll => false;
        public override string? FixAllSingleInstanceTitle => string.Empty;
        public override string? FixAllTitle => Title;

        public MoveToTableExtensionAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument,
            string equivalenceKey)
            : base(title, createChangedDocument, equivalenceKey)
        {
        }
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticIds.CaptionTooltipOnPage);

    public sealed override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext ctx)
    {
        var syntaxRoot = await ctx.Document.GetSyntaxRootAsync(ctx.CancellationToken).ConfigureAwait(false);
        if (syntaxRoot is null)
            return;

        var token = syntaxRoot.FindToken(ctx.Span.Start);
        var propertySyntax = token.Parent?.FirstAncestorOrSelf<PropertySyntax>();
        if (propertySyntax is null)
            return;

        var propertyName = token.ValueText ?? string.Empty;

        // Always offer "Remove from page field"
        ctx.RegisterCodeFix(
            new RemoveAction(
                string.Format(AICopAnalyzers.CaptionTooltipOnPageRemoveCodeAction, propertyName),
                ct => RemovePropertyAsync(ctx.Document, propertySyntax, ct),
                nameof(CaptionTooltipOnPageFixProvider) + "_Remove_" + propertyName,
                generateFixAll: true),
            ctx.Diagnostics[0]);

        // Resolve the related table field
        var pageFieldNode = Analyzers.CaptionTooltipOnPage.GetContainingPageFieldNode(token);
        if (pageFieldNode is null)
            return;

        var semanticModel = await ctx.Document.GetSemanticModelAsync(ctx.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return;

        var controlSymbol = semanticModel.GetDeclaredSymbol(pageFieldNode) as IControlSymbol;
        var relatedField = controlSymbol?.RelatedFieldSymbol;
        if (relatedField is null)
            return;

        var compilation = semanticModel.Compilation;
        var tableFieldNode = Analyzers.CaptionTooltipOnPage.GetFieldSyntaxNode(relatedField, compilation, ctx.CancellationToken);

        if (tableFieldNode is not null)
        {
            // Don't offer move if the table already has the property
            if (Analyzers.CaptionTooltipOnPage.FieldNodeHasProperty(tableFieldNode, propertyName))
                return;

            var tableFilePath = tableFieldNode.SyntaxTree?.FilePath;
            if (tableFilePath is null)
                return;

            var solution = ctx.Document.Project.Solution;
            var tableDocumentIds = solution.GetDocumentIdsWithFilePath(tableFilePath);

            var firstTableDoc = tableDocumentIds.IsEmpty ? null : solution.GetDocument(tableDocumentIds[0]);
            bool isSameProject = firstTableDoc is not null &&
                firstTableDoc.Project.Id == ctx.Document.Project.Id;

            if (isSameProject)
            {
                // Same project: move property directly to the table field
                var tableDocumentId = tableDocumentIds[0];
                ctx.RegisterCodeFix(
                    new MoveToTableAction(
                        string.Format(AICopAnalyzers.CaptionTooltipOnPageMoveCodeAction, propertyName),
                        ct => MovePropertyToTableAsync(ctx.Document, propertySyntax, tableDocumentId, tableFieldNode, ct),
                        nameof(CaptionTooltipOnPageFixProvider) + "_Move_" + propertyName),
                    ctx.Diagnostics[0]);
                return;
            }

            // Cross-project with source available: extract names from syntax node
            var tableName = GetTableName(tableFieldNode);
            var fieldName = GetFieldName(tableFieldNode);
            if (tableName is not null && fieldName is not null)
            {
                var capturedDoc = ctx.Document;
                var capturedProp = propertySyntax;
                ctx.RegisterCodeFix(
                    new MoveToTableExtensionAction(
                        string.Format(AICopAnalyzers.CaptionTooltipOnPageMoveExtCodeAction, propertyName),
                        ct => MovePropertyToTableExtensionAsync(capturedDoc, capturedProp, tableName, fieldName, ct),
                        nameof(CaptionTooltipOnPageFixProvider) + "_MoveExt_" + propertyName),
                    ctx.Diagnostics[0]);
            }
        }
        else
        {
            // Field is from an external package: use reflection to get field/table names
            var fieldName = Analyzers.CaptionTooltipOnPage.TryGetName(relatedField);
            var tableName = Analyzers.CaptionTooltipOnPage.TryGetContainingName(relatedField);
            if (tableName is not null && fieldName is not null)
            {
                var capturedDoc = ctx.Document;
                var capturedProp = propertySyntax;
                ctx.RegisterCodeFix(
                    new MoveToTableExtensionAction(
                        string.Format(AICopAnalyzers.CaptionTooltipOnPageMoveExtCodeAction, propertyName),
                        ct => MovePropertyToTableExtensionAsync(capturedDoc, capturedProp, tableName, fieldName, ct),
                        nameof(CaptionTooltipOnPageFixProvider) + "_MoveExt_" + propertyName),
                    ctx.Diagnostics[0]);
            }
        }
    }

    // ── Remove from page ──────────────────────────────────────────────────────

    private static async Task<Document> RemovePropertyAsync(
        Document document, PropertySyntax propertySyntax, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
            return document;

        if (propertySyntax.Parent is not PropertyListSyntax propertyList)
            return document;

        var newProperties = propertyList.Properties.Remove(propertySyntax);
        var newPropertyList = propertyList.WithProperties(newProperties);
        var newRoot = root.ReplaceNode(propertyList, newPropertyList);
        return document.WithSyntaxRoot(newRoot);
    }

    // ── Move to table (same project) ──────────────────────────────────────────

    private static async Task<Document> MovePropertyToTableAsync(
        Document pageDocument,
        PropertySyntax propertySyntax,
        DocumentId tableDocumentId,
        SyntaxNode tableFieldNode,
        CancellationToken cancellationToken)
    {
        var solution = pageDocument.Project.Solution;

        var tableDocument = solution.GetDocument(tableDocumentId);
        if (tableDocument is null)
            return pageDocument;

        var tableRoot = await tableDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (tableRoot is null)
            return pageDocument;

        var tableFieldInRoot = tableRoot.FindNode(tableFieldNode.Span);
        if (tableFieldInRoot is null)
            return pageDocument;

        var tablePropertyList = tableFieldInRoot
            .DescendantNodesAndSelf()
            .OfType<PropertyListSyntax>()
            .FirstOrDefault();

        if (tablePropertyList is null)
            return pageDocument;

        var newTableProperties = tablePropertyList.Properties.Add(propertySyntax);
        var newTablePropertyList = tablePropertyList.WithProperties(newTableProperties);
        var newTableRoot = tableRoot.ReplaceNode(tablePropertyList, newTablePropertyList);

        var updatedSolution = solution.WithDocumentSyntaxRoot(tableDocumentId, newTableRoot);

        return await RemovePropertyFromPageInSolutionAsync(
            updatedSolution, pageDocument.Id, propertySyntax, cancellationToken)
            ?? pageDocument;
    }

    // ── Move to table extension (cross-project) ───────────────────────────────

    private static async Task<Document> MovePropertyToTableExtensionAsync(
        Document pageDocument,
        PropertySyntax propertySyntax,
        string tableName,
        string fieldName,
        CancellationToken ct)
    {
        var solution = pageDocument.Project.Solution;
        var project = pageDocument.Project;
        var propertyText = propertySyntax.ToFullString().Trim();

        // Search current project for an existing table extension for this table
        var (tableExtDocId, tableExtNode) =
            await FindTableExtensionForTableAsync(solution, project, tableName, ct).ConfigureAwait(false);

        Solution updatedSolution;

        if (tableExtDocId is not null && tableExtNode is not null)
        {
            var tableExtDoc = solution.GetDocument(tableExtDocId);
            if (tableExtDoc is null)
                return pageDocument;

            var tableExtText = await tableExtDoc.GetTextAsync(ct).ConfigureAwait(false);
            var fullText = tableExtText.ToString();

            var nodeSpan = tableExtNode.Span;
            var nodeText = fullText.Substring(nodeSpan.Start, nodeSpan.Length);
            var quotedField = QuoteIfNeeded(fieldName);

            int modifyPos = FindModifyBlockPosition(nodeText, fieldName);

            SourceText newExtText;
            if (modifyPos >= 0)
            {
                // Insert property into the existing modify block after its opening '{'
                int bracePos = nodeText.IndexOf('{', modifyPos);
                if (bracePos < 0)
                    return pageDocument;

                int insertPos = nodeSpan.Start + bracePos + 1;
                var insertion = "\r\n            " + propertyText;
                newExtText = tableExtText.WithChanges(new TextChange(new TextSpan(insertPos, 0), insertion));
            }
            else
            {
                // No modify block — find the fields section and insert one
                int fieldsKeywordPos = nodeText.IndexOf("fields", StringComparison.OrdinalIgnoreCase);
                if (fieldsKeywordPos >= 0)
                {
                    int fieldsOpenBrace = nodeText.IndexOf('{', fieldsKeywordPos);
                    if (fieldsOpenBrace < 0)
                        return pageDocument;

                    int fieldsCloseBrace = FindMatchingCloseBrace(nodeText, fieldsOpenBrace);
                    if (fieldsCloseBrace < 0)
                        return pageDocument;

                    int insertPos = nodeSpan.Start + fieldsCloseBrace;
                    var modifyBlock = $"        modify({quotedField})\r\n        {{\r\n            {propertyText}\r\n        }}\r\n    ";
                    newExtText = tableExtText.WithChanges(new TextChange(new TextSpan(insertPos, 0), modifyBlock));
                }
                else
                {
                    // No fields section at all — insert one before the closing brace of the extension
                    int extCloseBrace = FindMatchingCloseBrace(nodeText, nodeText.IndexOf('{'));
                    if (extCloseBrace < 0)
                        return pageDocument;

                    int insertPos = nodeSpan.Start + extCloseBrace;
                    var fieldsBlock = $"    fields\r\n    {{\r\n        modify({quotedField})\r\n        {{\r\n            {propertyText}\r\n        }}\r\n    }}\r\n";
                    newExtText = tableExtText.WithChanges(new TextChange(new TextSpan(insertPos, 0), fieldsBlock));
                }
            }

            var updatedTableExtDoc = tableExtDoc.WithText(newExtText);
            updatedSolution = updatedTableExtDoc.Project.Solution;
        }
        else
        {
            // No existing table extension — write a new file directly to disk.
            // Solution.AddDocument() is not reliably supported in BC's workspace;
            // writing to disk lets the AL language server pick up the new file automatically.
            await WriteNewTableExtensionFileAsync(
                solution, project, pageDocument.FilePath, tableName, fieldName, propertyText, ct)
                .ConfigureAwait(false);

            // No solution-level changes needed; the property is only removed from the page.
            updatedSolution = solution;
        }

        return await RemovePropertyFromPageInSolutionAsync(
            updatedSolution, pageDocument.Id, propertySyntax, ct)
            ?? pageDocument;
    }

    // ── Name extraction helpers ───────────────────────────────────────────────

    /// <summary>
    /// Extracts the field name from a field declaration node: field(ID; FieldName; Type).
    /// Returns the first identifier after the first semicolon inside the parentheses.
    /// </summary>
    private static string? GetFieldName(SyntaxNode fieldNode)
    {
        bool inParens = false;
        bool afterFirstSemi = false;

        foreach (var token in fieldNode.DescendantTokens())
        {
            var text = token.ValueText;
            if (text is null)
                continue;

            if (!inParens)
            {
                if (text == "(")
                    inParens = true;
                continue;
            }

            if (!afterFirstSemi)
            {
                if (text == ";")
                    afterFirstSemi = true;
                continue;
            }

            // First non-semicolon token after the first semicolon → field name
            if (text == ";")
                return null; // hit second semicolon before finding an identifier
            if (token.Kind == SyntaxKind.IdentifierToken)
                return StripQuotes(text);
        }

        return null;
    }

    /// <summary>
    /// Walks up from the field node to its containing TableObject and extracts the table name
    /// (the token at position 2 in the header: table ID TableName).
    /// </summary>
    private static string? GetTableName(SyntaxNode fieldNode)
    {
        var node = fieldNode.Parent;
        while (node is not null)
        {
            if (node.Kind == EnumProvider.SyntaxKind.TableObject)
                return ExtractHeaderTokenAtIndex(node, 2);
            node = node.Parent;
        }
        return null;
    }

    /// <summary>
    /// Returns the ValueText of the token at position <paramref name="index"/> among the tokens
    /// that appear before the first '{' in the given node's descendant token stream.
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
                return StripQuotes(text);
            i++;
        }
        return null;
    }

    private static string StripQuotes(string name) =>
        name.Length >= 2 && name[0] == '"' && name[^1] == '"'
            ? name[1..^1]
            : name;

    private static string QuoteIfNeeded(string name) =>
        name.IndexOfAny([' ', '-', '.', '/', '\'']) >= 0
            ? $"\"{name}\""
            : name;

    // ── Table extension search ────────────────────────────────────────────────

    /// <summary>
    /// Searches all documents in the project for a tableextension that extends the given table.
    /// Returns (null, null) if none is found.
    /// </summary>
    private static async Task<(DocumentId?, SyntaxNode?)> FindTableExtensionForTableAsync(
        Solution solution, Project project, string tableName, CancellationToken ct)
    {
        foreach (var docId in project.DocumentIds)
        {
            var doc = solution.GetDocument(docId);
            if (doc is null)
                continue;

            var root = await doc.GetSyntaxRootAsync(ct).ConfigureAwait(false);
            if (root is null)
                continue;

            foreach (var node in root.DescendantNodesAndSelf())
            {
                if (node.Kind != EnumProvider.SyntaxKind.TableExtensionObject)
                    continue;

                // tableextension ID ExtName extends TableName { ... }
                // Header tokens at indices: 0=tableextension, 1=ID, 2=ExtName, 3=extends, 4=TableName
                var extendsName = ExtractHeaderTokenAtIndex(node, 4);
                if (extendsName is null)
                    continue;

                if (string.Equals(StripQuotes(extendsName), StripQuotes(tableName), StringComparison.OrdinalIgnoreCase))
                    return (docId, node);
            }
        }

        return (null, null);
    }

    // ── Text helpers for table extension insertion ────────────────────────────

    private static int FindModifyBlockPosition(string nodeText, string fieldName)
    {
        var patterns = new[]
        {
            $"modify(\"{fieldName}\")",
            $"modify({fieldName})"
        };

        foreach (var pattern in patterns)
        {
            int pos = nodeText.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
            if (pos >= 0)
                return pos;
        }

        return -1;
    }

    private static int FindMatchingCloseBrace(string text, int openBracePos)
    {
        if (openBracePos < 0 || openBracePos >= text.Length)
            return -1;

        int depth = 0;
        for (int i = openBracePos; i < text.Length; i++)
        {
            if (text[i] == '{')
                depth++;
            else if (text[i] == '}')
            {
                depth--;
                if (depth == 0)
                    return i;
            }
        }

        return -1;
    }

    // ── Create new table extension file ──────────────────────────────────────

    private static async Task WriteNewTableExtensionFileAsync(
        Solution solution,
        Project project,
        string? pageFilePath,
        string tableName,
        string fieldName,
        string propertyText,
        CancellationToken ct)
    {
        var nextId = await FindNextAvailableIdAsync(solution, project, ct).ConfigureAwait(false);
        var extName = $"{tableName} Ext";
        var quotedTable = QuoteIfNeeded(tableName);
        var quotedField = QuoteIfNeeded(fieldName);

        var safeName = string.Concat(tableName.Split(Path.GetInvalidFileNameChars()));
        var fileName = $"Tab{nextId}Ext-{safeName}.al";

        var content =
            $"tableextension {nextId} \"{extName}\" extends {quotedTable}\r\n" +
            $"{{\r\n" +
            $"    fields\r\n" +
            $"    {{\r\n" +
            $"        modify({quotedField})\r\n" +
            $"        {{\r\n" +
            $"            {propertyText}\r\n" +
            $"        }}\r\n" +
            $"    }}\r\n" +
            $"}}\r\n";

        var directory = pageFilePath is not null
            ? Path.GetDirectoryName(pageFilePath) ?? "."
            : ".";
        var filePath = Path.Combine(directory, fileName);

        await File.WriteAllTextAsync(filePath, content, Encoding.UTF8, ct).ConfigureAwait(false);
    }

    private static async Task<int> FindNextAvailableIdAsync(
        Solution solution, Project project, CancellationToken ct)
    {
        var idRegex = new Regex(@"tableextension\s+(\d+)", RegexOptions.IgnoreCase);
        int maxId = 50000;

        foreach (var docId in project.DocumentIds)
        {
            var doc = solution.GetDocument(docId);
            if (doc is null)
                continue;

            var text = await doc.GetTextAsync(ct).ConfigureAwait(false);
            var matches = idRegex.Matches(text.ToString());
            foreach (Match match in matches)
            {
                if (int.TryParse(match.Groups[1].Value, out int id) && id > maxId)
                    maxId = id;
            }
        }

        return maxId + 1;
    }

    // ── Shared page property removal ──────────────────────────────────────────

    private static async Task<Document?> RemovePropertyFromPageInSolutionAsync(
        Solution solution,
        DocumentId pageDocumentId,
        PropertySyntax originalPropertySyntax,
        CancellationToken ct)
    {
        var pageDoc = solution.GetDocument(pageDocumentId);
        if (pageDoc is null)
            return null;

        var pageRoot = await pageDoc.GetSyntaxRootAsync(ct).ConfigureAwait(false);
        if (pageRoot is null)
            return null;

        if (originalPropertySyntax.Parent is not PropertyListSyntax originalPagePropertyList)
            return null;

        var pagePropertyListInRoot = pageRoot
            .DescendantNodesAndSelf()
            .OfType<PropertyListSyntax>()
            .FirstOrDefault(pl => pl.Span == originalPagePropertyList.Span);

        if (pagePropertyListInRoot is null)
            return null;

        var propertyToRemove = pagePropertyListInRoot.Properties
            .FirstOrDefault(p => p.Span == originalPropertySyntax.Span);

        if (propertyToRemove is null)
            return null;

        var newPageProperties = pagePropertyListInRoot.Properties.Remove(propertyToRemove);
        var newPagePropertyList = pagePropertyListInRoot.WithProperties(newPageProperties);
        var newPageRoot = pageRoot.ReplaceNode(pagePropertyListInRoot, newPagePropertyList);

        return pageDoc.WithSyntaxRoot(newPageRoot);
    }
}
