// Solar: Heavy Layered Fire Tracer
// Uses multi-layered noise and domain warping for a chaotic, "plume-like" feel.
matrix uWorldViewProjection;
float uTime;
float uSeed;
float uLengthRatio;

// Samplers
sampler uImage1 : register(s1); // SolarFlameNoise (General turbulence)
sampler uImage2 : register(s2); // SolarStreaks (Directional warping/tearing)

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

    float t = uTime;
    float s = uSeed * 10.0;

    // --- PHASE 1: Simple Noise Layers ---
    float2 uv1 = float2(along * 2.0 - t * 1.2 + s, across * 0.8);
    float noise1 = tex2D(uImage1, uv1).r;
    
    float2 uv2 = float2(along * 4.0 - t * 2.5 + s, across * 1.5);
    float noise2 = tex2D(uImage1, uv2).r;
    
    float2 uv3 = float2(along * 1.0 - t * 0.5 + s, across * 0.4);
    float noise3 = tex2D(uImage1, uv3).r;

    // --- PHASE 2: Combination & Contrast ---
    float combined = noise1 * 0.5 + noise2 * 0.4 + noise3 * 0.3;
    combined = pow(abs(combined), 1.6) * 1.5; // High contrast
    
    // --- PHASE 3: Alpha & Masking ---
    float alphaThreshold = across + (1.0 - combined) * 0.8;
    float alpha = saturate(1.2 - alphaThreshold);
    
    // Fade at tips
    alpha *= saturate(along * 8.0); 
    alpha *= saturate((1.0 - along) * 4.0);
    
    // --- PHASE 4: Color ---
    float3 heatColor = float3(2.5, 1.5, 0.4); 
    float3 midColor = float3(2.5, 0.8, 0.0);  
    float3 edgeColor = float3(1.2, 0.1, 0.0); 
    
    float3 color = lerp(edgeColor, midColor, combined);
    color = lerp(color, heatColor, pow(abs(combined), 3.0)); 

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
