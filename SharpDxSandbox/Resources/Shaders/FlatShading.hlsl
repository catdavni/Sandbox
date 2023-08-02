cbuffer TransformMatrix : register(b0)
{
float4x4 ModelToWorld;
}

cbuffer TransformMatrix : register(b1)
{
float4x4 WorldToCameraProjection;
}

cbuffer LightPosition: register(b2)
{
float4 LightPosition;
}

struct VOut
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TexCoord;
    float LightAmount : LightAmount;
};

VOut VShader(float3 position : POSITION, float3 normal : Normal, float2 texCoord : TexCoord)
{
    const float ambient = 0.2;

    float4 vertexWorldPosition = mul(ModelToWorld, float4(position, 1.0f));

    float4 lightDirection = LightPosition - vertexWorldPosition;
    float4 normalizedLightDirection = normalize(lightDirection);

    float3 normalWorldTranslated = mul((float3x3)ModelToWorld, normal);
    float3 normalizedTranslatedNormal = normalize(normalWorldTranslated);

    float lightNormalCodirection = dot(normalizedTranslatedNormal, normalizedLightDirection.xyz);
    float directLight = max(0, lightNormalCodirection);

    VOut result;
    result.Position = mul(WorldToCameraProjection, vertexWorldPosition);
    result.TexCoord = texCoord;
    result.LightAmount = saturate(ambient + directLight);
    return result;
}

Texture2D tex;
SamplerState samplerState :register(s0);

float4 PShader(float4 position : SV_POSITION, float2 texCoord: TexCoord, float lightAmount: LightAmount) : SV_TARGET
{
    // float4 texData = tex.Sample(samplerState, texCoord) * lightAmount;
    // return texData;
    return float4(1.0f, 1.0f, 1.0f, 1.0f) * lightAmount;
}
