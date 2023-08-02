struct VOut
{
    //??? why float4 (seems like related to homogenius coordinate system)
    float4 position : SV_POSITION;
    float3 color : COLOR;
};

cbuffer TransformMatrix: register(b0)
{
  float4x4 ModelToWorld;
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