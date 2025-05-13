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