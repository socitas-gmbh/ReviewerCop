using AICop = Socitas.AICop;
using Socitas.AICop.CodeFixes;
using RoslynTestKit;

namespace Socitas.ReviewerCop.Test
{
    public class NoGlobalVariables : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private static readonly AICop.Analyzers.NoGlobalVariables _analyzer = new();
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<AICop.Analyzers.NoGlobalVariables>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(NoGlobalVariables)));
        }

        [Test]
        [TestCase("CodeunitWithGlobalVar")]
        [TestCase("PageWithGlobalVar")]
        [TestCase("PageWithUnusedGlobalVar")]
        [TestCase("ReportWithUnusedGlobalVar")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, AICop.DiagnosticIds.NoGlobalVariables);
        }

        [Test]
        [TestCase("CodeunitWithoutGlobalVar")]
        [TestCase("CodeunitWithLocalVar")]
        [TestCase("TableExtensionWithImplicitVars")]
        [TestCase("PageWithImplicitVars")]
        [TestCase("PageWithFieldSourceVars")]
        [TestCase("ReportWithColumnSourceVars")]
        [TestCase("ReportWithImplicitVars")]
        [TestCase("SingleInstanceCodeunit")]
        [TestCase("PageExtWithPropertyValueVars")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, AICop.DiagnosticIds.NoGlobalVariables);
        }

        [Test]
        public async Task HasGuidanceAction()
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), "CodeunitWithGlobalVar.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<NoGlobalVariablesGuidanceProvider>(
                new CodeFixTestFixtureConfig { AdditionalAnalyzers = [_analyzer] });

            var titles = fixture.GetCodeFixes(code, AICop.DiagnosticDescriptors.NoGlobalVariables)
                .Select(a => a.Title);

            Assert.That(titles, Has.Some.StartsWith("To fix"));
        }
    }
}
