sampler uImage0 : register(s0);

texture2D sampleTexture1;
texture2D sampleTexture2;
texture2D sampleTexture3;
texture2D sampleTexture4;

sampler2D texture1Sampler = sampler_state { texture = <sampleTexture1>; magfilter = LINEAR; minfilter = LINEAR; AddressU = wrap; AddressV = wrap; };
sampler2D texture2Sampler = sampler_state { texture = <sampleTexture2>; magfilter = LINEAR; minfilter = LINEAR; AddressU = wrap; AddressV = wrap; };
sampler2D texture3Sampler = sampler_state { texture = <sampleTexture3>; magfilter = LINEAR; minfilter = LINEAR; AddressU = wrap; AddressV = wrap; };
sampler2D texture4Sampler = sampler_state { texture = <sampleTexture4>; magfilter = LINEAR; minfilter = LINEAR; AddressU = wrap; AddressV = wrap; };

float3 Color1;
float3 Color2;
float3 Color3;
float3 Color4;

float uTime;
float uOpacity;

float4 PixelShaderFunction(float2 coords : TEXCOORD0, float4 color : COLOR0) : COLOR0
{
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

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
