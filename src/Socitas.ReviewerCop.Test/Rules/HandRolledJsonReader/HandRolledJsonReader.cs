using AICop = Socitas.AICop;
using Socitas.AICop.CodeFixes;
using RoslynTestKit;

namespace Socitas.ReviewerCop.Test
{
    public class HandRolledJsonReader : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private static readonly AICop.Analyzers.HandRolledJsonReader _analyzer = new();
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<AICop.Analyzers.HandRolledJsonReader>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(HandRolledJsonReader)));
        }

        [Test]
        [TestCase("Inline_AsText")]
        [TestCase("Inline_AsBoolean")]
        [TestCase("NullGuarded_AsText")]
        [TestCase("NullGuarded_AsInteger")]
        [TestCase("AllPrimitiveTypes")]
        [TestCase("Inline_AsObject")]
        [TestCase("Inline_AsArray")]
        [TestCase("GuardForm_AsText")]
        [TestCase("GuardForm_NoNullGuard")]
        [TestCase("GuardForm_AsObject")]
        [TestCase("GuardForm_AssignmentTerminal")]
        [TestCase("GuardForm_IsValueAndIsNull")]
        [TestCase("GuardForm_IsObjectGuard")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, AICop.DiagnosticIds.HandRolledJsonReader);
        }

        [Test]
        [TestCase("Compliant_DirectGetText")]
        [TestCase("Compliant_GetWithElseBranch")]
        [TestCase("Compliant_GetForExistenceCheck")]
        [TestCase("Compliant_GetWithDifferentArity")]
        [TestCase("Compliant_AsValueOnNonTokenIdent")]
        [TestCase("Compliant_DifferentTerminalCall")]
        [TestCase("Compliant_GetWithBeginEndBlock")]
        [TestCase("Compliant_GuardExtraStmt")]
        [TestCase("Compliant_GuardNoTerminal")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, AICop.DiagnosticIds.HandRolledJsonReader);
        }

        [Test]
        [TestCase("Fix_Inline_AsText")]
        [TestCase("Fix_NullGuarded_AsText")]
        [TestCase("Fix_Inline_AsBoolean")]
        [TestCase("Fix_Inline_AsObject")]
        [TestCase("Fix_Inline_AsArray")]
        [TestCase("Fix_GuardForm_AsText")]
        [TestCase("Fix_GuardForm_NoNullGuard")]
        [TestCase("Fix_GuardForm_AsObject")]
        [TestCase("Fix_GuardForm_Assignment")]
        [TestCase("Fix_GuardForm_IsValueAndIsNull")]
        public async Task HasFix(string testCase)
        {
            var currentCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "current.al"))
                .ConfigureAwait(false);

            var expectedCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "expected.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<HandRolledJsonReaderFixProvider>(
                new CodeFixTestFixtureConfig
                {
                    AdditionalAnalyzers = [_analyzer]
                });

            fixture.TestCodeFix(currentCode, expectedCode, AICop.DiagnosticDescriptors.HandRolledJsonReader);
        }
    }
}
