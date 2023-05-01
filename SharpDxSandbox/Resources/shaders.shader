﻿struct VOut
{
    //??? why float4 (seems like related to homogenius coordinate system)
    float4 position : SV_POSITION;
    float4 color : COLOR;
};

VOut VShader(float3 position : POSITION, float4 color : COLOR)
{
    VOut output;

    output.position = float4(position, 1.0f);
    output.color = color;

    return output;
}


float4 PShader(float4 position : SV_POSITION, float4 color : COLOR) : SV_TARGET
{
    //return float4(1.0f,1.0f,1.0f,1.0f);
    return color;
}