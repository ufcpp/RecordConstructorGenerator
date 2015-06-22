using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace RecordConstructorGenerator.Test
{
    [TestClass]
    public class RecordConstructorGeneratorTest : ConventionCodeFixVerifier
    {
        [TestMethod]
        public void NoDiagnostics()
        {
            VerifyCSharpByConvention("NoDiagnostics1");
            VerifyCSharpByConvention("NoDiagnostics2");
            VerifyCSharpByConvention("NoDiagnostics3");
        }

        [TestMethod]
        public void TypicalUsage() => VerifyCSharpByConvention();

        [TestMethod]
        public void AddNewProperty() => VerifyCSharpByConvention();

        [TestMethod]
        public void CanBeUsedForStruct() => VerifyCSharpByConvention();

        protected override CodeFixProvider GetCSharpCodeFixProvider() => new RecordConstructorGeneratorCodeFixProvider();

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new RecordConstructorGeneratorAnalyzer();
    }
}