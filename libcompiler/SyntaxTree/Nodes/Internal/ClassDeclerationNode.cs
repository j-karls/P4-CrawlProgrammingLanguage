﻿using System;
using Antlr4.Runtime.Misc;

namespace libcompiler.SyntaxTree.Nodes.Internal
{
    public class ClassDeclerationNode : DeclerationNode
    {
        public TokenNode Identifier { get; }
        public BlockNode BodyBlock { get; }

        public ClassDeclerationNode(Interval interval, ProtectionLevel protectionLevel, TokenNode identifier, BlockNode bodyBlock) : base(interval, NodeType.ClassDecleration, protectionLevel)
            
        {
            Identifier = identifier;
            BodyBlock = bodyBlock;
        }

        public override GreenNode GetChildAt(int slot)
        {
            switch (slot)
            {
                case 0: return Identifier;
                case 1: return BodyBlock;
                default:
                    return null;
            }
        }

        public override CrawlSyntaxNode CreateRed(CrawlSyntaxNode parent, int indexInParent)
        {
            return new Nodes.ClassDeclerationNode(parent, this, indexInParent);
        }

        internal override GreenNode WithReplacedChild(GreenNode newChild, int index)
        {
            if(index == 0)
                return new ClassDeclerationNode(this.Interval, ProtectionLevel, (TokenNode)newChild, BodyBlock);
            else if(index == 1)
                return new ClassDeclerationNode(this.Interval, ProtectionLevel, Identifier, (BlockNode)newChild);

            throw new ArgumentOutOfRangeException();
        }
    }
}