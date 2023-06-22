namespace CodeGeneration;

public static class EntryPointIlWeaving
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
}