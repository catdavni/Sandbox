using CodeGeneration.IL;
using CodeGeneration.Roslyn;

namespace CodeGeneration;

public static class EntryPointCodeGen
{
    public static void CompareFactorial()
    {
        const int bigValue = 40_000;

        Console.WriteLine($"{nameof(Factorial.RunGenerated)} WITH tail optimization started!");
        Factorial.RunGenerated(bigValue, useTailOptimization: true);
        Console.WriteLine($"{nameof(Factorial.RunGenerated)} WITH tail optimization is finished!");
        
        Console.WriteLine($"{nameof(Factorial.RunGenerated)} without tail optimization started!");
        Factorial.RunGenerated(bigValue, useTailOptimization: false);
        Console.WriteLine($"{nameof(Factorial.RunGenerated)} without tail optimization is finished!");
        
        Console.WriteLine($"{nameof(Factorial.RunCompiled)} is started!");
        Factorial.RunCompiled(bigValue, 1);
        Console.WriteLine($"{nameof(Factorial.RunCompiled)} is finished!");
    }

    public static void RunRoslyn()
    {
        var assemblyName = "RazorTemplateAsm";
        var classNames = new[] { "vrum", "piu", "puf" };
        RoslynSandbox.GenerateWithRazor(assemblyName, classNames);
        RoslynSandbox.Run(assemblyName, classNames);
        
        assemblyName = "StringTemplateAsm";
        classNames = new[] { "templateVrum", "templatePiu", "templatePuf" };
        RoslynSandbox.GenerateWithStringTemplate(assemblyName, classNames);
        RoslynSandbox.Run(assemblyName, classNames);
    }
}