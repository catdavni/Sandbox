cbuffer TransformMatrix : register(b0)
{
float4x4 ModelToWorld;
}

cbuffer WorldToCameraProjection : register(b1)
{
float4x4 WorldToCameraProjection;
}

struct VOut
{
    float4 Position : SV_POSITION;
    float4 vertex_position : VertexPosition;
    float3 normal : Normal;
    float2 TexCoord : TEXCOORD0;
};

VOut VShader(float3 position : POSITION, float3 normal : Normal, float2 texCoord : TexCoord)
{
    const float4 vertexWorldPosition = mul(ModelToWorld, float4(position, 1.0f));

    const float3 normalWorldTranslated = mul((float3x3)ModelToWorld, normal);
    const float3 normalizedTranslatedNormal = normalize(normalWorldTranslated);

    VOut result;
    result.Position = mul(WorldToCameraProjection, vertexWorldPosition);
    result.TexCoord = texCoord;
    result.normal = normalizedTranslatedNormal;
    result.vertex_position = vertexWorldPosition;
    return result;
}

cbuffer LightPosition: register(b0)
{
float4 LightPosition;
}

cbuffer CameraPosition: register(b1)
{
float4 CameraPosition;
}

cbuffer Material:register(b2)
{
float4 MaterialColor;

float Ambient;
float DiffuseIntensity;
float SpecularIntensity;
float SpecularPower;

float AttenuationConstant;
float AttenuationLinear;
float AttenuationQuadric;
float NOT_USED_ALIGN_PLACEHOLDER;
}

Texture2D tex;
SamplerState samplerState :register(s0);

float4 PShader(VOut pin) : SV_TARGET
{
    const float3 normal = normalize(pin.normal);
    const float3 vertex = pin.vertex_position.xyz;

    const float3 vertexToLightSource = normalize(LightPosition.xyz - vertex);
    const float3 vertexToCamera = normalize(CameraPosition.xyz - vertex);
    const float vertexToLightDistance = length(LightPosition.xyz - vertex);

    const float attenuation = 1 /
    (
        AttenuationConstant +
        AttenuationLinear * vertexToLightDistance +
        AttenuationQuadric * vertexToLightDistance * vertexToLightDistance
    );

    if (attenuation > 1)
    {
        return float4(1, 1, 0, 0);
    }

    // diffuse    
    const float diffuseCoefficient = max(0, dot(vertexToLightSource, normal));
    const float diffuse = diffuseCoefficient * DiffuseIntensity * attenuation;

    // if (diffuse > 1)
    // {
    //     return float4(1, 0, 0, 0);
    // }

    // // specular
    const float3 reflectedLight = normalize(reflect(-vertexToLightSource, normal));

    const float specularCoefficient = max(0, dot(vertexToCamera, reflectedLight));
    const float poweredSpecularCoefficient = pow(specularCoefficient, SpecularPower);

    if (poweredSpecularCoefficient > 1)
    {
        return float4(0, 1, 0, 0);
    }

    const float specular = poweredSpecularCoefficient * SpecularIntensity;

    // if (specular > 1)
    // {
    //     return float4(0, 0, 1, 0);
    // }

    // float lightAmount = saturate(Ambient + diffuse + specular) * attenuation;
    // return MaterialColor * lightAmount;

    return saturate(MaterialColor * (Ambient + diffuse + specular)) * attenuation;
}
