using Socitas.ReviewerCop.CodeFixes;
using RoslynTestKit;

namespace Socitas.ReviewerCop.Test
{
    public class TernaryOperator : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private static readonly Analyzers.TernaryOperator _analyzer = new();
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.TernaryOperator>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(TernaryOperator)));
        }

        [Test]
        [TestCase("TernaryInAssignment")]
        [TestCase("TernaryInArgument")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.TernaryOperator);
        }

        [Test]
        [TestCase("IfStatement")]
        [TestCase("NoConditional")]
        [TestCase("IfWithoutElse")]
        [TestCase("DifferentTargets")]
        [TestCase("ComplexCondition")]
        [TestCase("StringAlterationCall")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.TernaryOperator);
        }

        [Test]
        [TestCase("SimpleTernaryInAssignment")]
        [TestCase("TernaryInArgument")]
        public async Task HasFix(string testCase)
        {
            var currentCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "current.al"))
                .ConfigureAwait(false);

            var expectedCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "expected.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<TernaryOperatorFixProvider>(
                new CodeFixTestFixtureConfig
                {
                    AdditionalAnalyzers = [_analyzer]
                });

            fixture.TestCodeFix(currentCode, expectedCode, DiagnosticDescriptors.TernaryOperator);
        }
    }
}
