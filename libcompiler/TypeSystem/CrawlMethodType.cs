﻿using System.Collections.Generic;
using System.Linq;
using libcompiler.ExtensionMethods;
using libcompiler.SyntaxTree.Nodes;

namespace libcompiler.TypeSystem
{
    public class CrawlMethodType : CrawlType
    {
        public CrawlType ReturnType { get; }
        public List<CrawlType> Parameters { get; }

        public CrawlMethodType(MethodDeclerationNode decleration, CrawlType returnType, IEnumerable<CrawlType> parameters)
            : base(decleration.Identfier.Name, decleration.FindNameSpace().Package)
        {
            ReturnType = returnType;
            Parameters = parameters.ToList();
        }

        public override bool IsAssignableTo(CrawlType target)
        {
            return Equals(target);
        }

        public override bool ImplicitlyCastableTo(CrawlType target)
        {
            return Equals(target);
        }

        public override bool CastableTo(CrawlType target)
        {
            return Equals(target);
        }
    }
}