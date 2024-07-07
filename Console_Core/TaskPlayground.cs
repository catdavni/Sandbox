namespace AConsoleCoreRunner;

public class TaskPlayground
{
    public static async Task DifferenceBetweenFactoryAndRun()
    {
        var a = await Task.Factory.StartNew(async () =>
        {
            Console.WriteLine("starting from factory");
            await Task.Delay(1000);
            Console.WriteLine("ending from factory");
            return 1;
        });
        Console.WriteLine($"a: {a}");

        var b = await Task.Run(async () =>
        {
            Console.WriteLine("starting from run");
            await Task.Delay(1000);
            Console.WriteLine("ending from run");
            return 2;
        });
        Console.WriteLine($"b: {b}");
    }
}