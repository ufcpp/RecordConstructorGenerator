using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace RecordConstructorGenerator
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RecordConstructorGeneratorCodeFixProvider)), Shared]
    public class RecordConstructorGeneratorCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(NoRecordConstructor.DiagnosticId, NoAssignmentInRecordConstructor.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            // TODO: Replace the following code with your own analysis, generating a CodeAction for each fix to suggest
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().First();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create("Generate record constructor", c => MakeUppercaseAsync(context.Document, declaration, c)),
                diagnostic);
        }

        private static readonly SyntaxTriviaList EmptyTrivia = TriviaList();
        private static readonly SyntaxToken PublicToken = Token(SyntaxKind.PublicKeyword);

        private ConstructorDeclarationSyntax GenerateConstructor(string typeName, IEnumerable<PropertyDeclarationSyntax> propertyNames)
        {
            var props = propertyNames.Select(p => new Property(p)).ToArray();

            var docComment = GenerateDocComment(props.Select(p => p.Name));
            var parameterList = ParameterList().AddParameters(props.Select(p => p.ToParameter()).ToArray());
            var body = Block().AddStatements(props.Select(p => p.ToAssignment()).ToArray());

            return ConstructorDeclaration(typeName)
                .WithModifiers(SyntaxTokenList.Create(PublicToken))
                .WithParameterList(parameterList)
                .WithLeadingTrivia(docComment)
                .WithBody(body)
                .WithAdditionalAnnotations(Formatter.Annotation);
        }

        private static SyntaxTriviaList GenerateDocComment(IEnumerable<IdentifierName> props)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine();
            sb.AppendLine("/// <summary>" + SyntaxExtensions.RecordComment + "</summary>");
            foreach (var p in props)
                sb.AppendLine($"/// <param name=\"{p.Lower}\"><see cref=\"{p.Upper}\"/></param>");

            var docComment = ParseLeadingTrivia(sb.ToString());
            return docComment;
        }

        private async Task<Document> MakeUppercaseAsync(Document document, TypeDeclarationSyntax typeDecl, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) as CompilationUnitSyntax;

            var properties = typeDecl.Members.OfType<PropertyDeclarationSyntax>().Where(p => p.IsGetOnlyAuto());
            var recordCtor = typeDecl.GetRecordConstructor();
            TypeDeclarationSyntax newDecl;

            if (recordCtor != null)
            {
                var newCtor = GenerateConstructor(typeDecl.Identifier.Text, properties);
                newDecl = typeDecl.ReplaceNode(recordCtor, newCtor);
            }
            else
            {
                var newCtor = GenerateConstructor(typeDecl.Identifier.Text, properties);
                newDecl = typeDecl.InsertNodesAfter(typeDecl.Members.Last(), new[] { newCtor });
            }

            var newRoolt = root.ReplaceNode(typeDecl, newDecl)
                .WithAdditionalAnnotations(Formatter.Annotation);

            return document.WithSyntaxRoot(newRoolt);
        }
    }
}