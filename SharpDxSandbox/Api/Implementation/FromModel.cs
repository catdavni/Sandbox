using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using SharpDxSandbox.Api.Interface;
using SharpDxSandbox.Models;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

namespace SharpDxSandbox.Api.Implementation;

internal sealed class FromModel : CubeBase
{
    private readonly int[] _indices;
    private readonly Buffer _indexBuffer;

    public FromModel(Device device, IResourceFactory resourceFactory, RawVector3[] vertices, int[] indices, string key)
        : base(device, resourceFactory, ($"{key}Vertices", vertices), Cube.SideColors)
    {
        _indices = indices;
        _indexBuffer = resourceFactory.EnsureCrated($"{key}Indices",
            () =>
            {
                using var indexDataStream = DataStream.Create(indices, true, false);
                return new Buffer(
                    device,
                    indexDataStream,
                    Marshal.SizeOf<int>() * indices.Length,
                    ResourceUsage.Default,
                    BindFlags.IndexBuffer,
                    CpuAccessFlags.None,
                    ResourceOptionFlags.None,
                    Marshal.SizeOf<int>());
            });
    }

    public override DrawPipelineMetadata Draw(DrawPipelineMetadata previous, Device device)
    {
        var currentMetadata = base.Draw(previous, device);

        if (previous.IndexBufferHash != _indexBuffer.GetHashCode())
        {
            device.ImmediateContext.InputAssembler.SetIndexBuffer(_indexBuffer, Format.R32_UInt, 0);
            currentMetadata = currentMetadata with { IndexBufferHash = _indexBuffer.GetHashCode() };
        }

        device.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

        device.ImmediateContext.DrawIndexed(_indices.Length, 0, 0);

        return currentMetadata;
    }
}