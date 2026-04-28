using AICop = Socitas.AICop;
using Socitas.AICop.CodeFixes;
using RoslynTestKit;

namespace Socitas.ReviewerCop.Test
{
    public class ExtensionObjectSocSuffix : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private static readonly AICop.Analyzers.ExtensionObjectSocSuffix _analyzer = new();
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<AICop.Analyzers.ExtensionObjectSocSuffix>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(ExtensionObjectSocSuffix)));
        }

        [Test]
        [TestCase("NonLocalProcedureMissingSoc", "AI0008")]
        [TestCase("ActionMissingSoc", "AI0008")]
        [TestCase("FieldMissingSoc", "AI0008")]
        [TestCase("LocalProcedureWithSoc", "AI0009")]
        [TestCase("SocPrefixInsteadOfSuffix", "AI0008")]
        public async Task HasDiagnostic(string testCase, string diagnosticId)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, diagnosticId);
        }

        [Test]
        [TestCase("NonLocalProcedureWithSoc")]
        [TestCase("LocalProcedureWithoutSoc")]
        [TestCase("ActionWithSocSpace")]
        [TestCase("FieldWithSoc")]
        [TestCase("TriggerInExtension")]
        [TestCase("EnumExtensionWithValue")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, AICop.DiagnosticIds.ExtensionMemberMissingSocSuffix);
            _fixture.NoDiagnosticAtAllMarkers(code, AICop.DiagnosticIds.LocalProcedureHasSocSuffix);
        }

        [Test]
        [TestCase("AddSocToProcedure")]
        [TestCase("AddSocToAction")]
        [TestCase("AddSocToField")]
        public async Task HasFix_MissingSoc(string testCase)
        {
            var currentCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, "HasFix", testCase, "current.al"))
                .ConfigureAwait(false);

            var expectedCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, "HasFix", testCase, "expected.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<ExtensionObjectSocSuffixFixProvider>(
                new CodeFixTestFixtureConfig
                {
                    AdditionalAnalyzers = [_analyzer]
                });

            fixture.TestCodeFix(currentCode, expectedCode, AICop.DiagnosticDescriptors.ExtensionMemberMissingSocSuffix);
        }

        [Test]
        [TestCase("RemoveSocFromLocalProcedure")]
        public async Task HasFix_SocOnLocal(string testCase)
        {
            var currentCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, "HasFix", testCase, "current.al"))
                .ConfigureAwait(false);

            var expectedCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, "HasFix", testCase, "expected.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<ExtensionObjectSocSuffixFixProvider>(
                new CodeFixTestFixtureConfig
                {
                    AdditionalAnalyzers = [_analyzer]
                });

            fixture.TestCodeFix(currentCode, expectedCode, AICop.DiagnosticDescriptors.LocalProcedureHasSocSuffix);
        }

        [Test]
        public async Task HasGuidanceAction_MissingSoc()
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), "NonLocalProcedureMissingSoc.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<ExtensionObjectSocSuffixFixProvider>(
                new CodeFixTestFixtureConfig { AdditionalAnalyzers = [_analyzer] });

            var titles = fixture.GetCodeFixes(code, AICop.DiagnosticDescriptors.ExtensionMemberMissingSocSuffix)
                .Select(a => a.Title);

            Assert.That(titles, Has.Some.StartsWith("To fix"));
        }

        [Test]
        public async Task HasGuidanceAction_SocOnLocal()
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), "LocalProcedureWithSoc.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<ExtensionObjectSocSuffixFixProvider>(
                new CodeFixTestFixtureConfig { AdditionalAnalyzers = [_analyzer] });

            var titles = fixture.GetCodeFixes(code, AICop.DiagnosticDescriptors.LocalProcedureHasSocSuffix)
                .Select(a => a.Title);

            Assert.That(titles, Has.Some.StartsWith("To fix"));
        }
    }
}
