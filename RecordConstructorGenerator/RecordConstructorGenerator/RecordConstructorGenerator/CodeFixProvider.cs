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
                CodeAction.Create("Generate record constructor", c => GenerateCodeSameFile(context.Document, declaration, false, c), equivalenceKey: "SameFile"),
                diagnostic);

            context.RegisterCodeFix(
                CodeAction.Create("Generate record constructor (partial)", c => GenerateCodePartial(context.Document, declaration, false, c), equivalenceKey: "AnotherPartialFile"),
                diagnostic);

            context.RegisterCodeFix(
                CodeAction.Create("Generate record constructor (with optional params)", c => GenerateCodeSameFile(context.Document, declaration, true, c), equivalenceKey: "SameFileOpt"),
                diagnostic);

            context.RegisterCodeFix(
                CodeAction.Create("Generate record constructor (partial with optional params)", c => GenerateCodePartial(context.Document, declaration, true, c), equivalenceKey: "AnotherPartialFileOpt"),
                diagnostic);

            context.RegisterCodeFix(
                CodeAction.Create("Generate copy constructor", c => GenerateCodeForCopyConstructor(context.Document, declaration, false, c), equivalenceKey: "SameFileCopy"),
                diagnostic);
        }

        private static readonly SyntaxTriviaList EmptyTrivia = TriviaList();
        private static readonly SyntaxToken PublicToken = Token(SyntaxKind.PublicKeyword);

        private static ConstructorDeclarationSyntax GenerateCopyConstructor(string typeName, IEnumerable<PropertyDeclarationSyntax> propertyNames, bool isOptional)
        {
            var props = propertyNames.Select(p => new Property(p)).ToArray();

            var identifier = "obj";

            var docComment = GenerateDocComment(props.Select(p => p.Name), true);
            var parameterList = ParameterList().AddParameters(GetParameter(typeName, identifier));
            var body = Block().AddStatements(props.Select(p => p.ToAssignment(identifier)).ToArray());

            return ConstructorDeclaration(typeName)
                .WithModifiers(SyntaxTokenList.Create(PublicToken))
                .WithParameterList(parameterList)
                .WithLeadingTrivia(docComment)
                .WithBody(body)
                .WithAdditionalAnnotations(Formatter.Annotation);
        }

        public static ParameterSyntax GetParameter(string typeName, string identifier)
            => Parameter(
                    default(SyntaxList<AttributeListSyntax>),
                    default(SyntaxTokenList),
                    ParseTypeName(typeName),
                    Identifier(identifier),
                    null);

        private static ConstructorDeclarationSyntax GenerateConstructor(string typeName, IEnumerable<PropertyDeclarationSyntax> propertyNames, bool isOptional)
        {
            var props = propertyNames.Select(p => new Property(p)).ToArray();

            var docComment = GenerateDocComment(props.Select(p => p.Name));
            var parameterList = ParameterList().AddParameters(props.Select(p => p.ToParameter(isOptional)).ToArray());
            var body = Block().AddStatements(props.Select(p => p.ToAssignment()).ToArray());

            return ConstructorDeclaration(typeName)
                .WithModifiers(SyntaxTokenList.Create(PublicToken))
                .WithParameterList(parameterList)
                .WithLeadingTrivia(docComment)
                .WithBody(body)
                .WithAdditionalAnnotations(Formatter.Annotation);
        }

        private static SyntaxTriviaList GenerateDocComment(IEnumerable<IdentifierName> props, bool isCopyConstructor = false)
        {
            var sb = new System.Text.StringBuilder();
            var comment = isCopyConstructor ? SyntaxExtensions.CopyComment : SyntaxExtensions.RecordComment;
            sb.AppendLine("/// <summary>" + comment + "</summary>");
            foreach (var p in props)
                sb.AppendLine($"/// <param name=\"{p.Lower}\"><see cref=\"{p.Upper}\"/></param>");

            var docComment = ParseLeadingTrivia(sb.ToString());
            return docComment;
        }

        private async Task<Document> GenerateCodeForCopyConstructor(Document document, TypeDeclarationSyntax typeDecl, bool isOptional, CancellationToken cancellationToken)
        {
            var properties = typeDecl.Members.OfType<PropertyDeclarationSyntax>().Where(p => p.IsAutoProperty());
            var newCtor = GenerateCopyConstructor(typeDecl.Identifier.Text, properties, isOptional);

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) as CompilationUnitSyntax;
            var newTypeDecl = typeDecl.AddMembers(newCtor);

            var newRoot = root.ReplaceNode(typeDecl, newTypeDecl);

            document = document.WithSyntaxRoot(newRoot);
            return document;
        }

        private async Task<Document> GenerateCodeSameFile(Document document, TypeDeclarationSyntax typeDecl, bool isOptional, CancellationToken cancellationToken)
        {
            var properties = typeDecl.Members.OfType<PropertyDeclarationSyntax>().Where(p => p.IsAutoProperty());
            var newCtor = GenerateConstructor(typeDecl.Identifier.Text, properties, isOptional);

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) as CompilationUnitSyntax;
            var newTypeDecl = typeDecl.AddMembers(newCtor);

            var newRoot = root.ReplaceNode(typeDecl, newTypeDecl);

            document = document.WithSyntaxRoot(newRoot);
            return document;
        }

        private async Task<Solution> GenerateCodePartial(Document document, TypeDeclarationSyntax typeDecl, bool isOptional, CancellationToken cancellationToken)
        {
            document = await AddPartialModifier(document, typeDecl, cancellationToken);
            document = await AddNewDocument(document, typeDecl, isOptional, cancellationToken);
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

        private static async Task<Document> AddNewDocument(Document document, TypeDeclarationSyntax typeDecl, bool isOptional, CancellationToken cancellationToken)
        {
            var newRoot = await GeneratePartialDeclaration(document, typeDecl, isOptional, cancellationToken);

            var name = typeDecl.Identifier.Text;
            var generatedName = name + ".RecordConstructor.cs";

            var project = document.Project;

            var existed = project.Documents.FirstOrDefault(d => d.Name == generatedName);
            if (existed != null) return existed.WithSyntaxRoot(newRoot);
            else return project.AddDocument(generatedName, newRoot, document.Folders);
        }

        private static async Task<CompilationUnitSyntax> GeneratePartialDeclaration(Document document, TypeDeclarationSyntax typeDecl, bool isOptional, CancellationToken cancellationToken)
        {
            var properties = typeDecl.Members.OfType<PropertyDeclarationSyntax>().Where(p => p.IsAutoProperty());
            var newCtor = GenerateConstructor(typeDecl.Identifier.Text, properties, isOptional);

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