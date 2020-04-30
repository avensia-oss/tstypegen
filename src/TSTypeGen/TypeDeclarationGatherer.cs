using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TSTypeGen
{
    internal class TypeDeclarationGatherer : CSharpSyntaxWalker
    {
        private readonly SemanticModel _semanticModel;
        public List<INamedTypeSymbol> Result { get; } = new List<INamedTypeSymbol>();

        public TypeDeclarationGatherer(SemanticModel semanticModel) {
            _semanticModel = semanticModel;
        }

        private void Add(INamedTypeSymbol s)
        {
            if (s != null)
                Result.Add(s);
        }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            Add(_semanticModel.GetDeclaredSymbol(node));
            base.VisitClassDeclaration(node);
        }

        public override void VisitStructDeclaration(StructDeclarationSyntax node)
        {
            Add(_semanticModel.GetDeclaredSymbol(node));
            base.VisitStructDeclaration(node);
        }

        public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            Add(_semanticModel.GetDeclaredSymbol(node));
            base.VisitInterfaceDeclaration(node);
        }

        public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
        {
            Add(_semanticModel.GetDeclaredSymbol(node));
            base.VisitEnumDeclaration(node);
        }

        public override void VisitDelegateDeclaration(DelegateDeclarationSyntax node)
        {
            Add(_semanticModel.GetDeclaredSymbol(node));
            base.VisitDelegateDeclaration(node);
        }
    }
}