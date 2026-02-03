// Taken: Ghostly Black with White Core
matrix uWorldViewProjection;
float uTime;

// Textures
texture uImage0; // Base texture (White core gradient)
sampler uImage0Sampler = sampler_state
{
    Texture = <uImage0>;
    MinFilter = Linear;
    MagFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};

texture uImage1; // Noise texture (Taken Ink Swirls)
sampler uImage1Sampler = sampler_state
{
    Texture = <uImage1>;
    MinFilter = Linear;
    MagFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
};

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
    float2 coords = input.TextureCoordinates;
    float2 noiseCoords = coords;
    
    // Animate noise
    // Standardize to "Beam" (Static X, Drift Y)
    // noiseCoords.x -= uTime * 0.8; // Removed forward scroll
    noiseCoords.y += uTime * 0.2; // Slow vertical drift
    
    float noise = tex2D(uImage1Sampler, noiseCoords).r;
    
    // Core definition
    float distanceFromCenter = abs(coords.y - 0.5) * 2.0; // 0 at center, 1 at edges
    
    // The "Taken" Look:
    // Core: Pure White (High intensity at center)
    // Edge: Black Erosion (Noise eating away at the edges)
    
    // 1. Core Intensity (White)
    float coreIntensity = 1.0 - smoothstep(0.0, 0.4, distanceFromCenter); // Sharp white spine
    float3 coreColor = float3(1, 1, 1) * coreIntensity * 2.0; // Overbright

    // 2. Black Aura (The "Ghostly" part)
    // We want black to be VISIBLE.
    // If we return (0,0,0,1), it draws black.
    // If we return (1,1,1,0), it's transparent.
    
    float auraShape = 1.0 - smoothstep(0.4, 0.9, distanceFromCenter + noise * 0.4);
    
    // Combine:
    // If we are in the core, white.
    // If we are in the aura but not core, BLACK.
    // Use lerp to blend.
    
    float3 finalColor = lerp(float3(0,0,0), float3(1.2, 1.2, 1.5), coreIntensity);
    
    // Fade out at tail
    float opacity = input.Color.a * (1.0 - coords.x); 
    
    // Threshold alpha to create "torn" edges
    float alpha = auraShape * opacity;
    
    // Hard cutoff for erosion effect
    if (alpha < 0.1) discard;

    return float4(finalColor, alpha);
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
