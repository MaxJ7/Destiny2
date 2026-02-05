
// ==============================================
// DESTINY 2 MOD - LINEAR UBER SHADER
// Consolidating all "Pseudo-Hitscan" trails
// ==============================================

// --- GLOBAL PARAMETERS ---
matrix uWorldViewProjection;
float uTime;
float uOpacity;

// Standard Textures
sampler uImage0 : register(s0); // Base Texture (Implicit)

// Extra Textures (Mapped as needed)
texture2D sampleTexture1;
texture2D sampleTexture2;
texture2D sampleTexture3;
texture2D sampleTexture4;

sampler2D texture1Sampler = sampler_state { texture = <sampleTexture1>; magfilter = LINEAR; minfilter = LINEAR; AddressU = wrap; AddressV = wrap; };
sampler2D texture2Sampler = sampler_state { texture = <sampleTexture2>; magfilter = LINEAR; minfilter = LINEAR; AddressU = wrap; AddressV = wrap; };
sampler2D texture3Sampler = sampler_state { texture = <sampleTexture3>; magfilter = LINEAR; minfilter = LINEAR; AddressU = wrap; AddressV = wrap; };
sampler2D texture4Sampler = sampler_state { texture = <sampleTexture4>; magfilter = LINEAR; minfilter = LINEAR; AddressU = wrap; AddressV = wrap; };

// Colors (Solar uses these as gradients)
float3 Color1;
float3 Color2;
float3 Color3;
float3 Color4;

// --- VERTEX SHADER (Standard) ---
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

// ==============================================
// PIXEL SHADER: SOLAR ("The Boiling Needle")
// Round 7 Logic - PS 2.0
// ==============================================
float4 PixelShaderFunction_Solar(VertexShaderOutput input) : COLOR0
{
    float2 coords = input.TextureCoordinates;
    float4 color = input.Color;

    // RESTORING ROUND 7 LOOK (The boiling needle)
    float timeA = uTime * -4.71; 
    float timeB = uTime * -2.33;
    
    // Sample Noise A (Flame)
    float4 noiseA = tex2D(texture2Sampler, coords + float2(timeA, 0)); 
    
    // Distorted Coords (Horizontal & Vertical Jitter)
    float2 dCoords = coords;
    dCoords.y += (noiseA.r - 0.5) * 0.45;
    dCoords.x += (noiseA.g - 0.5) * 0.2; 
    
    // Sample Noise B (Cellular)
    float4 noiseB = tex2D(texture3Sampler, dCoords * 0.6 + float2(timeB, uTime * 0.1)); 

    // Base Shape
    float4 baseShape = tex2D(texture1Sampler, dCoords); 
    
    // RAZOR THINNING (Round 7 balance)
    float yDist = abs(dCoords.y - 0.5) * 2.0;
    float y2 = yDist * yDist;
    float y4 = y2 * y2;
    float y12 = y4 * y4 * y4;
    float beamCurve = saturate(1.0 - y12);
    
    // Value
    float value = baseShape.r * beamCurve * (noiseA.r * noiseB.r * 1.6 + 0.3);

    // Gaps (Round 7 threshold)
    clip(value - (0.22 + noiseB.g * 0.55));
    value = saturate((value - 0.2) * 25.0); 

    // Coloring
    float3 finalColor = lerp(Color1, Color2, value);
    finalColor = lerp(finalColor, Color3, saturate((value - 0.7) * 3.3));
    
    return float4(finalColor * 4.0, value * uOpacity) * color;
}

// ==============================================
// PIXEL SHADER: ARC (Jagged Edge)
// ==============================================
float4 PixelShaderFunction_Arc(VertexShaderOutput input) : COLOR0
{
    float along = input.TextureCoordinates.x;
    float across = abs(input.TextureCoordinates.y - 0.5) * 2.0;

    // Hard Edge Falloff (Step Function aka "Jagged")
    float width = 0.8 + 0.2 * sin(along * 50.0 + uTime * 20.0); // Fast electric jaggedness
    if (across > width) discard; // Hard cut

    float alpha = 1.0;
    alpha *= smoothstep(0.0, 0.1, along);

    // Gradient: Blue -> Cyan
    float3 cBlue = float3(0.0, 0.2, 0.8);
    float3 cCyan = float3(0.0, 1.0, 1.0);
    float3 cWhite = float3(1.0, 1.0, 1.0);

    float3 color = lerp(cBlue, cCyan, across);
    if (across < 0.2) color = cWhite; // Core

    return float4(color, alpha) * input.Color;
}

