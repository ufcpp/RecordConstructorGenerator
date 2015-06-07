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
        public void TypicalUsage() => VerifyCodeFix();

        [TestMethod]
        public void AddNewProperty() => VerifyCodeFix();

        [TestMethod]
        public void CanBeUsedForStruct() => VerifyCodeFix();

        protected override CodeFixProvider GetCSharpCodeFixProvider() => new RecordConstructorGeneratorCodeFixProvider();

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new RecordConstructorGeneratorAnalyzer();
    }
}