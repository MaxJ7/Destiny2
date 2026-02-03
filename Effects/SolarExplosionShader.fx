matrix uWorldViewProjection;
sampler uImage0 : register(s0); // SolarExplosionNoise
sampler uImage1 : register(s1); // SolarExplosionAccent
sampler uImage2 : register(s2); // SolarStreaks

float uTime;
float uIrregularity;
float uProgress;
float4 uAccentColor;

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
    float2 uvOrig = input.TextureCoordinates;
    float t = uTime;
    
    // Domain Warping using uImage1
    float2 warpUV = uvOrig + float2(t * 0.1, -t * 0.4);
    float warp = tex2D(uImage1, warpUV).r;
    float2 polar = float2(uvOrig.x + warp * 0.15, uvOrig.y);
    
    // Sample noises with distorted coordinates
    float fireNoise = tex2D(uImage0, polar + float2(0, -t * 0.8)).r;
    float accentNoise = tex2D(uImage1, polar * 1.5 - float2(0, t * 0.5)).r;
    float streakNoise = tex2D(uImage2, polar * float2(2.0, 0.4) - float2(0, t * 1.5)).r;
    
    // Shape distortion
    float dist = polar.y + (accentNoise - 0.5) * uIrregularity * 1.5;
    dist += (streakNoise - 0.5) * uIrregularity * 0.8;
    
    // Combine noise
    float combined = saturate(fireNoise + streakNoise * 0.5) * saturate(1.1 - dist);
    
    // Stable math (combined^2)
    float c2 = combined * combined;
    float alpha = saturate(c2 * (1.1 - uProgress));
    
    // Final color with heat glow
    float3 color = uAccentColor.rgb * combined;
    color += float3(1.0, 0.8, 0.4) * c2 * 2.5; // Core heat
    
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
