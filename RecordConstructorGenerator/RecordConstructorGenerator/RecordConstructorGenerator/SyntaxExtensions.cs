using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using System.Collections.Generic;

namespace RecordConstructorGenerator
{
    public static class SyntaxExtensions
    {
        public const string RecordComment = "Record Constructor";

        public static ConstructorDeclarationSyntax GetRecordConstructor(this TypeDeclarationSyntax typeDecl)
            => (ConstructorDeclarationSyntax)typeDecl.Members
                .FirstOrDefault(m => m.IsKind(SyntaxKind.ConstructorDeclaration) && m.IsGenerated());

        private static bool IsGenerated(this MemberDeclarationSyntax m)
            => m.HasLeadingTrivia
                && m.GetLeadingTrivia().Any(trivia =>
                    trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) && trivia.ToString().Contains(RecordComment));

        public static bool IsGetOnlyAuto(this PropertyDeclarationSyntax p)
        {
            if (p.AccessorList == null) return false;
            if (p.AccessorList.Accessors.Count != 1) return false;

            var accessor = p.AccessorList.Accessors[0];

            return accessor != null
                && accessor.Keyword.IsKind(SyntaxKind.GetKeyword)
                && accessor.Body == null
                && p.Initializer == null
                && !p.Modifiers.Any(m => m.IsKind(SyntaxKind.AbstractKeyword));
        }

        /// <summary>
        /// Creates <see cref="TypeDeclarationSyntax"/> with the same identifier as <paramref name="typeDecl"/>, a partial modifier and an empty body.
        /// </summary>
        /// <param name="typeDecl"></param>
        /// <returns></returns>
        public static TypeDeclarationSyntax CreatePartialTypeDelaration(this TypeDeclarationSyntax typeDecl)
        {
            var name = typeDecl.Identifier.Text;

            if (typeDecl.IsKind(SyntaxKind.ClassDeclaration))
            {
                return CSharpSyntaxTree.ParseText($@"
partial class {name}
{{
}}
").GetRoot().ChildNodes().OfType<ClassDeclarationSyntax>().First();
            }
            else
            {
                return CSharpSyntaxTree.ParseText($@"
partial struct {name}
{{
}}
").GetRoot().ChildNodes().OfType<StructDeclarationSyntax>().First();
            }
        }

        /// <summary>
        /// Finds all <see cref="TypeDeclarationSyntax"/>s which have the same identifier as <paramref name="typeDecl"/> from <paramref name="compilation"/>.
        /// </summary>
        /// <param name="typeDecl"></param>
        /// <param name="compilation"></param>
        /// <returns></returns>
        public static IEnumerable<TypeDeclarationSyntax> FindPartialTypeDelarations(this TypeDeclarationSyntax typeDecl, Compilation compilation)
        {
            foreach (var tree in compilation.SyntaxTrees)
            {
                var root = tree.GetRoot();
                var types = root.DescendantNodes(n => !(n is TypeDeclarationSyntax))
                    .OfType<TypeDeclarationSyntax>()
                    .Where(n => n.Identifier.Text == typeDecl.Identifier.Text);

                foreach (var t in types)
                {
                    yield return t;
                }
            }
        }

        public static TypeDeclarationSyntax AddMembers(this TypeDeclarationSyntax typeDecl, params MemberDeclarationSyntax[] members)
        {
            if (typeDecl.IsKind(SyntaxKind.ClassDeclaration))
                return ((ClassDeclarationSyntax)typeDecl).AddMembers(members);
            else
                return ((StructDeclarationSyntax)typeDecl).AddMembers(members);
        }

        private static readonly SyntaxToken PartialToken = SyntaxFactory.Token(SyntaxKind.PartialKeyword);

        public static TypeDeclarationSyntax AddPartialModifier(this TypeDeclarationSyntax typeDecl)
        {
            if (typeDecl.IsKind(SyntaxKind.ClassDeclaration))
            {
                var d = ((ClassDeclarationSyntax)typeDecl);
                if (d.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword))) return d;
                return d.AddModifiers(new[] { PartialToken });
            }
            else
            {
                var d = ((StructDeclarationSyntax)typeDecl);
                if (d.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword))) return d;
                return d.AddModifiers(new[] { PartialToken });
            }
        }
    }

    public class Property
    {
        public IdentifierName Name { get; }

        private PropertyDeclarationSyntax _p;

        public TypeSyntax Type => _p.Type;

        public Property(PropertyDeclarationSyntax p)
        {
            Name = IdentifierName.FromUpper(p.Identifier.Text);
            _p = p;
        }

        public ParameterSyntax ToParameter() => Parameter(
            default(SyntaxList<AttributeListSyntax>),
            default(SyntaxTokenList),
            Type,
            Identifier(Name.Lower),
            EqualsValueClause(DefaultExpression(Type)));

        public ExpressionStatementSyntax ToAssignment() => ExpressionStatement(
            AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                IdentifierName(Name.Upper),
                IdentifierName(Name.Lower)));
    }

    public struct IdentifierName
    {
        public string Lower { get; }
        public string Upper { get; }

        private IdentifierName(string lower, string upper) { Lower = lower; Upper = upper; }

        public static IdentifierName FromUpper(string upper) => new IdentifierName(char.ToLower(upper[0]) + upper.Substring(1, upper.Length - 1), upper);
        public static IdentifierName FromLower(string lower) => new IdentifierName(lower, char.ToUpper(lower[0]) + lower.Substring(1, lower.Length - 1));
    }
}
