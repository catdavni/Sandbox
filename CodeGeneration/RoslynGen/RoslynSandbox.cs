using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using CodeGeneration.RoslynGen;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using RazorLight;

namespace CodeGeneration.Roslyn;

public record SomeModel(string ClassName, string Inherit, string Expression);

public class RoslynSandbox
{
    public static void GenerateWithRazor(string assemblyName, string[] classNames)
    {
        var templateFolderName = Path.Combine("RoslynGen", "Templates");
        var refs = AppDomain.CurrentDomain.GetAssemblies().Select(a => MetadataReference.CreateFromFile(a.Location)).ToList();

        var engine = new RazorLightEngineBuilder()
            .UseFileSystemProject(Path.Combine(Directory.GetCurrentDirectory(), templateFolderName))
            .UseMemoryCachingProvider()
            .SetOperatingAssembly(Assembly.GetExecutingAssembly())
            .Build();

        var classes = classNames.Select(name => engine.CompileRenderAsync(
                "SimpleClassTemplate",
                new SomeModel(name,
                    nameof(ISomeInterface),
                    "Console.WriteLine(\"ola\");"))
            .Result);
        var classesSyntaxTrees = classes.Select(c => CSharpSyntaxTree.ParseText(c));

        // var text = engine.CompileRenderAsync(
        //         "SimpleClassTemplate",
        //         //Path.ChangeExtension("SimpleClassTemplate", "cshtml"), 
        //         new SomeModel("PiuLolo",
        //             nameof(ISomeInterface),
        //             "Console.WriteLine(\"ola\");"))
        //     .Result;

        var compilation = CSharpCompilation.Create(
            assemblyName,
            classesSyntaxTrees,
            references: refs,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, moduleName: assemblyName));

        using var assemblyStream = new FileStream(Path.ChangeExtension(assemblyName, "dll"), FileMode.Create);
        //using var symbolsStream = new FileStream(Path.ChangeExtension(AssemblyName, "pdb"), FileMode.Create);
        //var result = compilation.Emit(assemblyStream, symbolsStream);
        var result = compilation.Emit(assemblyStream);

        HandleResult(result);
    }

    public static void GenerateWithStringTemplate(string assemblyName, string[] classNames)
    {
        var encoding = Encoding.UTF8;

        var syntaxTrees = new List<(SyntaxTree CodeTree, EmbeddedText EmbededMetadata)>();
        foreach (var name in classNames)
        {
            var sourceCodePath = $"{name}.cs";
            var buffer = encoding.GetBytes(GenerateClass(name));

            var sourceText = SourceText.From(buffer, buffer.Length, encoding, canBeEmbedded: true);

            var syntaxTree = CSharpSyntaxTree.ParseText(
                sourceText,
                //new CSharpParseOptions(),
                path: sourceCodePath);

            //var syntaxRootNode = syntaxTree.GetRoot() as CSharpSyntaxNode;
            //var encoded = CSharpSyntaxTree.Create(syntaxRootNode, null, sourceCodePath, encoding);
            var embeddedText = EmbeddedText.FromSource(sourceCodePath, sourceText);

            syntaxTrees.Add((syntaxTree, embeddedText));
        }

        var refs = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !string.IsNullOrEmpty(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location)).ToList();
        var symbolsFileName = Path.ChangeExtension(assemblyName, "pdb");
        var optimizationLevel = OptimizationLevel.Debug;

        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName,
            syntaxTrees: syntaxTrees.Select(t => t.Item1),
            references: refs,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithOptimizationLevel(optimizationLevel)
                .WithPlatform(Platform.AnyCpu)
        );

        using var assemblyStream = new FileStream(Path.ChangeExtension(assemblyName, "dll"), FileMode.Create);
        using var symbolsStream = new FileStream(Path.ChangeExtension(assemblyName, "pdb"), FileMode.Create);
        {
            var emitOptions = new EmitOptions(
                debugInformationFormat: DebugInformationFormat.PortablePdb);

            EmitResult result = compilation.Emit(
                peStream: assemblyStream,
                pdbStream: symbolsStream,
                embeddedTexts: syntaxTrees.Select(t => t.EmbededMetadata),
                options: emitOptions);

            HandleResult(result);
        }
    }

    public static void Run(string assemblyName, string[] classNames)
    {
        var assembly = Assembly.LoadFrom(Path.ChangeExtension(assemblyName, "dll"));

        foreach (var cn in classNames)
        {
            var type = assembly.GetType(cn);
            var ctor = type.GetConstructor(BindingFlags.Public | BindingFlags.Instance, new[] { typeof(string), typeof(int) });
            var instance = (ISomeInterface)ctor.Invoke(new object?[] { "Piu", 333 });
            instance.SomeMethod();
        }
    }

    private static void HandleResult(EmitResult result)
    {
        if (!result.Success)
        {
            var errors = new List<string>();

            IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                diagnostic.IsWarningAsError ||
                diagnostic.Severity == DiagnosticSeverity.Error);

            foreach (Diagnostic diagnostic in failures)
                errors.Add($"{diagnostic.Id}: {diagnostic.GetMessage()}");

            throw new Exception(String.Join("\n", errors));
        }
    }

    private static string GenerateClass(string name)
    {
        return
            $$"""
        using CodeGeneration.RoslynGen;
        using System;
        
        public sealed class {{name}} : ISomeInterface
        {
            public {{name}}(string someString, int someInt)
            {
                SomeString = someString;
                SomeInt = someInt;
            }

            public string SomeString { get; }

            public int SomeInt { get; }

            public void SomeMethod()
            {
                var a = DateTime.Now.Hour;
                var b = DateTime.Now.DayOfWeek;
                var c = a + b;
                Console.WriteLine(c);
                Console.WriteLine(c);
            }
        }
        """;
    }
}