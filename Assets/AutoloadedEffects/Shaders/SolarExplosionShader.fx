sampler fireNoiseTexture : register(s1);
sampler edgeAccentTexture : register(s2);

float time;
float explosionShapeIrregularity;
float lifetimeRatio;
float4 accentColor;

float4 PixelShaderFunction(float2 coords : TEXCOORD0, float4 sampleColor : COLOR0) : COLOR0
{
    float distanceFromCenter = distance(coords, 0.5);
    float2 polar = float2(atan2(coords.y - 0.5, coords.x - 0.5) / 6.283 + 0.5, distanceFromCenter);
    float n1 = tex2D(edgeAccentTexture, polar + time * float2(0.15, -0.9)).r;
    float n2 = tex2D(edgeAccentTexture, polar * 2.3 + time * float2(-0.12, -0.6)).r;
    distanceFromCenter += (n1 - 0.5) * explosionShapeIrregularity * 1.4;
    distanceFromCenter += (n2 - 0.5) * explosionShapeIrregularity * 0.8;
    float innerGlow = lerp(0.04, 0.5, tex2D(edgeAccentTexture, polar + float2(time * 0.2, -time * 1.2)).r);
    float4 color = sampleColor + innerGlow / (distanceFromCenter + 0.02);
    float fireNoise = tex2D(fireNoiseTexture, polar * float2(2.5, 1.2) + time * float2(-0.15, -1.1)).r;
    fireNoise += tex2D(fireNoiseTexture, polar * 1.3 + time * float2(0.1, -0.4)).r * 0.5;
    color += accentColor * fireNoise * lifetimeRatio * 1.4;
    float lr2 = lifetimeRatio * lifetimeRatio;
    float fadeFromWithin = smoothstep(-0.2, 0.05, distanceFromCenter - lr2 * lr2 * 0.55 * lifetimeRatio);
    float edgeFade = smoothstep(0.52, 0.35, distanceFromCenter);
    return color * fadeFromWithin * edgeFade;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
