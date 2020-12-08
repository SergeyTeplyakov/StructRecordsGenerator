using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace StructRecordGenerator.Tests
{
    public class NUnitTestOutputHelper : ITestOutputHelper
    {
        public void WriteLine(string message)
        {
            Console.WriteLine(message);
        }

        public void WriteLine(string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }
    }

    public class GeneratorTestHelper<TGenerator> where TGenerator : ISourceGenerator, new()
    {
        private readonly ITestOutputHelper _output;

        public GeneratorTestHelper(ITestOutputHelper? output = null)
        {
            _output = output ?? new NUnitTestOutputHelper();
        }

        public ImmutableArray<Diagnostic> GetGeneratedDiagnostics(string source)
        {
            CSharpCompilation compilation = Compile(source);

            ThrowIfInvalid(compilation, before: true);

            ISourceGenerator generator = new TGenerator();

            var driver = CSharpGeneratorDriver.Create(generator);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var generateDiagnostics);

            _output.WriteLine($"Diagnostics.Count: {generateDiagnostics.Length}");

            foreach(var d in generateDiagnostics)
            {
                _output.WriteLine(d.ToString());
            }

            return generateDiagnostics;
        }

        public string GetGeneratedOutput(string source)
        {
            CSharpCompilation compilation = Compile(source);

            ThrowIfInvalid(compilation, before: true);

            ISourceGenerator generator = new TGenerator();

            var driver = CSharpGeneratorDriver.Create(generator);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generatedDiagnostics);

            // Intentionally writing the output before potentially generating an exception if diagnostics are presented.
            string output = outputCompilation.SyntaxTrees.Last().ToString();
            _output.WriteLine($"Generated output:" + Environment.NewLine + output);

            if (generatedDiagnostics.Length != 0)
            {
                _output.WriteLine("Generated diagnostics:");
                _output.WriteLine(string.Join(Environment.NewLine, generatedDiagnostics.Select(d => d.ToString())));

                generatedDiagnostics = generatedDiagnostics.Where(d => d.Severity is DiagnosticSeverity.Warning or DiagnosticSeverity.Error).ToImmutableArray();

                // Throw only when there are warnings or errors.
                if (generatedDiagnostics.Length != 0)
                {
                    // The tests does not expect any diagnostics before running the generator!
                    string errors = string.Join(Environment.NewLine, generatedDiagnostics);
                    throw new InvalidOperationException($"Unexpected diagnostics from source generators: {errors}");
                }
            }

            ThrowIfInvalid(outputCompilation, before: false);

            return output;
        }

        private static CSharpCompilation Compile(string source)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source);

            var references = new List<MetadataReference>();
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                if (!assembly.IsDynamic && assembly.Location.IsNotNullOrEmpty())
                {
                    references.Add(MetadataReference.CreateFromFile(assembly.Location));
                }
            }

            CSharpCompilationOptions options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            var compilation = CSharpCompilation.Create("foo", new SyntaxTree[] { syntaxTree }, references, options);
            return compilation;
        }

        private static void ThrowIfInvalid(Compilation compilation, bool before)
        {
            // Excluding error CS0518: Predefined type 'System.Runtime.CompilerServices.IsExternalInit' is not defined or imported	Microsoft.CodeAnalysis.DiagnosticInfo {Microsoft.CodeAnalysis.CSharp.CSDiagnosticInfo}
            var diagnostics = compilation
                .GetDiagnostics()
                // Tracking only errors or the custom diagnostics from the current project
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Where(d => d.Id != "CS0518")
                .ToList();
            
            if (before)
            {
                // For 'before' case, excluding the error CS0246 because 'StructGenerators' namespace actually is not defined for the original compilation
                // (it would be added by the generator itself).
                diagnostics = diagnostics.Where(d => d.Id != "CS0246" && d.GetMessage().Contains("The type or namespace \"StructGenerators\"")).ToList();
            }

            if (diagnostics.Count != 0)
            {
                // The tests does not expect any diagnostics before running the generator!
                string errors = string.Join(Environment.NewLine, diagnostics);
                string extraMessage = before ? "Original code is invalid" : "Generated code is invalid";
                throw new InvalidOperationException($"{extraMessage}: {errors}");
            }
        }
    }
}
