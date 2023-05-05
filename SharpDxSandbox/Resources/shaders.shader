﻿struct VOut
{
    //??? why float4 (seems like related to homogenius coordinate system)
    float4 position : SV_POSITION;
    float3 color : COLOR;
};

cbuffer ConstantBuffer : register(b0)
{
  row_major float4x4 WorldViewProjection;
};

VOut VShader(float3 position : POSITION, float3 color : COLOR)
{
    VOut output;

    output.position = mul(float4(position, 1.0f), WorldViewProjection);
    output.color = color;

    return output;
}


float4 PShader(float4 position : SV_POSITION, float3 color : COLOR) : SV_TARGET
{
    //return float4(1.0f,1.0f,1.0f,1.0f);
    return float4(color, 1.0f);
}