using AICop = Socitas.AICop;
using RoslynTestKit;

namespace Socitas.ReviewerCop.Test
{
    public class InitializeRestClientWithHandler : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
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
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, AICop.DiagnosticIds.InitializeRestClientWithHandler);
        }
    }
}
