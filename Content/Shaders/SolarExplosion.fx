// Destiny 2-style Solar explosion - cinematic, outside-of-Terraria feel
// Flash phase, white-hot core, orange-amber falloff, shockwave rings, ember noise, HDR
#if OPENGL
	#define SV_POSITION POSITION
	#define VS_MODEL vs_3_0
	#define PS_MODEL ps_3_0
#else
	#define VS_MODEL vs_4_0_level_9_1
	#define PS_MODEL ps_4_0_level_9_1
#endif

matrix MatrixTransform;
texture Texture;
sampler2D TextureSampler : register(s0) = sampler_state { Texture = (Texture); };
float Progress;
float Intensity;
float3 ColorTint;
float uTime;
float FlashPhase; // 1 = full flash (first frame), 0 = normal

struct VertexIn
{
	float4 Position : POSITION0;
	float4 Color : COLOR0;
	float2 TexCoord : TEXCOORD0;
};

struct VertexOut
{
	float4 Position : SV_POSITION;
	float4 Color : COLOR0;
	float2 TexCoord : TEXCOORD0;
};

VertexOut VS(VertexIn v)
{
	VertexOut o;
	o.Position = mul(v.Position, MatrixTransform);
	o.Color = v.Color;
	o.TexCoord = v.TexCoord;
	return o;
}

float hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
float noise(float2 p)
{
	float2 i = floor(p); float2 f = frac(p);
	f = f * f * (3.0 - 2.0 * f);
	return lerp(lerp(hash(i), hash(i + float2(1,0)), f.x),
		lerp(hash(i + float2(0,1)), hash(i + float2(1,1)), f.x), f.y);
}

float4 PS(VertexOut p) : SV_TARGET
{
	float2 uv = p.TexCoord - 0.5;
	float dist = length(uv) * 2.0;

	float prog = 1.0 - (1.0 - Progress) * (1.0 - Progress);
	float opacityCurve = 1.0;
	if (Progress < 0.08) opacityCurve = saturate(Progress / 0.08) * 2.0;
	else if (Progress > 0.65) opacityCurve = (1.0 - Progress) / 0.35;

	float pulse = 1.0 + 0.25 * sin(uTime * 15.0 + Progress * 25.0);

	// FLASH PHASE: First moments = near-pure white burst (Destiny 2 "pop")
	float flashMix = FlashPhase;
	float3 flashWhite = float3(1.0, 1.0, 1.0);

	// Core -> orange -> amber
	float coreFalloff = exp(-dist * 6.0) * (1.0 - prog) * pulse;
	float midFalloff = (1.0 - smoothstep(0.3, 0.5, dist)) * (1.0 - prog * 0.75);
	float rimFalloff = (1.0 - smoothstep(0.45, 0.7, dist)) * (1.0 - prog * 0.5);

	float3 whiteHot = float3(1.0, 1.0, 1.0);
	float3 orange = float3(1.0, 0.5, 0.08);
	float3 amber = float3(0.5, 0.15, 0.02);

	float3 col = whiteHot * coreFalloff + orange * midFalloff + amber * rimFalloff;
	col = lerp(col, flashWhite, flashMix * (1.0 - dist * 0.5));
	col *= ColorTint;

	// Shockwave rings - more visible, thicker
	float r1 = abs(dist - (0.12 + prog * 0.55));
	float r2 = abs(dist - (0.32 + prog * 0.5));
	float r3 = abs(dist - (0.52 + prog * 0.45));
	float r4 = abs(dist - (0.72 + prog * 0.35));
	float ring1 = 1.0 - smoothstep(0.0, 0.08, r1);
	float ring2 = 1.0 - smoothstep(0.0, 0.07, r2);
	float ring3 = 1.0 - smoothstep(0.0, 0.06, r3);
	float ring4 = 1.0 - smoothstep(0.0, 0.05, r4);

	float rings = (ring1 + ring2 * 0.95 + ring3 * 0.75 + ring4 * 0.5) * (1.0 - prog) * (1.0 + Intensity * 0.6);
	col += float3(1.0, 0.8, 0.4) * rings * ColorTint;

	// Ember turbulence
	float n = noise(uv * 15.0 + uTime * 3.0);
	float ember = saturate((n - 0.4) * 2.5 + (1.0 - dist) * 2.5) * (1.0 - prog) * (0.4 + Intensity * 0.3);
	col += float3(1.0, 0.6, 0.15) * ember * ColorTint;

	// Outer halo
	float halo = exp(-dist * 1.0) * (1.0 - prog) * (0.5 + Intensity * 0.4);
	col += amber * halo;

	// HDR - push brightness hard for bloom-like feel
	col *= 2.0 + Intensity * 1.5;

	// Alpha
	float alpha = saturate(coreFalloff * 2.0 + midFalloff + rimFalloff * 0.6 + rings * 1.3 + ember + halo * 0.6);
	alpha *= opacityCurve * (0.98 + Intensity * 0.15) * p.Color.a;
	alpha = lerp(alpha, saturate(alpha * (1.0 + flashMix * 2.0)), flashMix);

	return float4(col, alpha);
}

technique Technique1
{
	pass Pass1
	{
		VertexShader = compile VS_MODEL VS();
		PixelShader = compile PS_MODEL PS();
	}
}
