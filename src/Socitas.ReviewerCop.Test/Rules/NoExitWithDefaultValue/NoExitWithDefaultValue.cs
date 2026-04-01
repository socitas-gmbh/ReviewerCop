using RoslynTestKit;

namespace Socitas.ReviewerCop.Test
{
    public class NoExitWithDefaultValue : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.NoExitWithDefaultValue>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(NoExitWithDefaultValue)));
        }

        [Test]
        [TestCase("ExitWithFalse")]
        [TestCase("ExitWithZero")]
        [TestCase("ExitWithEmptyString")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.NoExitWithDefaultValue);
        }

        [Test]
        [TestCase("ExitWithTrue")]
        [TestCase("ExitWithNonZero")]
        [TestCase("BareExit")]
        [TestCase("ExitWithNonEmptyString")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.NoExitWithDefaultValue);
        }
    }
}
