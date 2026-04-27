using AICop = Socitas.AICop;
using RoslynTestKit;

namespace Socitas.ReviewerCop.Test
{
    public class NoGlobalVariables : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
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
    }
}
