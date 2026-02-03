matrix uWorldViewProjection;
sampler uImage0 : register(s0); // SolarExpansionRing (Concentric)
sampler uImage1 : register(s1); // SolarStreaks (Linear Wisps)

float uTime;
float uOpacity;
float4 uColor;

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
    // x = angle, y = radial
    float2 polar = input.TextureCoordinates;
    float t = uTime;
    
    // Safety check for radius
    float radius = max(0.001, polar.y);
    
    // Domain Warping: Use ring noise to distort the streak sampling
    float warp = tex2D(uImage0, float2(polar.x, radius * 0.5 + t * 0.2)).r;
    
    // Radial Streaking: scrolling streaks along the 'y' (radius) axis
    float2 streakUV = float2(polar.x + warp * 0.1, radius * 0.4 - t * 2.5);
    float streaks = tex2D(uImage1, streakUV).r;
    
    // Base ring noise for internal texture
    float ringNoise = tex2D(uImage0, float2(polar.x + streaks * 0.05, radius * 0.8 - t * 0.6)).r;
    
    // Smooth the edges of the primitive width
    float edgeMask = smoothstep(0.0, 0.4, radius) * smoothstep(1.0, 0.6, radius);
    
    // Combine for a 'torn' shockwave
    float combined = saturate(ringNoise * 0.6 + streaks * 0.8) * edgeMask * uOpacity;
    
    // High-heat core glow
    float c2 = combined * combined;
    float c4 = c2 * c2;
    float3 finalColor = uColor.rgb * combined;
    finalColor += float3(1.0, 0.9, 0.7) * c4 * 2.0;
    
    return float4(finalColor, combined) * input.Color;
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
