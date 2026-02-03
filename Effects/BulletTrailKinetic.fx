// Kinetic: White/Grey Vapor Trail
// Uses scrolling noise to create a smoke-like trail effect.
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

// Simple hash for noise
float hash(float2 p) {
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

// 2D Noise
float noise(float2 p) {
    float2 i = floor(p);
    float2 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);
    float res = lerp(lerp(hash(i), hash(i + float2(1.0, 0.0)), f.x),
                     lerp(hash(i + float2(0.0, 1.0)), hash(i + float2(1.0, 1.0)), f.x), f.y);
    return res;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float along = input.TextureCoordinates.x;
    float across = abs(input.TextureCoordinates.y - 0.5) * 2.0;
    
    // Kinetic Logic: Static noise (no motion)
    float2 uv = float2(along * 6.0, across * 2.0);
    float n = noise(uv);
    
    // Vapor look: Soft edges modulated by noise
    float alpha = 1.0 - smoothstep(0.0, 1.0, across + n * 0.4); 
    alpha *= smoothstep(0.0, 0.2, along); // Fade in at start
    
    // Color: White core to light grey vapor (Neutral Luma)
    float3 color = lerp(float3(1.0, 1.0, 1.0), float3(0.5, 0.5, 0.5), across);
    color += float3(0.2, 0.2, 0.2) * (1.0 - along) * 0.5;

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

