// Strand: Golden Era Safe Gradient
// Green -> Neon Green (Weave)
// Organic ribbon

matrix uWorldViewProjection;
float uTime;

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

VertexShaderOutput VertexShaderFunction(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput)0;
    output.Position = mul(input.Position, uWorldViewProjection);
    output.Color = input.Color;
    output.TextureCoordinates = input.TextureCoordinates;
    return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float along = input.TextureCoordinates.x;
    float across = abs(input.TextureCoordinates.y - 0.5) * 2.0;

    // Soft Edge
    float alpha = 1.0 - smoothstep(0.6, 1.0, across);
    alpha *= smoothstep(0.0, 0.1, along);

    // Weave pattern (Sine wave, safe)
    float weave = sin(along * 30.0 + uTime * 5.0) * 0.2;
    float3 cGreen = float3(0.0, 0.6, 0.2);
    float3 cNeon = float3(0.2, 1.0, 0.4);
    
    float3 color = lerp(cGreen, cNeon, across + weave);

    return float4(color, alpha) * input.Color;
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