// ==============================================
// PIXEL SHADER: VOID (Inverted Core)
// ==============================================
float4 PixelShaderFunction_Void(VertexShaderOutput input) : COLOR0
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

// ==============================================
// PIXEL SHADER: STASIS (Crystalline)
// ==============================================
float4 PixelShaderFunction_Stasis(VertexShaderOutput input) : COLOR0
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

// ==============================================
// PIXEL SHADER: STRAND (Weave)
// ==============================================
float4 PixelShaderFunction_Strand(VertexShaderOutput input) : COLOR0
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

// ==============================================
// PIXEL SHADER: KINETIC (Sharp Gray)
// ==============================================
float4 PixelShaderFunction_Kinetic(VertexShaderOutput input) : COLOR0
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

// ==============================================
// PIXEL SHADER: CORRUPTION (Aggressive Red)
// ==============================================
float4 PixelShaderFunction_Corruption(VertexShaderOutput input) : COLOR0
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

    return float4(color, alpha) * input.Color;
}

// ==============================================
// PIXEL SHADER: EXPLOSIVE SHADOW (Nebula)
// Mapped uImage1 -> sampleTexture2
// ==============================================
float4 PixelShaderFunction_ExplosiveShadow(VertexShaderOutput input) : COLOR0
{
    float2 coords = input.TextureCoordinates;
    float2 noiseCoords = coords * 1.5; 
    
    // Animate noise: Flow backwards + Drift Up/Down
    noiseCoords.x -= uTime * 1.2; 
    noiseCoords.y += sin(uTime + coords.x * 5.0) * 0.1;
    
    // Use texture2Sampler instead of uImage1Sampler
    float noise = tex2D(texture2Sampler, noiseCoords).r;
    
    // Coordinate remapping for "Beam" center
    float distanceFromCenter = abs(coords.y - 0.5) * 2.0;
    
    // Erode the beam width with noise
    float edgeNoise = noise * 0.6; 
    float effectiveWidth = distanceFromCenter + edgeNoise * 0.5;
    
    // Hard cutoff
    if (effectiveWidth > 0.9) discard;

    // WHITE CORE (Inner 20%)
    float coreMask = 1.0 - smoothstep(0.0, 0.3, distanceFromCenter);
    float3 cCore = float3(1.0, 1.0, 1.2);
    
    // DARK CYAN / BLACK AURA (Outer 80%)
    float3 cDarkCyan = float3(0.0, 0.2, 0.2); 
    float3 cBlack = float3(0.0, 0.0, 0.05);
    
    float3 cAura = lerp(cBlack, cDarkCyan, noise);
    
    // Combine
    float3 finalColor = lerp(cAura, cCore, coreMask);
    
    float alpha = 1.0;
    
    return float4(finalColor, alpha) * input.Color;
}

// ==============================================
// TECHNIQUES
// ==============================================

technique Solar
{
    pass AutoloadPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction_Solar();
    }
}

technique Arc
{
    pass AutoloadPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction_Arc();
    }
}

technique Void
{
    pass AutoloadPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction_Void();
    }
}

technique Stasis
{
    pass AutoloadPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction_Stasis();
    }
}

technique Strand
{
    pass AutoloadPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction_Strand();
    }
}

technique Kinetic
{
    pass AutoloadPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction_Kinetic();
    }
}

technique Corruption
{
    pass AutoloadPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction_Corruption();
    }
}

technique ExplosiveShadow
{
    pass AutoloadPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction_ExplosiveShadow();
    }
}
