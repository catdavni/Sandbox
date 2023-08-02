using SharpDX;
using SharpDX.Mathematics.Interop;
using SharpDxSandbox.Graphics.Drawables;

namespace SharpDxSandbox.Resources.Models;

public static class Cube
{
    public static readonly string TransformationMatrixKey = "TransformationMatrix_D90C55C4-E22A-40C0-B43E-E8E44613A701";

    public static class Simple
    {
        public static readonly (string Key, RawVector3[] Data) Vertices =
            ("SimpleCubeVertices_5B9393E5-D5FC-4CEC-930B-4E5DF22CF67E", new[]
            {
                new RawVector3(-1f, -1f, -1f), // front bottom left
                new RawVector3(-1f, 1f, -1f), // front top left
                new RawVector3(1f, 1f, -1f), // front top right
                new RawVector3(1f, -1f, -1f), // front bottom right

                new RawVector3(-1f, -1f, 1f), // back bottom left
                new RawVector3(-1f, 1f, 1f), // back top left
                new RawVector3(1f, 1f, 1f), // back top right
                new RawVector3(1f, -1f, 1f), // back bottom right
            });

        public static readonly (string Key, int[] Data) TriangleIndices =
            ("SimpleCubeTriangleIndices_F5206BD8-AC62-4362-A0D7-DF9B7A8185BE", new[]
            {
                0, 1, 2, 3, 0, 2, // front
                0, 4, 5, 0, 5, 1, // left
                7, 3, 2, 7, 2, 6, // right
                2, 1, 5, 2, 5, 6, // top
                7, 4, 0, 7, 0, 3, // bottom
                4, 7, 6, 4, 6, 5 // back
            });
    }

    public static class Skinned
    {
        public static readonly (string Key, VertexWithTexCoord[] Data) Vertices =
            ($"{nameof(Skinned)}CubeVertices_B0C7231A-6B29-4576-A6FE-A1D4439316AA", new[]
            {
                new VertexWithTexCoord(new RawVector3(-1f, -1f, -1f), new RawVector2(0, 1)), // front bottom left
                new VertexWithTexCoord(new RawVector3(-1f, 1f, -1f), new RawVector2(0, 0)), // front top left
                new VertexWithTexCoord(new RawVector3(1f, 1f, -1f), new RawVector2(1, 0)), // front top right
                new VertexWithTexCoord(new RawVector3(1f, -1f, -1f), new RawVector2(1, 1)), // front bottom right

                new VertexWithTexCoord(new RawVector3(-1f, -1f, 1f), new RawVector2(1, 1)), // back bottom left
                new VertexWithTexCoord(new RawVector3(-1f, 1f, 1f), new RawVector2(1, 0)), // back top left
                new VertexWithTexCoord(new RawVector3(1f, 1f, 1f), new RawVector2(0, 0)), // back top right
                new VertexWithTexCoord(new RawVector3(1f, -1f, 1f), new RawVector2(0, 1)), // back bottom right

                new VertexWithTexCoord(new RawVector3(-1f, 1f, -1f), new RawVector2(0, 1)), // top bottom left
                new VertexWithTexCoord(new RawVector3(-1f, 1f, 1f), new RawVector2(0, 0)), // top top left
                new VertexWithTexCoord(new RawVector3(1f, 1f, 1f), new RawVector2(1, 0)), // top top right
                new VertexWithTexCoord(new RawVector3(1f, 1f, -1f), new RawVector2(1, 1)), // top bottom right

                new VertexWithTexCoord(new RawVector3(-1f, -1f, 1f), new RawVector2(0, 1)), // bottom bottom left
                new VertexWithTexCoord(new RawVector3(-1f, -1f, -1f), new RawVector2(0, 0)), // bottom top left
                new VertexWithTexCoord(new RawVector3(1f, -1f, -1f), new RawVector2(1, 0)), // bottom top right
                new VertexWithTexCoord(new RawVector3(1f, -1f, 1f), new RawVector2(1, 1)), // bottom bottom right
            });

        public static readonly (string Key, int[] Data) TriangleIndices =
            ("CubeTriangleIndices_9EF152B5-227E-4FB3-827E-4F2EE568252C", new[]
            {
                0, 1, 2, 3, 0, 2, // front
                0, 4, 5, 0, 5, 1, // left
                7, 3, 2, 7, 2, 6, // right
                4, 7, 6, 4, 6, 5, // back

                8, 9, 10, 8, 10, 11, // top
                12, 13, 14, 12, 14, 15 // bottom
            });
    }

