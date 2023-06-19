using SharpDX.Mathematics.Interop;

namespace SharpDxSandbox.Api.PrimitiveData;

public static class Cube
{
    public const string VertexBufferKey = "CubeVertexBuffer";
    public const string VertexShaderKey = "CubeVertexShader";
    public const string InputLayout = "CubeInputLayout";
    public const string TriangleIndexBufferKey = "CubeTriangleIndexBuffer";
    public const string PixelShaderKey = "CubePixelShader";
    public const string PixelShaderConstantBufferKey = "CubeColorsBuffer";
    
    public static readonly RawVector3[] Vertices =
    {
        new(-0.8f, -0.8f, 0.8f), // front bottom left
        new(-0.8f, 0.8f, 0.8f), // front top left
        new(0.8f, 0.8f, 0.8f), // front top right
        new(0.8f, -0.8f, 0.8f), // front bottom right

        new(-0.8f, -0.8f, -0.8f), // back bottom left
        new(-0.8f, 0.8f, -0.8f), // back top left
        new(0.8f, 0.8f, -0.8f), // back top right
        new(0.8f, -0.8f, -0.8f), // back bottom right
    };

    public static readonly int[] TriangleIndices =
    {
        7, 4, 5, 7, 5, 6, // front
        4, 0, 5, 0, 1, 5, // left
        7, 6, 3, 6, 2, 3, // right
        6, 5, 1, 6, 1, 2, // top
        0, 4, 7, 3, 0, 7, // bottom
        0, 2, 1, 0, 3, 2 // back
    };

    public static readonly RawVector4[] SideColors =
    {
        new(1, 0, 0, 1), // front
        new(0, 1, 0, 1), // left
        new(0, 0, 1, 1), // right
        new(1, 1, 0, 1), // top
        new(1, 0, 1, 1), // bottom
        new(0, 1, 1, 1) // back
    };
}