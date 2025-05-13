float River(float2 RiverUV, float Time) {
    float2 uv = RiverUV;
    uv.y -= Time;
    uv.y = frac(uv.y);
    return uv.y;
}