using System.Runtime.InteropServices;
using SharpDX.Mathematics.Interop;

namespace SharpDxSandbox.Graphics.Drawables;

[StructLayout(LayoutKind.Sequential)]
public readonly record struct VertexWithTexCoord(RawVector3 Vertex, RawVector2 TexCoord);