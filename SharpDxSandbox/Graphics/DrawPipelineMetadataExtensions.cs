using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

namespace SharpDxSandbox.Graphics;

public static class DrawPipelineMetadataExtensions
{
    public static DrawPipelineMetadata BreakIfChanged(this DrawPipelineMetadata current, DrawPipelineMetadata previous)
    {
        Dictionary<string, Func<DrawPipelineMetadata, int>> namedHashGetter = new()
        {
            { nameof(DrawPipelineMetadata.VertexBufferHash), p => p.VertexBufferHash },
            { nameof(DrawPipelineMetadata.VertexShaderHash), p => p.VertexShaderHash },
            { nameof(DrawPipelineMetadata.InputLayoutHash), p => p.InputLayoutHash },
            { nameof(DrawPipelineMetadata.IndexBufferHash), p => p.IndexBufferHash },
            { nameof(DrawPipelineMetadata.PixelShaderHash), p => p.PixelShaderHash },
            { nameof(DrawPipelineMetadata.PixelShaderConstantBufferHash), p => p.PixelShaderConstantBufferHash }
        };

        if (current != previous && previous != default)
        {
            var message = namedHashGetter
                .Where(keyValuePair => keyValuePair.Value(current) != keyValuePair.Value(previous))
                .Aggregate(new StringBuilder("Was changed:\n"), (acc, cur) => acc.AppendLine(cur.Key));
           
            Trace.WriteLine(message);
            Debugger.Break();
        }
        return current;
    }

    public static DrawPipelineMetadata EnsureVertexBufferBinding<T>(this DrawPipelineMetadata origin, Device device, Buffer vertexBuffer) where T : struct
    {
        if (origin.VertexBufferHash != vertexBuffer.GetHashCode())
        {
            device.ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertexBuffer, Marshal.SizeOf<T>(), 0));
            origin = origin with { VertexBufferHash = vertexBuffer.GetHashCode() };
        }
        return origin;
    }

    public static DrawPipelineMetadata EnsureVertexShaderBinding(this DrawPipelineMetadata origin, Device device, VertexShader shader)
    {
        if (origin.VertexShaderHash != shader.GetHashCode())
        {
            device.ImmediateContext.VertexShader.Set(shader);
            origin = origin with { VertexShaderHash = shader.GetHashCode() };
        }
        return origin;
    }

    public static DrawPipelineMetadata EnsureInputLayoutBinding(this DrawPipelineMetadata origin, Device device, InputLayout layout)
    {
        if (origin.InputLayoutHash != layout.GetHashCode())
        {
            device.ImmediateContext.InputAssembler.InputLayout = layout;
            origin = origin with { InputLayoutHash = layout.GetHashCode() };
        }
        return origin;
    }

    public static DrawPipelineMetadata EnsurePixelShader(this DrawPipelineMetadata origin, Device device, PixelShader shader)
    {
        if (origin.PixelShaderHash != shader.GetHashCode())
        {
            device.ImmediateContext.PixelShader.Set(shader);
            origin = origin with { PixelShaderHash = shader.GetHashCode() };
        }
        return origin;
    }

    public static DrawPipelineMetadata EnsureSamplerState(this DrawPipelineMetadata origin, Device device, SamplerState sampler)
    {
        if (origin.SamplerHash != sampler.GetHashCode())
        {
            device.ImmediateContext.PixelShader.SetSampler(0, sampler);
            origin = origin with { SamplerHash = sampler.GetHashCode() };
        }
        return origin;
    }
    
    public static DrawPipelineMetadata EnsurePixelShaderTextureView(this DrawPipelineMetadata origin, Device device, ShaderResourceView textureView)
    {
        if (origin.PixelShaderTextureView != textureView.GetHashCode())
        {
            device.ImmediateContext.PixelShader.SetShaderResource(0, textureView);
            origin = origin with { PixelShaderTextureView = textureView.GetHashCode() };
        }
        return origin;
    }

    public static DrawPipelineMetadata EnsureIndexBufferBinding(this DrawPipelineMetadata origin, Device device, Buffer indexBuffer)
    {
        if (origin.IndexBufferHash != indexBuffer.GetHashCode())
        {
            device.ImmediateContext.InputAssembler.SetIndexBuffer(indexBuffer, Format.R32_UInt, 0);
            origin = origin with { IndexBufferHash = indexBuffer.GetHashCode() };
        }
        return origin;
    }
}