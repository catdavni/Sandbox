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
    centroid float4 normal : TEXCOORD1;
    float2 TexCoord : TEXCOORD0;
};

VOut VShader(float3 position : POSITION, float3 normal : Normal, float2 texCoord : TexCoord)
{
    float4 vertexWorldPosition = mul(ModelToWorld, float4(position, 1.0f));

    float3 normalWorldTranslated = mul((float3x3)ModelToWorld, normal);
    float3 normalizedTranslatedNormal = normalize(normalWorldTranslated);

    VOut result;
    result.Position = mul(WorldToCameraProjection, vertexWorldPosition);
    result.TexCoord = texCoord;
    result.normal = float4(normalizedTranslatedNormal, 1);
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
    float3 normalLocal = normalize(pin.normal.xyz);
    float4 vertexPosition = pin.vertex_position;

    const float3 vToL = (LightPosition - vertexPosition).xyz;
    const float distToL = length(vToL);
    const float3 dirToL = normalize(vToL);

    // attenuation
    const float att = 1.0f /
        (AttenuationConstant + AttenuationLinear * distToL + AttenuationQuadric * (distToL * distToL));

    // diffuse intensity
    const float diffuse = DiffuseIntensity * att * max(0.0f, dot(dirToL, normalLocal));

    // reflected light vector
    float3 w = normalLocal * dot(vToL, normalLocal);
    float3 r = w * 2.0f - vToL;

    // calculate specular intensity based on angle between viewing vector and reflection vector, narrow with power function
    const float specular = att * DiffuseIntensity * SpecularIntensity * pow(max(0.0f, dot(normalize(-r), normalize(vertexPosition.xyz))), SpecularPower);
    // final color
    return saturate((diffuse + Ambient + specular) * MaterialColor);

    // // diffuse
    // float3 lightToObject = (LightPosition - vertexPosition).xyz;
    // float3 lightToObjectNormalized = normalize(lightToObject);
    // float lightNormalCodirection = dot(normalLocal, lightToObjectNormalized);
    // float lightDistance = length(lightToObject);
    // float diffuse = max(0, lightNormalCodirection) * MaterialDiffusePower.x / (lightDistance * lightDistance);
    //
    // // specular
    // float3 lightToCamera = normalize((CameraPosition - vertexPosition).xyz);
    // float3 reflectedLight = reflect(-lightToObject, normalLocal);
    // float reflectedLightWithCameraCoDirection = max(0, dot(lightToCamera, reflectedLight));
    // float specularIntensity = pow(reflectedLightWithCameraCoDirection, MaterialSpecular_Hardness_Power.x);
    // float specular = specularIntensity * MaterialSpecular_Hardness_Power.y / (lightDistance * lightDistance);
    //
    // const float ambient = 0.2;
    // float lightAmount = ambient + diffuse + specular;
    //
    // return MaterialColor * lightAmount;
}
