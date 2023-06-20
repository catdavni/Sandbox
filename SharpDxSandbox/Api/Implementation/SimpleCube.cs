using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDxSandbox.Api.Interface;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

namespace SharpDxSandbox.Api.Implementation;

public sealed class SimpleCube : CubeBase
{
    private const string LineIndicesKey = $"{nameof(SimpleCube)}LineIndexBuffer";
    private static readonly int[] LineIndices = 
    {
        3, 0,
        0, 1,
        1, 2,
        2, 3,
        1, 5,
        5, 6,
        6, 2,
        3, 7,
        7, 6,
        7, 4,
        4, 0,
        5, 4
    };
    
    private readonly Buffer _indexBuffer;

    public SimpleCube(Device device, IResourceFactory resourceFactory):base(device, resourceFactory)
    {
        _indexBuffer = resourceFactory.EnsureCrated(LineIndicesKey,
            () =>
            {
                using var indexDataStream = DataStream.Create(LineIndices, true, false);
                return new Buffer(
                    device,
                    indexDataStream,
                    Marshal.SizeOf<int>() * LineIndices.Length,
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

    public DrawPipelineMetadata Draw(DrawPipelineMetadata previous, Device device)
    {
        var currentMetadata = base.Draw(previous, device);
        
        if (previous.IndexBufferHash != _indexBuffer.GetHashCode())
        {
            device.ImmediateContext.InputAssembler.SetIndexBuffer(_indexBuffer, Format.R32_UInt, 0);
            currentMetadata = currentMetadata with { IndexBufferHash = _indexBuffer.GetHashCode() };
        }
        
        device.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.LineList;

        device.ImmediateContext.DrawIndexed(LineIndices.Length, 0, 0);

        return currentMetadata;
    }
}