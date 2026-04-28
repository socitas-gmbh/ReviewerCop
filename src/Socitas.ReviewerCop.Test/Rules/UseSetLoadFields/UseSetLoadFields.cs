using RoslynTestKit;
using Socitas.ReviewerCop.CodeFixes;

namespace Socitas.ReviewerCop.Test
{
    public class UseSetLoadFields : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private static readonly Analyzers.UseSetLoadFields _analyzer = new();
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.UseSetLoadFields>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(UseSetLoadFields)));
        }

        [Test]
        [TestCase("FindSetWithoutSetLoadFields")]
        [TestCase("FindFirstWithoutSetLoadFields")]
        [TestCase("CompilerCanary_FindFirstWithoutSetLoadFields")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.UseSetLoadFields);
        }

        [Test]
        [TestCase("FindSetWithSetLoadFields")]
        [TestCase("TemporaryRecordFindSet")]
        [TestCase("FindMinusExistenceCheck")]
        [TestCase("VariablePassedToAnotherFunction")]
        [TestCase("FindInOnFindRecord")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.UseSetLoadFields);
        }

        [Test]
        [TestCase("AddSetLoadFields")]
        [TestCase("UpdateSetLoadFields")]
        public async Task HasFix(string testCase)
        {
            var currentCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "current.al"))
                .ConfigureAwait(false);

            var expectedCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "expected.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<UseSetLoadFieldsFixProvider>(
                new CodeFixTestFixtureConfig
                {
                    AdditionalAnalyzers = [_analyzer]
                });

            fixture.TestCodeFix(currentCode, expectedCode, DiagnosticDescriptors.UseSetLoadFields);
        }
    }
}
