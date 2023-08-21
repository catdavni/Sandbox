using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDxSandbox.Infrastructure;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;

namespace SharpDxSandbox.Graphics;

internal static class ResourceFactoryExtensions
{
    private static readonly ShaderFlags ShaderFlags = ShaderFlags.Debug 
                                                      | ShaderFlags.DebugNameForSource 
                                                      | ShaderFlags.EnableStrictness
                                                      | ShaderFlags.WarningsAreErrors;
    private static readonly string ShadersPath = Path.Combine("Resources", "Shaders");

    public static CompiledVertexShader EnsureVertexShader(this IResourceFactory factory, Device device, string shaderFileName, string entryPoint) =>
        factory.EnsureCrated(shaderFileName + entryPoint,
            () =>
            {
                var path = Path.Combine(ShadersPath, shaderFileName);
                using var vertexShaderBytes = ShaderBytecode.CompileFromFile(
                    path, 
                    entryPoint, 
                    "vs_5_0", 
                    ShaderFlags);
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
                using var pixelShaderBytes = ShaderBytecode.CompileFromFile(
                    path, 
                    entryPoint,
                    "ps_5_0",
                    ShaderFlags);
                return new PixelShader(device, pixelShaderBytes.Bytecode);
            });

    public static Buffer EnsureBuffer<T>(
        this IResourceFactory factory,
        Device device,
        string key,
        T[] data,
        BindFlags bindingFlags,
        ResourceUsage resourceUsage = ResourceUsage.Default,
        CpuAccessFlags cpuAccessFlags = CpuAccessFlags.None,
        [CallerArgumentExpression(nameof(data))]
        string dataExpression = null) where T : struct =>
        factory.EnsureCrated(key + "_" + dataExpression,
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

    public static Func<Buffer> EnsureUpdateBuffer<T>(this IResourceFactory factory, Device device, string key, Func<T> dataFactory) where T : struct =>
        () =>
        {
            var data = dataFactory();
            var transformMatrixBuffer = factory.EnsureBuffer(
                device,
                key,
                new[] { data },
                BindFlags.ConstantBuffer,
                ResourceUsage.Dynamic,
                CpuAccessFlags.Write);

            device.ImmediateContext.MapSubresource(transformMatrixBuffer, MapMode.WriteDiscard, MapFlags.None, out var dataStream);
            using (dataStream)
            {
                dataStream.Write(data);
                device.ImmediateContext.UnmapSubresource(transformMatrixBuffer, 0);
            }
            return transformMatrixBuffer;
        };

    public static ShaderResourceView EnsureTextureAsPixelShaderResourceView(this IResourceFactory factory, Device device, string imageFileName)
    {
        return factory.EnsureCrated($"{imageFileName}_ShaderResourceView",
            () =>
            {
                using var img = ImageLoader.Load(imageFileName);

                var stride = img.Size.Width * 4;
                using var imgData = new DataStream(img.Size.Width * img.Size.Height * 4, true, true);
                img.CopyPixels(stride, imgData);

                var textureDesc = new Texture2DDescription
                {
                    Width = img.Size.Width,
                    Height = img.Size.Height,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = Format.B8G8R8A8_UNorm, // play with it
                    SampleDescription = new SampleDescription(1, 0),
                    Usage = ResourceUsage.Default,
                    BindFlags = BindFlags.ShaderResource,
                    CpuAccessFlags = CpuAccessFlags.None,
                    OptionFlags = ResourceOptionFlags.None
                };
                using var texture = new Texture2D(device, textureDesc, new DataRectangle(imgData.DataPointer, stride));

                var resourceViewDesc = new ShaderResourceViewDescription
                {
                    Format = textureDesc.Format,
                    Dimension = ShaderResourceViewDimension.Texture2D,
                    //Buffer = default,
                    //Texture1D = default,
                    //Texture1DArray = default,
                    Texture2D = new ShaderResourceViewDescription.Texture2DResource
                    {
                        MipLevels = 1,
                        MostDetailedMip = 0
                    },
                    //Texture2DArray = default,
                    //Texture2DMS = default,
                    //Texture2DMSArray = default,
                    // Texture3D = default,
                    //TextureCube = default,
                    //TextureCubeArray = default,
                    //BufferEx = default
                };
                return new ShaderResourceView(device, texture, resourceViewDesc);
            });
    }
}