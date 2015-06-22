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
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(NoAssignmentInRecordConstructor.DiagnosticId);

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
                CodeAction.Create("Generate record constructor", c => GenerateCode(context.Document, declaration, c)),
                diagnostic);
        }

        private static readonly SyntaxTriviaList EmptyTrivia = TriviaList();
        private static readonly SyntaxToken PublicToken = Token(SyntaxKind.PublicKeyword);

        private static ConstructorDeclarationSyntax GenerateConstructor(string typeName, IEnumerable<PropertyDeclarationSyntax> propertyNames)
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
            sb.AppendLine("/// <summary>" + SyntaxExtensions.RecordComment + "</summary>");
            foreach (var p in props)
                sb.AppendLine($"/// <param name=\"{p.Lower}\"><see cref=\"{p.Upper}\"/></param>");

            var docComment = ParseLeadingTrivia(sb.ToString());
            return docComment;
        }

        private async Task<Solution> GenerateCode(Document document, TypeDeclarationSyntax typeDecl, CancellationToken cancellationToken)
        {
            document = await AddPartialModifier(document, typeDecl, cancellationToken);
            document = await AddNewDocument(document, typeDecl, cancellationToken);
            return document.Project.Solution;
        }

        private static async Task<Document> AddPartialModifier(Document document, TypeDeclarationSyntax typeDecl, CancellationToken cancellationToken)
        {
            var newTypeDecl = typeDecl.AddPartialModifier();

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) as CompilationUnitSyntax;
            var newRoolt = root.ReplaceNode(typeDecl, newTypeDecl)
                .WithAdditionalAnnotations(Formatter.Annotation);

            document = document.WithSyntaxRoot(newRoolt);
            return document;
        }

        private static async Task<Document> AddNewDocument(Document document, TypeDeclarationSyntax typeDecl, CancellationToken cancellationToken)
        {
            var newRoot = await GeneratePartialDeclaration(document, typeDecl, cancellationToken);

            var name = typeDecl.Identifier.Text;
            var generatedName = name + ".RecordConstructor.cs";

            var project = document.Project;

            var existed = project.Documents.FirstOrDefault(d => d.Name == generatedName);
            if (existed != null)
                project = project.RemoveDocument(existed.Id);

            var newDocument = project.AddDocument(generatedName, newRoot, document.Folders);
            return newDocument;
        }

        private static async Task<CompilationUnitSyntax> GeneratePartialDeclaration(Document document, TypeDeclarationSyntax typeDecl, CancellationToken cancellationToken)
        {
            var properties = typeDecl.Members.OfType<PropertyDeclarationSyntax>().Where(p => p.IsGetOnlyAuto());
            var newCtor = GenerateConstructor(typeDecl.Identifier.Text, properties);

            var name = typeDecl.Identifier.Text;
            var newTypeDecl = typeDecl.CreatePartialTypeDelaration()
                .AddMembers(newCtor)
                .WithAdditionalAnnotations(Formatter.Annotation);

            var ns = typeDecl.FirstAncestorOrSelf<NamespaceDeclarationSyntax>()?.Name.WithoutTrivia().GetText().ToString();

            MemberDeclarationSyntax topDecl;
            if (ns != null)
            {
                topDecl = NamespaceDeclaration(IdentifierName(ns))
                    .AddMembers(newTypeDecl)
                    .WithAdditionalAnnotations(Formatter.Annotation);
            }
            else
            {
                topDecl = newTypeDecl;
            }

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) as CompilationUnitSyntax;

            return CompilationUnit().AddUsings(root.Usings.ToArray())
                .AddMembers(topDecl)
                .WithTrailingTrivia(CarriageReturnLineFeed)
                .WithAdditionalAnnotations(Formatter.Annotation);
        }
    }
}