    public static class Shaded
    {
        public static readonly (string Key, Vertex_Normal_TexCoord[] Data) Vertices =
            ($"{nameof(Shaded)}CubeVertices_DAC4FD15-A9D8-4216-8989-6DAC634F84AB", new[]
            {
                // 0
                new Vertex_Normal_TexCoord(new RawVector3(-1f, -1f, -1f), -Vector3.UnitZ, new RawVector2(0, 1)), // front bottom left
                new Vertex_Normal_TexCoord(new RawVector3(-1f, 1f, -1f), -Vector3.UnitZ, new RawVector2(0, 0)), // front top left
                new Vertex_Normal_TexCoord(new RawVector3(1f, 1f, -1f), -Vector3.UnitZ, new RawVector2(1, 0)), // front top right
                new Vertex_Normal_TexCoord(new RawVector3(1f, -1f, -1f), -Vector3.UnitZ, new RawVector2(1, 1)), // front bottom right
                // 4
                new Vertex_Normal_TexCoord(new RawVector3(-1f, -1f, 1f), Vector3.UnitZ, new RawVector2(1, 1)), // back bottom left
                new Vertex_Normal_TexCoord(new RawVector3(-1f, 1f, 1f), Vector3.UnitZ, new RawVector2(1, 0)), // back top left
                new Vertex_Normal_TexCoord(new RawVector3(1f, 1f, 1f), Vector3.UnitZ, new RawVector2(0, 0)), // back top right
                new Vertex_Normal_TexCoord(new RawVector3(1f, -1f, 1f), Vector3.UnitZ, new RawVector2(0, 1)), // back bottom right
                // 8
                new Vertex_Normal_TexCoord(new RawVector3(-1f, 1f, -1f), Vector3.UnitY, new RawVector2(0, 1)), // top bottom left
                new Vertex_Normal_TexCoord(new RawVector3(-1f, 1f, 1f), Vector3.UnitY, new RawVector2(0, 0)), // top top left
                new Vertex_Normal_TexCoord(new RawVector3(1f, 1f, 1f), Vector3.UnitY, new RawVector2(1, 0)), // top top right
                new Vertex_Normal_TexCoord(new RawVector3(1f, 1f, -1f), Vector3.UnitY, new RawVector2(1, 1)), // top bottom right
                // 12
                new Vertex_Normal_TexCoord(new RawVector3(-1f, -1f, 1f), -Vector3.UnitY, new RawVector2(0, 1)), // bottom bottom left
                new Vertex_Normal_TexCoord(new RawVector3(-1f, -1f, -1f), -Vector3.UnitY, new RawVector2(0, 0)), // bottom top left
                new Vertex_Normal_TexCoord(new RawVector3(1f, -1f, -1f), -Vector3.UnitY, new RawVector2(1, 0)), // bottom top right
                new Vertex_Normal_TexCoord(new RawVector3(1f, -1f, 1f), -Vector3.UnitY, new RawVector2(1, 1)), // bottom bottom right
                // 16
                new Vertex_Normal_TexCoord(new RawVector3(-1f, -1f, 1f), -Vector3.UnitX, new RawVector2(0, 1)), // left bottom left
                new Vertex_Normal_TexCoord(new RawVector3(-1f, 1f, 1f), -Vector3.UnitX, new RawVector2(0, 0)), // left top left
                new Vertex_Normal_TexCoord(new RawVector3(-1f, 1f, -1f), -Vector3.UnitX, new RawVector2(1, 0)), // left top right
                new Vertex_Normal_TexCoord(new RawVector3(-1f, -1f, -1f), -Vector3.UnitX, new RawVector2(1, 1)), // left bottom right
                // 20
                new Vertex_Normal_TexCoord(new RawVector3(1f, -1f, -1f), Vector3.UnitX, new RawVector2(0, 1)), // right bottom left
                new Vertex_Normal_TexCoord(new RawVector3(1f, 1f, -1f), Vector3.UnitX, new RawVector2(0, 0)), // right top left
                new Vertex_Normal_TexCoord(new RawVector3(1f, 1f, 1f), Vector3.UnitX, new RawVector2(1, 0)), // right top right
                new Vertex_Normal_TexCoord(new RawVector3(1f, -1f, 1f), Vector3.UnitX, new RawVector2(1, 1)), // right bottom right
            });

        public static readonly (string Key, int[] Data) TriangleIndices =
            ("CubeTriangleIndices_9EF152B5-227E-4FB3-827E-4F2EE568252C", new[]
            {
                0, 1, 2, 3, 0, 2, // front
                4, 7, 6, 4, 6, 5, // back

                8, 9, 10, 8, 10, 11, // top
                12, 13, 14, 12, 14, 15, // bottom

                16, 17, 18, 16, 18, 19, // left
                20, 21, 22, 20, 22, 23, // right
            });
    }

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