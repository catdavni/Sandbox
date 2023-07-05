using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

namespace SharpDxSandbox.Graphics;

internal static class ResourceFactoryExtensions
{
    private static readonly string ShadersPath = Path.Combine("Resources", "Shaders");

    public static CompiledVertexShader EnsureVertexShader(this IResourceFactory factory, Device device, string shaderFileName, string entryPoint) =>
        factory.EnsureCrated(shaderFileName + entryPoint,
            () =>
            {
                var path = Path.Combine(ShadersPath, shaderFileName);
                using var vertexShaderBytes = ShaderBytecode.CompileFromFile(path, entryPoint, "vs_4_0");
                var shader = new VertexShader(device, vertexShaderBytes.Bytecode);
                return new CompiledVertexShader(shader, vertexShaderBytes.Bytecode);
            });

    public static InputLayout EnsureInputLayout(this IResourceFactory factory, Device device, byte[] vertexShaderBytes, params InputElement[] elements)
    {
        var keys = elements.Select(e => $"[{e.SemanticName}_{e.SemanticIndex}_{e.Format}_{e.AlignedByteOffset}_{e.Slot}]");
        var compoundKey = string.Join('|', keys);
        return factory.EnsureCrated(compoundKey, () => new InputLayout(device, vertexShaderBytes, elements));
    }

    public static PixelShader EnsurePixelShader(this IResourceFactory factory, Device device, string shaderFileName, string entryPoint) =>
        factory.EnsureCrated(shaderFileName + entryPoint,
            () =>
            {
                var path = Path.Combine(ShadersPath, shaderFileName);
                using var pixelShaderBytes = ShaderBytecode.CompileFromFile(path, entryPoint, "ps_4_0");
                return new PixelShader(device, pixelShaderBytes.Bytecode);
            });

    public static Buffer EnsureBuffer<T>(
        this IResourceFactory factory,
        Device device,
        string key,
        T[] data,
        BindFlags bindingFlags,
        ResourceUsage resourceUsage = ResourceUsage.Default,
        CpuAccessFlags cpuAccessFlags = CpuAccessFlags.None) where T : struct =>
        factory.EnsureCrated(key,
            () =>
            {
                using var verticesByteCode = DataStream.Create(data, true, false);
                return new Buffer(
                    device,
                    verticesByteCode,
                    Marshal.SizeOf<T>() * data.Length,
                    resourceUsage,
                    bindingFlags,
                    cpuAccessFlags,
                    ResourceOptionFlags.None,
                    Marshal.SizeOf<T>());
            });

    public static Func<Buffer> EnsureUpdateTransformMatrix(this IResourceFactory factory, Device device, string key, Func<Matrix> transform) =>
        () =>
        {
            var transformMatrix = transform();
            var transformMatrixBuffer = factory.EnsureBuffer(
                device,
                key,
                transformMatrix.ToArray(),
                BindFlags.ConstantBuffer,
                ResourceUsage.Dynamic,
                CpuAccessFlags.Write);

            device.ImmediateContext.MapSubresource(transformMatrixBuffer, MapMode.WriteDiscard, MapFlags.None, out var dataStream);
            using (dataStream)
            {
                dataStream.Write(transformMatrix);
                device.ImmediateContext.UnmapSubresource(transformMatrixBuffer, 0);
            }
            return transformMatrixBuffer;
        };
}