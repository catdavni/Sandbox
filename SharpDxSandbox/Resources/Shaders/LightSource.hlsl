cbuffer TransformMatrix : register(b0)
{
float4x4 WorldViewProjection;
};

float4 VShader(float3 position : POSITION) : SV_POSITION
{
    return mul(WorldViewProjection, float4(position, 1.0f));
}

float4 PShader() : SV_TARGET
{
    return float4(1.0f,1.0f,1.0f,1.0f);
}
