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

cbuffer CameraPosition: register(b3)
{
float4 CameraPosition;
}

struct VOut
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TexCoord;
    float DiffuseLight : DiffuseLight;
    float SpecularLight : SpecularLight;
};

VOut VShader(float3 position : POSITION, float3 normal : Normal, float2 texCoord : TexCoord)
{
    float4 vertexWorldPosition = mul(ModelToWorld, float4(position, 1.0f));

    // diffuse
    float4 lightDirection = LightPosition - vertexWorldPosition;
    float3 normalizedLightDirection = normalize(lightDirection).xyz;

    float3 normalWorldTranslated = mul((float3x3)ModelToWorld, normal);
    float3 normalizedTranslatedNormal = normalize(normalWorldTranslated);

    float lightNormalCodirection = dot(normalizedTranslatedNormal, normalizedLightDirection);
    float diffuseLight = max(0, lightNormalCodirection);

    // specular
    float3 lightToCameraDirection = normalize((CameraPosition - vertexWorldPosition).xyz);
    float3 reflectedLight = reflect(-normalizedLightDirection, normalizedTranslatedNormal);
    float specular = max(0, dot(lightToCameraDirection, reflectedLight));

    VOut result;
    result.Position = mul(WorldToCameraProjection, vertexWorldPosition);
    result.TexCoord = texCoord;
    result.DiffuseLight = diffuseLight;
    result.SpecularLight = specular;
    return result;
}

Texture2D tex;
SamplerState samplerState :register(s0);

float4 PShader(float4 position : SV_POSITION, float2 texCoord: TexCoord, float diffuse: DiffuseLight,
               float specular: SpecularLight) : SV_TARGET
{
    // float4 texData = tex.Sample(samplerState, texCoord) * lightAmount;
    // return texData;

    const float diffuseIntensity = 0.2;
    const float specularIntensity = 0.5;
    const float ambient = 0.1;
    float lightAmount = ambient + (diffuse * diffuseIntensity) + (specular * specularIntensity);

    return float4(1.0f, 0.0f, 0.0f, 1.0f) * lightAmount;
}
