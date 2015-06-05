using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

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
            var accessor = p.AccessorList?.Accessors.SingleOrDefault();

            return accessor != null
                && accessor.Keyword.IsKind(SyntaxKind.GetKeyword)
                && accessor.Body == null
                && p.Initializer == null;
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
