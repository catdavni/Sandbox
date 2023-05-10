//using IlWeaving;

using IlWeaving;
using SharpDxSandbox.Sandbox;

//RunFactorial();

//await Direct3DSandbox.StartTest();
await Direct3DSandbox.RotatingCube();
//await Direct2DSandbox.DrawImage();

Console.WriteLine("Hello, World!");

void RunFactorial()
{
    var fact = TailRecursion.RunGeneratedFactorial(40_000, true);
    //var fact = TailRecursion.CompiledFactorial(30_000, 1);
    Console.WriteLine(fact);
}
