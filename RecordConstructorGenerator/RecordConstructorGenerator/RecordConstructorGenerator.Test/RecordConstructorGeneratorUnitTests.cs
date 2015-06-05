using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace RecordConstructorGenerator.Test
{
    [TestClass]
    public class RecordConstructorGeneratorTest : ContractCodeFixVerifier
    {

        [TestMethod]
        public void NoDiagnostics()
        {
            VerifyDiagnostic("NoDiagnostics1");
            VerifyDiagnostic("NoDiagnostics2");
        }

        [TestMethod]
        public void TypicalUsage() => VerifyDiagnostic(new[]
        {
            NoRecordConstructorResult(8, 9),
            NoRecordConstructorResult(13, 9),
        });

        [TestMethod]
        public void AddNewProperty() => VerifyDiagnostic(NoAssignmentInRecordConstructorResult("Y", 16, 9));

        protected override CodeFixProvider GetCSharpCodeFixProvider() => new RecordConstructorGeneratorCodeFixProvider();

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new RecordConstructorGeneratorAnalyzer();

        private DiagnosticResult NoRecordConstructorResult(DiagnosticResultLocation location) => new DiagnosticResult
        {
            Id = NoRecordConstructor.DiagnosticId,
            Message = NoRecordConstructor.MessageFormat.ToString(),
            Severity = DiagnosticSeverity.Info,
            Locations = new[] { location }
        };

        private DiagnosticResult NoRecordConstructorResult(int line, int column)
            => NoRecordConstructorResult(new DiagnosticResultLocation("Test0.cs", line, column));

        private DiagnosticResult NoAssignmentInRecordConstructorResult(string propertyName, DiagnosticResultLocation location) => new DiagnosticResult
        {
            Id = NoAssignmentInRecordConstructor.DiagnosticId,
            Message = string.Format(NoAssignmentInRecordConstructor.MessageFormat.ToString(), propertyName),
            Severity = DiagnosticSeverity.Info,
            Locations = new[] { location }
        };

        private DiagnosticResult NoAssignmentInRecordConstructorResult(string propertyName, int line, int column)
            => NoAssignmentInRecordConstructorResult(propertyName, new DiagnosticResultLocation("Test0.cs", line, column));
    }
}