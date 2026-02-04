// Void: Golden Era Safe Gradient (No Procedural Noise)
// Inverted Intensity: Dark Core -> Bright Rim

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

    // Edge Falloff (Ragged/Sinewave based on uTime - Safe)
    float edgeMod = 0.8 + 0.2 * sin(along * 20.0 - uTime * 10.0);
    float alpha = 1.0 - smoothstep(edgeMod - 0.2, edgeMod, across);
    alpha *= smoothstep(0.0, 0.1, along);

    // Gradient: Dark Core -> Bright Rim
    float3 cDark = float3(0.05, 0.0, 0.1); 
    float3 cBright = float3(0.7, 0.0, 1.0);

    // Inverse Lerp: Bright at edges, Dark at center
    float3 color = lerp(cBright, cDark, 1.0 - across);

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
