using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Channels;

namespace Analyzer
{
    public class Walker : CSharpSyntaxWalker
    {
        private readonly SemanticModel smodel;
        private readonly List<Reference> references;

        public Walker(SemanticModel semanticModel)
        {
            this.smodel = semanticModel;
            this.references = new List<Reference>();
        }

        public IReadOnlyCollection<Reference> GetReferences() => references.AsReadOnly();

        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            var from = GetContainingDeclaration(node);

            if (!node.IsVar && !(from is null))
            {
                var symbol = smodel.GetSymbolInfo(node).Symbol;

                if (symbol is null)
                {
                    if (!IsConstantValue(node))
                    {
                        throw new InvalidOperationException($"Not identified: {node.Identifier} in {node.Parent} at {node.GetLocation().GetLineSpan()}");
                    }
                }
                else
                {
                    var to = GetContainingTypeSymbol(symbol);
                    if (to is null)
                    {
                        throw new InvalidOperationException($"No containing type symbol found: {node.Identifier} in {node.Parent}");
                    }
                    var fromSym = smodel.GetDeclaredSymbol(from);
                    references.Add(new Reference(FullName(fromSym), FullName(to)));
                }
            }

            base.VisitIdentifierName(node);
        }

        private bool IsConstantValue(IdentifierNameSyntax node)
        {
            //return smodel.GetConstantValue(node.Parent).HasValue;
            if (smodel.GetConstantValue(node.Parent).HasValue)
                return true;
            var expr = node.FirstAncestorOrSelf<InvocationExpressionSyntax>();
            if (!(expr is null) && smodel.GetConstantValue(expr).HasValue)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns the symbol of the containing type.
        /// </summary>
        private static INamespaceOrTypeSymbol GetContainingTypeSymbol(ISymbol symbol)
        {
            if (symbol is INamespaceOrTypeSymbol type)
            {
                return type;
            }
            else
            {
                var cont = symbol.ContainingType;

                if (cont is null)
                {
                    return null;
                }
                else
                {
                    return cont;
                }
            }
        }

        /// <summary>
        /// Returns the node of the declaration which includes this node.
        /// </summary>
        private static TypeDeclarationSyntax GetContainingDeclaration(IdentifierNameSyntax node)
        {
            var decl = node.FirstAncestorOrSelf<TypeDeclarationSyntax>();
            if (decl is null)
            {
                // The name is not used within type declaration. It could be e.g. a namespace decl.
                // Here we are not interested in these.
                return null;
            }

            return decl;
        }

        private static string FullName(TypeDeclarationSyntax syntax)
        {
            return syntax.Identifier.Text;
        }

        private static string FullName(ISymbol symbol)
        {
            var ct = symbol.ContainingType;
            if (ct is null)
            {
                return GetFullNamespaceName(symbol.ContainingNamespace) + "." + symbol.Name;
            }
            return FullName(ct) + "." + symbol.Name;
        }

        private static string GetFullNamespaceName(ISymbol symbol)
        {
            var cns = symbol.ContainingNamespace;
            if (cns is null) return symbol.Name;
            var n = GetFullNamespaceName(cns);
            return n == "" ? symbol.Name : n + "." + symbol.Name;
        }

        public class Reference
        {
            public Reference(string from, string to)
            {
                From = from;
                To = to;
            }

            public string From { get; }
            public string To { get; }
        }
    }
}
