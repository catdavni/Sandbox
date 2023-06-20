using System.Numerics;
using System.Reflection.Emit;
using System.Reflection;
using GrEmit;

namespace IlWeaving;

public static class Factorial
{
    public static BigInteger RunGenerated(BigInteger n, bool useTailOptimization) => GenerateFactorial(useTailOptimization)(n, n, 1);

    public static BigInteger RunCompiled(BigInteger n, BigInteger acc) => n != 0 && n != 1 ? RunCompiled(n - 1, acc * n) : acc;

    
    // MAGIC
    private delegate BigInteger RunFactorial(BigInteger initial, BigInteger current, BigInteger accumulator);

    private static RunFactorial GenerateFactorial(bool useTailOptimization)
    {
        var assemblyName = new AssemblyName("TailRecursionExperiment");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name!);

        var type = moduleBuilder.DefineType("Factorial", TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit);
        var method = type.DefineMethod("TailOptimization", MethodAttributes.HideBySig | MethodAttributes.Static | MethodAttributes.Public, typeof(BigInteger), new[] { typeof(BigInteger), typeof(BigInteger), typeof(BigInteger) });
        method.DefineParameter(1, ParameterAttributes.None, "initial"); // used for troubleshooting
        method.DefineParameter(1, ParameterAttributes.None, "current");
        method.DefineParameter(2, ParameterAttributes.None, "acc");

        using (var il = new GroboIL(method))
        {
            var exit = il.DefineLabel("exit");

            il.Ldarg(1);
            il.Ldc_I8(0);
            il.Call(typeof(BigInteger).GetMethod("op_Equality", BindingFlags.Public | BindingFlags.Static, new[] { typeof(BigInteger), typeof(long) }));
            il.Brtrue(exit);

            il.Ldarg(1);
            il.Ldc_I8(1);
            il.Call(typeof(BigInteger).GetMethod("op_Equality", BindingFlags.Public | BindingFlags.Static, new[] { typeof(BigInteger), typeof(long) }));
            il.Brtrue(exit);

            il.Ldarg(0);
            il.Ldarg(1);
            il.Ldc_I8(1);
            il.Call(typeof(BigInteger).GetMethod("op_Implicit", BindingFlags.Public | BindingFlags.Static, new[] { typeof(long) }));
            il.Call(typeof(BigInteger).GetMethod("op_Subtraction", BindingFlags.Public | BindingFlags.Static, new[] { typeof(BigInteger), typeof(BigInteger) }));
            il.Ldarg(2);
            il.Ldarg(1);
            il.Call(typeof(BigInteger).GetMethod("op_Multiply", BindingFlags.Public | BindingFlags.Static, new[] { typeof(BigInteger), typeof(BigInteger) }));
            il.Call(method, tailcall: useTailOptimization);
            il.Ret();

            il.MarkLabel(exit);
            il.Ldarg(2);
            il.Ret();
        }

        var compiled = type.CreateType();
        var compiledMethod = compiled.GetMethod(method.Name, BindingFlags.Public | BindingFlags.Static);
        new Lokad.ILPack.AssemblyGenerator().GenerateAssembly(assemblyBuilder, assemblyName.Name + ".dll");

        return compiledMethod!.CreateDelegate<RunFactorial>();
    }
}