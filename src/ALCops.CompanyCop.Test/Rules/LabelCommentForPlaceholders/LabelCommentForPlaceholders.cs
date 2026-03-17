using RoslynTestKit;

namespace ALCops.CompanyCop.Test
{
    public class LabelCommentForPlaceholders : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.LabelCommentForPlaceholders>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(LabelCommentForPlaceholders)));
        }

        [Test]
        [TestCase("LabelWithPlaceholderNoComment")]
        [TestCase("LabelWithHashPlaceholderNoComment")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.LabelCommentForPlaceholders);
        }

        [Test]
        [TestCase("LabelWithPlaceholderAndComment")]
        [TestCase("LabelWithoutPlaceholder")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.LabelCommentForPlaceholders);
        }
    }
}
