using System.Reflection.Emit;
using System.Reflection;

namespace IlWeaving;

public static class TailRecursion
{
    public static Func<ulong, ulong, ulong> GenerateFacrorial()
    {
        var assemblyName = new AssemblyName("PiuPiu");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name!);

        var type = moduleBuilder.DefineType("Ololo", TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit);
        var method = type.DefineMethod("TailFactorial", MethodAttributes.HideBySig | MethodAttributes.Static | MethodAttributes.Public, typeof(ulong), new[] { typeof(ulong), typeof(ulong) });
        method.DefineParameter(1, ParameterAttributes.None, "n");
        method.DefineParameter(2, ParameterAttributes.None, "acc");

        var il = method.GetILGenerator();
        var L008 = il.DefineLabel();
        var IL_000b = il.DefineLabel();
        var IL_0017 = il.DefineLabel();

        var localN = il.DeclareLocal(typeof(ulong));
        var localAcc = il.DeclareLocal(typeof(ulong));
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Stloc, localN);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Stloc, localAcc);

        il.EmitWriteLine(localN);
        il.EmitWriteLine(localAcc);

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Brfalse_S, L008);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4_1);
        il.Emit(OpCodes.Conv_I8);
        il.Emit(OpCodes.Bne_Un_S, IL_000b);
        il.MarkLabel(L008);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Br_S, IL_0017);
        il.MarkLabel(IL_000b);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4_1);
        il.Emit(OpCodes.Conv_I8);
        il.Emit(OpCodes.Sub);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Mul);
        il.EmitCall(OpCodes.Call, method, Type.EmptyTypes);
        il.MarkLabel(IL_0017);
        il.Emit(OpCodes.Ret);
            

        //using (var il = new GroboIL(method))
        //{
        //    var L008 = il.DefineLabel("008");
        //    var IL_000b = il.DefineLabel("IL_000b");
        //    var IL_0017 = il.DefineLabel("IL_0017");

        //    il.Ldarg(0);
        //    il.Brfalse(L008);
        //    il.Ldarg(0);
        //    il.Ldc_I4(1);
        //    il.Conv<long>();
        //    il.Bne_Un(IL_000b);
        //    il.MarkLabel(L008);
        //    il.Ldarg(1);
        //    il.Br(IL_0017);
        //    il.MarkLabel(IL_000b);
        //    il.Ldarg(0);
        //    il.Ldc_I4(1);
        //    il.Conv<long>();
        //    il.Sub();
        //    il.Ldarg(1);
        //    il.Ldarg(0);
        //    il.Mul();
        //    il.Call(method, tailcall: false);
        //    il.MarkLabel(IL_0017);
        //    il.Ret();




        //    ///////////////////////////////
        //    //var exit = il.DefineLabel("exit");

        //    //il.Ldarg(0);
        //    //il.Ldc_I8(0);
        //    //il.Ceq();
        //    //il.Brtrue(exit);

        //    //il.Ldarg(0);
        //    //il.Ldc_I8(1);
        //    //il.Ceq();
        //    //il.Brtrue(exit);

        //    //il.Ldarg(0);
        //    //il.Ldc_I8(1);
        //    //il.Sub();
        //    //il.Ldarg(1);
        //    //il.Ldarg(0);
        //    //il.Mul();
        //    //il.Call(method, tailcall: false);
        //    //il.Ret();

        //    //il.MarkLabel(exit);
        //    //il.Ldarg(1);
        //    //il.Ret();

        //}

        var compiled = type.CreateType();
        var compiledMethod = compiled.GetMethod(method.Name, BindingFlags.Public | BindingFlags.Static);
        new Lokad.ILPack.AssemblyGenerator().GenerateAssembly(assemblyBuilder, assemblyName.Name + ".dll");

        return compiledMethod.CreateDelegate<Func<ulong, ulong, ulong>>();
    }

    public static ulong CompiledFactorial(ulong n, ulong acc) => n != 0UL && n != 1UL ? CompiledFactorial(n - 1UL, acc * n) : acc;
}