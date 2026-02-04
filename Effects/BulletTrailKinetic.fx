// Kinetic: Golden Era Safe Gradient (No Procedural Noise)
// Neutral Gray -> White
// Razor Sharp Edges

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

    // Sharp Edge
    float alpha = 1.0 - pow(across, 4.0); 
    alpha *= smoothstep(0.0, 0.1, along);

    // Kinetic Color: Cool Gray
    float3 cDark = float3(0.4, 0.4, 0.45);
    float3 cLight = float3(0.8, 0.8, 0.9);
    
    float3 color = lerp(cLight, cDark, across);

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
