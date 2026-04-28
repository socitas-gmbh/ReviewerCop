using AICop = Socitas.AICop;
using Socitas.AICop.CodeFixes;
using RoslynTestKit;

namespace Socitas.ReviewerCop.Test
{
    public class UseRestClient : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private static readonly AICop.Analyzers.UseRestClient _analyzer = new();
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<AICop.Analyzers.UseRestClient>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(UseRestClient)));
        }

        [Test]
        [TestCase("HttpClientVariable")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, AICop.DiagnosticIds.UseRestClient);
        }

        [Test]
        [TestCase("RestClientCodeunit")]
        [TestCase("HttpClientHandlerImpl")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, AICop.DiagnosticIds.UseRestClient);
        }

        [Test]
        public async Task HasGuidanceAction()
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), "HttpClientVariable.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<UseRestClientGuidanceProvider>(
                new CodeFixTestFixtureConfig { AdditionalAnalyzers = [_analyzer] });

            var titles = fixture.GetCodeFixes(code, AICop.DiagnosticDescriptors.UseRestClient)
                .Select(a => a.Title);

            Assert.That(titles, Has.Some.StartsWith("To fix"));
        }
    }
}
