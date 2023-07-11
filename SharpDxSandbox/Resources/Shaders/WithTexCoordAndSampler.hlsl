cbuffer TransformMatrix : register(b0)
{
float4x4 WorldViewProjection;
}

struct VOut
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TexCoord;
};

VOut VShader(float3 position : POSITION, float2 texCoord: TexCoord)
{
    VOut result;
    result.Position =mul(WorldViewProjection, float4(position, 1.0f));
    result.TexCoord = texCoord;
    return result;
}

Texture2D tex;
SamplerState samplerState :register(s0);

float4 PShader(float4 position : SV_POSITION, float2 texCoord: TexCoord) : SV_TARGET
{
    return tex.Sample(samplerState, texCoord);
    //return float4(1.0f, 0.0f, 0.0f, 1.0f);
}
