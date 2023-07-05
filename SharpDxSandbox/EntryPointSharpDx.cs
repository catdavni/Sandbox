using SharpDxSandbox.Sandbox;

namespace SharpDxSandbox;

public static class EntryPointSharpDx
{
    public static async Task Run()
    {
        // 3D
        await new GraphicsSandbox().Start();

        // await Direct3DSandbox.StartTest();
        // await Direct3DSandbox.RotatingCube();
        //
        // // 2D
        // await Direct2DSandbox.FromDirect2D();
        // await Direct2DSandbox.FromDirect3D11();
    }
}