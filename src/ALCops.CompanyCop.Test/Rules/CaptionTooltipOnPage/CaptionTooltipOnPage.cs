using ALCops.CompanyCop.CodeFixes;
using RoslynTestKit;

namespace ALCops.CompanyCop.Test
{
    public class CaptionTooltipOnPage : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private static readonly Analyzers.CaptionTooltipOnPage _analyzer = new();
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.CaptionTooltipOnPage>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(CaptionTooltipOnPage)));
        }

        [Test]
        [TestCase("PageFieldWithCaption")]
        [TestCase("PageExtensionFieldWithCaption")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.CaptionTooltipOnPage);
        }

        [Test]
        [TestCase("TableFieldHasCaption")]
        [TestCase("PageFieldNoCaptionOrTooltip")]
        [TestCase("GlobalVariablePageField")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.CaptionTooltipOnPage);
        }

        [Test]
        [TestCase("RemoveCaptionFromPageField")]
        [TestCase("RemoveToolTipFromPageField")]
        public async Task HasFix(string testCase)
        {
            var currentCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "current.al"))
                .ConfigureAwait(false);

            var expectedCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "expected.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<CaptionTooltipOnPageFixProvider>(
                new CodeFixTestFixtureConfig
                {
                    AdditionalAnalyzers = [_analyzer]
                });

            fixture.TestCodeFix(currentCode, expectedCode, DiagnosticDescriptors.CaptionTooltipOnPage);
        }
    }
}
