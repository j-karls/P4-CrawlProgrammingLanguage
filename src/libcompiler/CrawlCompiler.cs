﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using libcompiler.CompilerStage;

namespace libcompiler
{
    public static class CrawlCompiler
    {
        public static CompilationResult Compile(CrawlCompilerConfiguration configuration)
        {
            TraceListners.AssemblyResolverListner =
                new System.Diagnostics.TextWriterTraceListener(Utils.GetPrimaryOutputStream(configuration));
            var importedstuff = Namespace.NamespaceLoader.LoadAll(configuration.Assemblies);

            //The ConcurrentBag is an unordered 

            ConcurrentBag<CompilationMessage> messages = new ConcurrentBag<CompilationMessage>();
            CompilationStatus status = CompilationStatus.Success;

            try
            {
                ConcurrentBag<AstData> parsedFiles = new ConcurrentBag<AstData>();
                bool parallel = !configuration.ForceSingleThreaded;

                Execute(configuration.Files, ParsePipeline.CreateParsePipeline(parsedFiles, messages, configuration.TargetStage), parallel);


                //TODO: Collect information on referenced assemblies (This can actually be started in the background asap)

                //TODO: Semantic analysis
                ConcurrentBag<AstData> filesWithScope = new ConcurrentBag<AstData>();
                Execute(parsedFiles, SemanticAnalysisPipeline.DataCollection(filesWithScope, messages, configuration.TargetStage), parallel);

                ConcurrentBag<AstData> decoratedAsts = new ConcurrentBag<AstData>();
                Execute(filesWithScope, SemanticAnalysisPipeline.Analyse(decoratedAsts, messages, configuration.TargetStage), parallel);


                //TODO: Interpeter or code generation

                //Until meaningfull end, print everything

                Execute(filesWithScope, Utils.GetPrimaryOutputStream(configuration).WriteLine, parallel);

                if (messages.Count(message => message.Severity >= MessageSeverity.Error) > 0)
                    status = CompilationStatus.Failure;
                
            }
            catch (Exception e) when(!Debugger.IsAttached)
            {
                
                messages.Add(CompilationMessage.CreateNonCodeMessage(MessageCode.InternalCompilerError, e.ToString(), MessageSeverity.Fatal));
                status = CompilationStatus.Failure;
                
            }

            return new CompilationResult(status, messages);
        }

        private static void Execute<TIn>(IEnumerable<TIn> indata, Action<TIn> action, bool parallel) where TIn : class
        {
            if (parallel)
            {
                Task.WaitAll(indata.Select(item => Task.Run(() => action(item))).ToArray());
            }
            else
            {
                foreach (TIn @in in indata)
                {
                    action(@in);
                }
            }
        }
    }
}
 