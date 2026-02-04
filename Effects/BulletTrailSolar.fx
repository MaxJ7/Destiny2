// Solar: Textured Fire (Phase 19 Revert)
// Uses scrolling noise for turbulent flame effect.

matrix uWorldViewProjection;
float uTime;
float uLengthRatio;

texture2D NoiseTexture;
sampler2D NoiseSampler = sampler_state
{
    Texture = <NoiseTexture>;
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
    float along = input.TextureCoordinates.x;
    float across = abs(input.TextureCoordinates.y - 0.5) * 2.0;

    // Scroll noise
    float2 noiseCoords = float2(along * 0.5 - uTime * 2.0, across * 0.5);
    float noise = tex2D(NoiseSampler, noiseCoords).r;

    // Distort edge transparency with noise
    float edgeAlpha = 1.0 - smoothstep(0.5 + noise * 0.2, 1.0, across);
    edgeAlpha *= smoothstep(0.0, 0.1, along);

    // Color Gradient (Amber -> Gold -> White)
    float3 cAmber = float3(1.0, 0.5, 0.0);
    float3 cGold = float3(1.0, 0.8, 0.0);
    float3 cWhite = float3(1.0, 1.0, 1.0);

    // Core intensity (noise modulated)
    float core = smoothstep(0.0, 0.3, 1.0 - across - noise * 0.2);
    
    float3 color = lerp(cAmber, cGold, across);
    color = lerp(color, cWhite, core);

    // Pulse
    color *= 1.0 + 0.5 * noise;

    return float4(color, edgeAlpha) * input.Color;
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
