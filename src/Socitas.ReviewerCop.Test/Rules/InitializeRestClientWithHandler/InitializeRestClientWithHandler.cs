using AICop = Socitas.AICop;
using Socitas.AICop.CodeFixes;
using RoslynTestKit;

namespace Socitas.ReviewerCop.Test
{
    public class InitializeRestClientWithHandler : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private static readonly AICop.Analyzers.InitializeRestClientWithHandler _analyzer = new();
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<AICop.Analyzers.InitializeRestClientWithHandler>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(InitializeRestClientWithHandler)));
        }

        [Test]
        [TestCase("NoLocalHandler")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, AICop.DiagnosticIds.InitializeRestClientWithHandler);
        }

        [Test]
        [TestCase("WithLocalHandler")]
        [TestCase("WithLocalHandlerAndAuth")]
        [TestCase("WithLocalHandlerSystemAuth")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, AICop.DiagnosticIds.InitializeRestClientWithHandler);
        }

        [Test]
        public async Task HasGuidanceAction()
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), "NoLocalHandler.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<InitializeRestClientWithHandlerGuidanceProvider>(
                new CodeFixTestFixtureConfig { AdditionalAnalyzers = [_analyzer] });

            var titles = fixture.GetCodeFixes(code, AICop.DiagnosticDescriptors.InitializeRestClientWithHandler)
                .Select(a => a.Title);

            Assert.That(titles, Has.Some.StartsWith("To fix"));
        }
    }
}
