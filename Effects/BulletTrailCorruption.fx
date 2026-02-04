// Corruption: SIVA / Glitch Aesthetic
// Dark Red -> Black -> Crimson
// Aggressive Edges

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

    // Sharp Edge with slight jitter (simulated by steep power)
    float alpha = 1.0 - pow(across, 6.0); 
    alpha *= smoothstep(0.0, 0.1, along);

    // Corruption Color: Dark Red / Black / Crimson
    float3 cDark = float3(0.1, 0.0, 0.0);   // Almost black red
    float3 cMid = float3(0.6, 0.05, 0.05);  // Deep red
    float3 cLight = float3(1.0, 0.2, 0.2);  // Bright SIVA red
    
    // Gradient logic
    float3 color = lerp(cLight, cMid, across);
    color = lerp(color, cDark, pow(across, 3.0));

    // Boost opacity for the core
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
