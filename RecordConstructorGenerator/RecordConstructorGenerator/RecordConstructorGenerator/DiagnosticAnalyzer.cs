using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

[assembly: InternalsVisibleTo("RecordConstructorGenerator.Test")]

namespace RecordConstructorGenerator
{
    internal class NoRecordConstructor
    {
        public const string DiagnosticId = "NoRecordConstructor";

        internal static readonly LocalizableString Title = "There is no record constructor";
        internal static readonly LocalizableString MessageFormat = "There is no record constructor in the type which has get-only auto-properties.";
        internal static readonly LocalizableString Description = "You can generate the record constructor.";
        internal const string Category = "Refactoring";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Info, isEnabledByDefault: true, description: Description);
    }

    internal class NoAssignmentInRecordConstructor
    {
        public const string DiagnosticId = "NoAssignmentInRecordConstructor";

        internal static readonly LocalizableString Title = "There is no assignment in the record constructor";
        internal static readonly LocalizableString MessageFormat = "There is no assignment in the record constructor though '{0}' is a get-only auto-property.";
        internal static readonly LocalizableString Description = "You can generate the record constructor and assignments.";
        internal const string Category = "Refactoring";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Info, isEnabledByDefault: true, description: Description);
    }

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RecordConstructorGeneratorAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(NoRecordConstructor.Rule, NoAssignmentInRecordConstructor.Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzerSymbol, SyntaxKind.PropertyDeclaration);
            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.Property);
        }

        private void AnalyzerSymbol(SyntaxNodeAnalysisContext context)
        {
            var propDecl = (PropertyDeclarationSyntax)context.Node;

            var typeDecl = propDecl.FirstAncestorOrSelf<TypeDeclarationSyntax>();

            var model = context.SemanticModel;

            var asm = model.Compilation.Assembly;

            var l = asm.Locations;
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            var p = (IPropertySymbol)context.Symbol;

            var ps = (PropertyDeclarationSyntax)p.DeclaringSyntaxReferences.First().GetSyntax();

            if (!ps.IsGetOnlyAuto()) return;

            var ts = ps.FirstAncestorOrSelf<TypeDeclarationSyntax>();

            var name = ts.Identifier.Text + ".RecordConstructor.cs";
            var generatedTree = context.Compilation.SyntaxTrees.FirstOrDefault(t => t.FilePath == name);

            //no generated file
            if (generatedTree == null) goto NO_RECORD_CTOR;

            ts = generatedTree.GetRoot().DescendantNodes()
                .OfType<TypeDeclarationSyntax>()
                .FirstOrDefault();

            //no generated file
            if (ts == null) goto NO_RECORD_CTOR;

            var recordCtor = ts.GetRecordConstructor();

            //no record constructor
            if (recordCtor == null) goto NO_RECORD_CTOR;

            var assignments = recordCtor.Body
                .DescendantNodes(s => !s.IsKind(SyntaxKind.SimpleAssignmentExpression))
                .OfType<AssignmentExpressionSyntax>();

            foreach (var statement in assignments)
            {
                var id = statement.Left as IdentifierNameSyntax;
                if (id == null) continue;

                if (id.Identifier.Text == p.Name)
                    return;
            }

            //no assignment in the record constructor
            {
                var diagnostic = Diagnostic.Create(NoAssignmentInRecordConstructor.Rule, ps.GetLocation(), p.Name);
                context.ReportDiagnostic(diagnostic);
                return;
            }

            NO_RECORD_CTOR:
            {
                var diagnostic = Diagnostic.Create(NoRecordConstructor.Rule, ps.GetLocation());
                context.ReportDiagnostic(diagnostic);
                return;
            }
        }
    }
}
