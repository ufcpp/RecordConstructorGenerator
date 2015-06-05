using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RecordConstructorGenerator
{
    public static class SyntaxExtensions
    {
        const string RegionComment = "Record Constructor";

        public static ConstructorDeclarationSyntax GetRecordConstructor(this TypeDeclarationSyntax typeDecl)
            => (ConstructorDeclarationSyntax)typeDecl.Members
                .FirstOrDefault(m => m.IsKind(SyntaxKind.ConstructorDeclaration) && m.IsGenerated());

        private static bool IsGenerated(this MemberDeclarationSyntax m)
            => m.HasLeadingTrivia
                && m.GetLeadingTrivia().Any(trivia =>
                    trivia.IsKind(SyntaxKind.RegionDirectiveTrivia) && trivia.ToString().Contains(RegionComment));
    }
}
