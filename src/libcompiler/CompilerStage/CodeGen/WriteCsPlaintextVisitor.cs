﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using libcompiler.SyntaxTree;

namespace libcompiler.CompilerStage.CodeGen
{
    public class WriteCsPlaintextVisitor : SimpleSyntaxVisitor<string>
    {
        string VisitAndAddDelimiters<T>(ListNode<T> arguments, string delimiter) where T : CrawlSyntaxNode
        {
            var sb = new StringBuilder();
            for (int i = 0; i < arguments.Count(); i++)
            {
                if (i != 0) sb.Append(delimiter);
                sb.Append(Visit(arguments[i]));
            }
            return sb.ToString();
        }

        protected override string Combine(params string[] parts)
        {
            StringBuilder sb = new StringBuilder();

            foreach (string part in parts)
            {
                sb.Append(part);
            }
            return sb.ToString();
        }

        protected override string VisitBlock(BlockNode block)
        {
            StringBuilder sb = new StringBuilder();

            foreach (string child in block.Select(Visit))
            {
                sb.Append($"\n{child}\n");
            }
            return sb.ToString();
        }

        protected override string VisitVariableDecleration(VariableDeclerationNode node)
        {
            string type = Visit(node.DeclerationType);
            string decls = Visit(node.Declerations);
            StringBuilder sb = new StringBuilder();

            foreach (string decl in decls.Split(','))
            {
                if(decl.Length>0)
                    sb.Append($"{type} {decl};\n\n");
            }

            return sb.ToString();
        }

        protected override string VisitType(TypeNode node)
        {
            return node.ActualType.Identifier;
        }

        protected override string VisitIdentifier(IdentifierNode node)
        {
            return node.Value;
        }

        protected override string VisitBooleanLiteral(BooleanLiteralNode node)
        {
            return node.Value.ToString();
        }

        protected override string VisitStringLiteral(StringLiteralNode node)
        {
            return node.Value;
        }

        protected override string VisitRealLiteral(RealLiteralNode node)
        {
            return node.Value.ToString(CultureInfo.GetCultureInfo("en-GB"));
        }

        protected override string VisitSingleVariableDecleration(SingleVariableDeclerationNode node)
        {
            string identifier = Visit(node.Identifier);
            if (node.DefaultValue != null)
            {
                string defaultValue = Visit(node.DefaultValue);
                return $"{identifier} = {defaultValue},";
            }
            else
            {
                return $"{identifier},";
            }
            return base.VisitSingleVariableDecleration(node);
        }

        protected override string VisitVariable(VariableNode node)
        {
            return node.Name;
        }

        protected override string VisitIntegerLiteral(IntegerLiteralNode node)
        {
            return node.Value.ToString();
        }

        protected override string VisitMultiChildExpression(MultiChildExpressionNode node)
        {
            string delimiter;
            switch (node.ExpressionType)
            {
                case ExpressionType.Add:
                    delimiter = " + ";
                    break;
                case ExpressionType.Subtract:
                    delimiter = " - ";
                    break;
                case ExpressionType.Divide:
                    delimiter = " / ";
                    break;
                case ExpressionType.Multiply:
                    delimiter = " * ";
                    break;
                case ExpressionType.Power:
                    return WritePowerExpression(node.Arguments.ToList());
                default: return "OPERATOR_ERR ";
            }
            return VisitAndAddDelimiters(node.Arguments, delimiter);
        }

        private string WritePowerExpression(List<ExpressionNode> arguments /*, int recursion = 0*/)
        {
            return InsertVisitsRecursivelyUntilTwoRemain(arguments, "System.Math.Pow(", ", ", ")");

            #region DeprecatedCode
            //if (arguments.Count - recursion == 2)
            //{
            //    // If there are only two left, then the recursion is done
            //    return $"System.Math.Pow({Visit(arguments[recursion])}, {Visit(arguments[recursion + 1])})";
            //}
            //return $"System.Math.Pow({Visit(arguments[recursion])}, {WritePowerExpression(arguments, recursion + 1)})";
            #endregion
        }

        private string InsertVisitsRecursivelyUntilTwoRemain(List<ExpressionNode> arguments,
            string front = "", string middle = "", string end = "", bool rightRecursion = true, int currentRecursion = 0)
        {
            // A bit tought to understand, so take a look at the deprecated code in the WritePowerExpression method in stead:
            // This is simply a more widely applicable version of the same concept.

            if (arguments.Count - currentRecursion == 2)
            {
                // If there are only two arguments left, then the recursion is done
                return front + Visit(arguments[currentRecursion]) + middle + Visit(arguments[currentRecursion + 1]) + end;
            }
            if(rightRecursion)
                return front + Visit(arguments[currentRecursion]) + middle + 
                    InsertVisitsRecursivelyUntilTwoRemain(arguments, front, middle, end, rightRecursion, currentRecursion + 1) + end;
            // Otherwise its a left recursion, so we reverse the order
            return front + InsertVisitsRecursivelyUntilTwoRemain(arguments, front, middle, end, rightRecursion, currentRecursion + 1)
                + middle + Visit(arguments[currentRecursion]) + end;
        }

        protected override string VisitCall(CallNode node)
        {
            string target = Visit(node.Target);
            string arg = VisitAndAddDelimiters(node.Arguments, ", ");
            return $"{target}({arg})";
        }

        protected override string VisitCastExpression(CastExpressionNode node)
        {
            return $"({Visit(node.TypeToConvertTo)}) {Visit(node.Target)}";
        }

        protected override string VisitArrayConstructor(ArrayConstructorNode node)
        {
            //string type = node.Target.ActualType.
            
            return "new {type} " + "{ ARGUMENTS }";
        }

        protected override string VisitAssignment(AssignmentNode node)
        {
            string target = Visit(node.Target);
            string val = Visit(node.Value);
            return $"{target} = {val};";
        }
        /*    Der skal findes en måde hvorpå funktionen kan kaldes rekursivt siden man aldrig vil vide
         *    hvor mange members der er.
        protected override string VisitMemberAccess(MemberAccessNode node)
        {
            string target = Visit(node.Target);
            string member = Visit(node.Member);
            return $"{target}.{member}";
        }
        */
    }
}