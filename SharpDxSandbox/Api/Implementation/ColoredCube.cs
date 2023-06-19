using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDxSandbox.Api.Interface;
using SharpDxSandbox.Api.PrimitiveData;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

namespace SharpDxSandbox.Api.Implementation;

internal sealed class ColoredCube : CubeBase
{
    private readonly Buffer _indexBuffer;
    public ColoredCube(Device device, IResourceFactory resourceFactory):base(device, resourceFactory)
    {
        _indexBuffer = resourceFactory.EnsureCrated(Cube.TriangleIndexBufferKey,
            () =>
            {
                using var indexDataStream = DataStream.Create(Cube.TriangleIndices, true, false);
                return new Buffer(
                    device,
                    indexDataStream,
                    Marshal.SizeOf<int>() * Cube.TriangleIndices.Length,
                    ResourceUsage.Default,
                    BindFlags.IndexBuffer,
                    CpuAccessFlags.None,
                    ResourceOptionFlags.None,
                    Marshal.SizeOf<int>());
            });
    }

    public void Dispose()
    {
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

        device.ImmediateContext.DrawIndexed(Cube.TriangleIndices.Length, 0, 0);

        return currentMetadata;
    }
}