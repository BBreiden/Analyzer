using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

namespace Analyzer
{
    public class Walker : CSharpSyntaxWalker
    {
        private readonly SemanticModel smodel;

        public Walker(SemanticModel semanticModel)
        {
            this.smodel = semanticModel;
        }

        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            var symbol = smodel.GetSymbolInfo(node).Symbol;

            if (node.IsVar)
            {
                var s = DateTime.Now;
            }

            if (symbol is null)
            {
                System.Console.WriteLine($"{node.Identifier}: not identified.");
            }
            else
            {
                if (node.IsVar)
                {
                    System.Console.WriteLine($"{node.Identifier} {symbol.ContainingAssembly}");
                }
            }

            base.VisitIdentifierName(node);
        }
    }
}
