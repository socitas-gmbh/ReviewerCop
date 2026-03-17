using RoslynTestKit;

namespace ALCops.CompanyCop.Test
{
    public class NoTypeOrPrefixInVariableName : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.NoTypeOrPrefixInVariableName>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(NoTypeOrPrefixInVariableName)));
        }

        [Test]
        [TestCase("LrecPrefix")]
        [TestCase("GrecPrefix")]
        [TestCase("TypeNameAsVariableName")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.NoTypeOrPrefixInVariableName);
        }

        [Test]
        [TestCase("DescriptiveVariableNames")]
        [TestCase("PascalCaseNames")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.NoTypeOrPrefixInVariableName);
        }
    }
}
