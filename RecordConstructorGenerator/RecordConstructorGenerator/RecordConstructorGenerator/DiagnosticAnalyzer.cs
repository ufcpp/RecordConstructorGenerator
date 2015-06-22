using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("RecordConstructorGenerator.Test")]

namespace RecordConstructorGenerator
{
    internal class NoAssignmentInRecordConstructor
    {
        public const string DiagnosticId = "RCNoAssignment";

        internal static readonly LocalizableString Title = "No assignment to a get-only auto-property.";
        internal static readonly LocalizableString MessageFormat = "There is no assignment to a get-only auto-property '{0}'.";
        internal static readonly LocalizableString Description = "You can generate the record constructor and assignments.";
        internal const string Category = "Refactoring";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Info, isEnabledByDefault: true, description: Description);
    }

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RecordConstructorGeneratorAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(NoAssignmentInRecordConstructor.Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.Property);
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            var p = context.Symbol as IPropertySymbol;
            if (p == null) return;

            var ps = p.DeclaringSyntaxReferences.First().GetSyntax() as PropertyDeclarationSyntax;
            if (ps == null) return;

            if (!ps.IsGetOnlyAuto()) return;

            var ts = ps.FirstAncestorOrSelf<TypeDeclarationSyntax>();
            if (ts == null) return;
            if (ts.IsKind(SyntaxKind.InterfaceDeclaration)) return;

            var assigned = ts.FindPartialTypeDelarations(context.Compilation)
                .SelectMany(t => t.DescendantNodes(n => !n.IsKind(SyntaxKind.SimpleAssignmentExpression)))
                .OfType<AssignmentExpressionSyntax>()
                .Where(n => (n.Left as IdentifierNameSyntax)?.Identifier.Text == p.Name)
                .Any();

            if(!assigned)
            {
                var diagnostic = Diagnostic.Create(NoAssignmentInRecordConstructor.Rule, ps.GetLocation(), p.Name);
                context.ReportDiagnostic(diagnostic);
                return;
            }
        }
    }
}
