// Stasis: Golden Era Safe Gradient
// Bright Cyan -> White (Crystal)
// Hard Crystal Edges

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

    // Hard Edge (Crystal)
    if (across > 0.8) discard;
    
    // Faceted look (simple steps)
    float facet = floor(across * 3.0) / 3.0;
    float alpha = 1.0;
    alpha *= smoothstep(0.0, 0.1, along);

    // Gradient: Bright Cyan -> White
    float3 cCyan = float3(0.0, 0.8, 1.0);
    float3 cWhite = float3(1.0, 1.0, 1.0);
    
    float3 color = lerp(cWhite, cCyan, facet);

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
