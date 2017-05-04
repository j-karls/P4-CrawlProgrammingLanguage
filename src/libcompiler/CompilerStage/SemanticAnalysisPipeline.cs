﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using libcompiler.Namespaces;
using libcompiler.Scope;
using libcompiler.SyntaxTree;
using libcompiler.TypeSystem;

namespace libcompiler.CompilerStage
{
    internal class SemanticAnalysisPipeline
    {
        private readonly ConcurrentBag<CompilationMessage> _messages;
        private readonly ConcurrentDictionary<string, Namespace> _allNamespaces;

        private SemanticAnalysisPipeline(ConcurrentBag<CompilationMessage> messages, ConcurrentDictionary<string, Namespace> allNamespaces)
        {
            _messages = messages;
            _allNamespaces = allNamespaces;
        }

        public static Action<AstData> DataCollection(ConcurrentBag<AstData> astDestination,
            ConcurrentBag<CompilationMessage> messages, TargetStage stopAt, ConcurrentDictionary<string, Namespace> allNamespaces)
        {
            SemanticAnalysisPipeline self = new SemanticAnalysisPipeline(messages, allNamespaces);
            Func<AstData, AstData> export = self.AddExport;
            Func<AstData, AstData> collect = export.Then(self.CollectScopeInformation);
            return collect.EndWith(astDestination.Add);

        }

        private AstData AddExport(AstData arg)
        {
            var tu = ((TranslationUnitNode) arg.Tree.RootNode);

            return new AstData(
                arg.TokenStream,
                arg.Filename,
                tu
                    .WithImportedNamespaces(
                        Namespace.Merge(tu.Imports.Select(x => _allNamespaces[x.Module]).ToArray())
                    )
                    .OwningTree
            );

        }

        public static Action<AstData> Analyse(ConcurrentBag<AstData> dastDestination,
            ConcurrentBag<CompilationMessage> messages, TargetStage stopat)
        {
            SemanticAnalysisPipeline self = new SemanticAnalysisPipeline(messages, null);
            Func<AstData, AstData> orderCheck = self.DeclerationOrderCheck;
            Func<AstData, AstData> decorateLeafs = orderCheck.Then(self.PutTypes);
            return decorateLeafs.EndWith(dastDestination.Add);
        }

        private AstData PutTypes(AstData arg)
        {
            return new AstData(
                arg.TokenStream,
                arg.Filename,
                new PutTypeVisitor().Visit(arg.Tree.RootNode).OwningTree);
        }

        private AstData DeclerationOrderCheck(AstData arg)
        {
            new CheckDeclerationOrderVisitor(_messages, arg).Visit(arg.Tree.RootNode);
            return arg;
        }

        private AstData CollectScopeInformation(AstData withoutScope)
        {
            return new AstData(
                withoutScope.TokenStream,
                withoutScope.Filename,
                new AddScopeVisitor().Visit(withoutScope.Tree.RootNode).OwningTree);
        }

        private class AddScopeVisitor : SyntaxRewriter
        {
            protected override CrawlSyntaxNode VisitBlock(BlockNode block)
            {
                BlockScope scope = new BlockScope(block);
                var tmp = ((BlockNode)base.VisitBlock(block)).WithScope(scope);
                return tmp;
            }

            protected override CrawlSyntaxNode VisitMethodDecleration(MethodDeclerationNode methodDecleration)
            {
                GenericScope scope = new GenericScope(
                    methodDecleration
                        .Parameters
                        .Select(
                            parameter =>
                                new KeyValuePair<string, TypeInformation>(parameter.Value,
                                    new TypeInformation(
                                        null, 
                                        ProtectionLevel.NotApplicable, 
                                        parameter.Interval.b
                                    )
                                )
                        )
                );

                MethodDeclerationNode afterupdate = methodDecleration.Update(methodDecleration.Interval,
                    methodDecleration.ProtectionLevel, scope,
                    methodDecleration.MethodSignature, methodDecleration.Body, methodDecleration.Identifier,
                    methodDecleration.Parameters, methodDecleration.GenericParameters);
                return base.VisitMethodDecleration(afterupdate);
            }

