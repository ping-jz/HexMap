float River(float2 riverUV, float time, UnityTexture2D noiseTex) {
    float2 uv = riverUV;
	uv.x = uv.x * 0.0625 + _Time.y * 0.005;
	uv.y -= _Time.y * 0.25;
	float4 noise = noiseTex.Sample(noiseTex.samplerstate, uv);

	float2 uv2 = riverUV;
	uv2.x = uv2.x * 0.0625 - _Time.y * 0.0052;
	uv2.y -= _Time.y * 0.23;
	float4 noise2 = noiseTex.Sample(noiseTex.samplerstate, uv2);

	return noise.r * noise2.w;
}

float Foam(float shore, float2 worldXZ, float Time, UnityTexture2D NoiseTexture) {
	shore = sqrt(shore);

	float2 noiseUV = worldXZ + Time * 0.25;
    float4 noise = NoiseTexture.Sample(NoiseTexture.samplerstate, noiseUV * 0.015);

    float distortion1 = noise.x * (1 - shore);
    float foam1 = sin((shore + distortion1) * 10 - Time);
    foam1 *= foam1;

    float distortion2 = noise.y * (1 - shore);
    float foam2 = sin((shore + distortion2) * 10 + Time + 2);
    foam2 *= foam2 * 0.7;

    return max(foam1, foam2) * shore;
}

float Waves(float2 worldXZ, float Time, UnityTexture2D NoiseTexture) {
	float2 uv1 = worldXZ;
	uv1.y += Time;
    float4 noise1 = NoiseTexture.Sample(NoiseTexture.samplerstate, uv1 * 0.025);

    float2 uv2 = worldXZ;
    uv2.x += Time;
    float4 noise2 = NoiseTexture.Sample(NoiseTexture.samplerstate, uv2 * 0.025);

    float blendWave = sin(
		(worldXZ.x + worldXZ.y) * 0.1 + 
		(noise1.y + noise2.z) + Time
	);
    blendWave *= blendWave;


    float waves = 
         lerp(noise1.z, noise1.w, blendWave) + 
         lerp(noise2.x, noise2.y, blendWave);
    return smoothstep(0.75, 2, waves);
}