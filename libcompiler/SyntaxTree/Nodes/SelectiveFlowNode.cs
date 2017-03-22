﻿using System;

namespace libcompiler.SyntaxTree.Nodes
{
    public class SelectiveFlowNode : FlowNode
    {
        private ExpressionNode _check;
        private BlockNode _1;
        private BlockNode _2;

        public ExpressionNode Check => GetRed(ref _check, 0);
        public BlockNode Primary => GetRed(ref _1, 1);
        public BlockNode Alternative => GetRed(ref _2, 2);

        public SelectiveFlowNode(CrawlSyntaxNode parent, Internal.SelectiveFlowNode self, int indexInParent) : base(parent, self, indexInParent)
        {
            
        }

        private static NodeType MakeNodeType(FlowType type)
        {
            switch (type)
            {
                case FlowType.If:
                    return NodeType.If;
                case FlowType.IfElse:
                    return NodeType.IfElse;
                case FlowType.While:
                    return NodeType.While;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public enum FlowType
        {
            If,
            IfElse,
            While,
        }

        public override CrawlSyntaxNode GetChildAt(int index)
        {
            switch (index)
            {
                case 0: return Check;
                case 1: return Primary;
                case 2: return Alternative;
                default: return default(CrawlSyntaxNode);
            }
        }
    }
}