            protected override CrawlSyntaxNode VisitConstructor(ConstructorNode constructor)
            {
                return base.VisitConstructor(constructor);
            }

            protected override CrawlSyntaxNode VisitClassTypeDecleration(ClassTypeDeclerationNode classTypeDecleration)
            {
                return base.VisitClassTypeDecleration(classTypeDecleration);
            }

            //NB: This one might break, there was talk about refactoring all for loops to while loops behind the scenes
            protected override CrawlSyntaxNode VisitForLoop(ForLoopNode forLoop)
            {
                return base.VisitForLoop(forLoop);
            }


        }
    }

    internal class PutTypeVisitor : SyntaxRewriter
    {
        protected override CrawlSyntaxNode VisitType(TypeNode type)
        {
            IScope scope = type.FindFirstScope();
            CrawlType actualType = CrawlType.ParseDecleration(scope, type.TypeName);

            var v =  type.WithActualType(actualType);
            return v;
        }
    }


    internal class CheckDeclerationOrderVisitor : SyntaxVisitor
    {
        private readonly ConcurrentBag<CompilationMessage> _messages;
        private readonly AstData _data;

        public CheckDeclerationOrderVisitor(ConcurrentBag<CompilationMessage> messages, AstData data)
        {
            _messages = messages;
            _data = data;
        }

        public override void Visit(CrawlSyntaxNode node)
        {
            if (node is IScope)
            {
                CheckHides(node);      
            }

            base.Visit(node);
        }

        protected override void VisitVariable(VariableNode nodes)
        {
            IScope scope = nodes.FindFirstScope();
            TypeInformation[] decl =  scope.FindSymbol(nodes.Name);
            if (decl == null || decl.Length == 0 )
            {
                //TODO: Edit lenght on everything...
                _messages.Add(CompilationMessage.Create(_data.TokenStream, nodes.Interval, MessageCode.NoSuchSymbol,
                    _data.Filename, null));
            }
            else if (decl.Length == 1)
            {
                //TODO: If TypeInformation needs a ScopeType that contains MethodLine and ClassLike. Supress for classLike
                if (decl[0].DeclarationLocation > nodes.Interval.a)
                {
                    //TODO: Make CompilationMessage.Create take an IntervalSet of intresting locations instead of one location...
                    _messages.Add(CompilationMessage.Create(_data.TokenStream, nodes.Interval, MessageCode.UseBeforeDecleration, _data.Filename, null));
                }
            }
            else //if(decl.Length > 1)
            {
                _messages.Add(CompilationMessage.Create(_data.TokenStream, nodes.Interval,
                    MessageCode.InternalCompilerError, _data.Filename, "Not allowed due technical (lazy) reasons",
                    MessageSeverity.Fatal));
            }
            
            


            base.VisitVariable(nodes);
        }

        void CheckHides(CrawlSyntaxNode aScope)
        {
            //TODO: This should be optimized by adding and removing from mapping with a stack
            //Would go from O(n^2) -> O(1)
            //This checks if something gets redefined.
            Dictionary<string, CrawlSyntaxNode> mapping = new Dictionary<string, CrawlSyntaxNode>();
            while (aScope != null)
            {
                IScope scope = aScope as IScope;
                if (scope != null)
                {
                    var local = scope.LocalSymbols();

                    //TODO: REMOVE. Just to stop it crashing
                    if (local == null)
                        local = new List<string>();

                    foreach (string symbol in local)
                    {
                        if (mapping.ContainsKey(symbol))
                        {
                            Interval first = mapping[symbol].Interval;
                            _messages.Add(CompilationMessage.Create(_data.TokenStream, first,
                                MessageCode.HidesOtherSymbol, _data.Filename,
                                $"Unable to declare {symbol} as it hides {symbol} from {aScope}"));
                        }
                        else
                        {
                            mapping.Add(symbol, aScope);
                        }
                    }
                }

                aScope = aScope.Parent;
            }
        }
    }
}