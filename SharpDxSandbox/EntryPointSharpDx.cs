using SharpDxSandbox.Sandbox;

namespace SharpDxSandbox;

public static class EntryPointSharpDx
{
    public static async Task RunDirect3D()
    {
        //await Direct3DSandbox.StartTest();
        await Direct3DSandbox.RotatingCube();
    }

    public static async Task RunDirect2D()
    {
        await Direct2DSandbox.FromDirect2D();
        await Direct2DSandbox.FromDirect3D11();
    }
}