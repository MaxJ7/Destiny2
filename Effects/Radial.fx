sampler uImage0 : register(s0);

texture2D causticTexture; // The Shape (e.g., Ring, Bolt, Skull)
texture2D distortTexture; // The Noise (Scrolls to warp UVs)
texture2D gradientTexture; // 1D Gradient Map

sampler2D causticSampler = sampler_state { texture = <causticTexture>; magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = clamp; AddressV = clamp; };
sampler2D distortSampler = sampler_state { texture = <distortTexture>; magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = wrap; AddressV = wrap; };
sampler2D gradientSampler = sampler_state { texture = <gradientTexture>; magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = clamp; AddressV = clamp; };

float uTime;
float flowSpeed;
float distortStrength;
float colorIntensity;
float uOpacity;

float4 PixelShaderFunction(float2 coords : TEXCOORD0, float4 color : COLOR0) : COLOR0
{
    // 1. Distortion
    // Sample the noise texture to get a displacement vector.
    float4 noise = tex2D(distortSampler, coords + float2(uTime * flowSpeed, uTime * flowSpeed * 0.5));
    
    // Warp the lookup coordinates for the main shape.
    float2 warpedCoords = coords + (noise.xy - 0.5) * distortStrength;

    // 2. Sample Shape
    float4 shape = tex2D(causticSampler, warpedCoords);
    
    // If we warped off the texture, clip it.
    if (warpedCoords.x < 0 || warpedCoords.x > 1 || warpedCoords.y < 0 || warpedCoords.y > 1) 
        shape = 0;

    // 3. Gradient Map
    // Use the shape's brightness (R channel) to pick a color from the gradient.
    float gradientLookUp = saturate(shape.r * color.a); 
    float4 gradientColor = tex2D(gradientSampler, float2(gradientLookUp, 0.5));

    // 4. Output
    // Apply vertex color tint and global opacity.
    return gradientColor * colorIntensity * uOpacity * shape.a;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
