// Explosive Shadow: White Core surrounded by Dark Cyan/Black irregular nebula
matrix uWorldViewProjection;
float uTime;

// Textures
texture uImage0; // Base texture (Gradient)
sampler uImage0Sampler = sampler_state
{
    Texture = <uImage0>;
    MinFilter = Linear;
    MagFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};

// Use Noise Texture for irregularity
texture uImage1; 
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
    float2 noiseCoords = coords * 1.5; // Scale noise density
    
    // Animate noise: Flow backwards + Drift Up/Down
    noiseCoords.x -= uTime * 1.2; 
    noiseCoords.y += sin(uTime + coords.x * 5.0) * 0.1;
    
    float noise = tex2D(uImage1Sampler, noiseCoords).r;
    
    // Coordinate remapping for "Beam" center (0.5 is center, 0/1 are edges)
    float distanceFromCenter = abs(coords.y - 0.5) * 2.0;
    
    // --- SHAPE DEFINITION ---
    // Create an irregular edge by modulating distance with noise
    // "Erode" the beam width with noise
    float edgeNoise = noise * 0.6; 
    float effectiveWidth = distanceFromCenter + edgeNoise * 0.5;
    
    // Hard cutoff for the jagged shape
    if (effectiveWidth > 0.9) discard;

    // --- COLOR ZONES ---
    
    // 1. WHITE CORE (Inner 20%)
    // Sharp falloff
    float coreMask = 1.0 - smoothstep(0.0, 0.3, distanceFromCenter);
    float3 cCore = float3(1.0, 1.0, 1.2); // Bright White (slightly blue tint)
    
    // 2. DARK CYAN / BLACK AURA (Outer 80%)
    float3 cDarkCyan = float3(0.0, 0.2, 0.2); // Dark Cyan
    float3 cBlack = float3(0.0, 0.0, 0.05);   // Deep Black (with tiniest blue hint)
    
    // Blend Black -> Cyan based on noise intensity vs distance
    float3 cAura = lerp(cBlack, cDarkCyan, noise);
    
    // Combine Core and Aura
    // If coreMask is high, show White. Else show Aura.
    float3 finalColor = lerp(cAura, cCore, coreMask);
    
    // --- OPACITY ---
    // Fade out at the tail of the trail (x=1 is head, x=0 is tail usually, or vice versa depending on generation)
    // Assuming standard: 0=Start, 1=End. BulletDraw usually puts head at 1?
    // Let's assume standard fade.
    float alpha = 1.0;
    
    return float4(finalColor, alpha) * input.Color;
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
