using SharpDX.Mathematics.Interop;
using SharpDxSandbox.Graphics.Drawables;

namespace SharpDxSandbox.Resources.Models;

public static class Cube
{
    public static readonly string TransformationMatrixKey = "TransformationMatrix_D90C55C4-E22A-40C0-B43E-E8E44613A701";

    public static readonly (string Key, VertexWithTexCoord[] Data) Vertices =
        ("CubeVertices_B0C7231A-6B29-4576-A6FE-A1D4439316AA", new[]
        {
            new VertexWithTexCoord(new RawVector3(-1f, -1f, 1f), new RawVector2(0, 1)), // front bottom left
            new VertexWithTexCoord(new RawVector3(-1f, 1f, 1f), new RawVector2(0, 0)), // front top left
            new VertexWithTexCoord(new RawVector3(1f, 1f, 1f), new RawVector2(1, 0)), // front top right
            new VertexWithTexCoord(new RawVector3(1f, -1f, 1f), new RawVector2(1, 1)), // front bottom right

            new VertexWithTexCoord(new RawVector3(-1f, -1f, -1f), new RawVector2(1, 1)), // back bottom left
            new VertexWithTexCoord(new RawVector3(-1f, 1f, -1f), new RawVector2(1, 0)), // back top left
            new VertexWithTexCoord(new RawVector3(1f, 1f, -1f), new RawVector2(0, 0)), // back top right
            new VertexWithTexCoord(new RawVector3(1f, -1f, -1f), new RawVector2(0, 1)) // back bottom right
        });

    public static readonly (string Key, int[] Data) TriangleIndices =
        ("CubeTriangleIndices_9EF152B5-227E-4FB3-827E-4F2EE568252C", new[]
        {
            7, 4, 5, 7, 5, 6, // front
            4, 0, 5, 0, 1, 5, // left
            7, 6, 3, 6, 2, 3, // right
            6, 5, 1, 6, 1, 2, // top
            0, 4, 7, 3, 0, 7, // bottom
            0, 2, 1, 0, 3, 2 // back
        });

    public static readonly (string Key, RawVector4[] Data) SideColors =
        ("CubeSideColors_62FE1F0C-3C53-4E9B-B25E-1FCA5C28F2C7", new[]
        {
            new RawVector4(1, 0, 0, 1), // front
            new RawVector4(0, 1, 0, 1), // left
            new RawVector4(0, 0, 1, 1), // right
            new RawVector4(1, 1, 0, 1), // top
            new RawVector4(1, 0, 1, 1), // bottom
            new RawVector4(0, 1, 1, 1) // back
        });
}