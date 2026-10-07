using AICop = Socitas.AICop;
using RoslynTestKit;

namespace Socitas.ReviewerCop.Test
{
    public class SetLoadFieldsOnFlowField : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<AICop.Analyzers.SetLoadFieldsOnFlowField>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(SetLoadFieldsOnFlowField)));
        }

        [Test]
        [TestCase("SetLoadFieldsWithoutCalc")]
        [TestCase("AddLoadFieldsWithoutCalc")]
        [TestCase("CalcFieldsOnOtherFlowField")]
        [TestCase("SetAutoCalcFieldsAfterFind")]
        [TestCase("CalcFieldsOnOtherRecord")]
        [TestCase("SetAutoCalcFieldsBefore")]
        [TestCase("CalcFieldsAfter")]
        [TestCase("ImplicitRecCalcFieldsAfter")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, AICop.DiagnosticIds.SetLoadFieldsOnFlowField);
        }

        [Test]
        [TestCase("NormalFieldsOnly")]
        [TestCase("FlowFieldOnlyInSetAutoCalcFields")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, AICop.DiagnosticIds.SetLoadFieldsOnFlowField);
        }
    }
}
