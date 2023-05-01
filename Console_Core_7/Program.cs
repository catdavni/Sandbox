//using IlWeaving;

using IlWeaving;
using SharpDxSandbox.Sandbox;



await Direct3DSandbox.StartTriangle();
//await Direct2DSandbox.DrawImage();

Console.WriteLine("Hello, World!");

void RunFactorial()
{
    var fact = TailRecursion.GenerateFacrorial()(int.MaxValue, 1);
    //var fact = TailRecursion.CompiledFactorial(20_000, 1);
    Console.WriteLine(fact);
}
