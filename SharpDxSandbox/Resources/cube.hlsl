cbuffer TransformMatrix : register(b0)
{
    float4x4 WorldViewProjection;
};

float4 VShader(float3 position : POSITION) : SV_POSITION
{
    return mul(WorldViewProjection, float4(position, 1.0f));
}

cbuffer ColorMatrix : register(b0)
{
    float4 Colors[6];
}

float4 PShader(uint primitiveIndex : SV_PrimitiveID) : SV_TARGET
{
    return Colors[primitiveIndex/2];
    //return float4(1.0f,1.0f,1.0f,1.0f);
}