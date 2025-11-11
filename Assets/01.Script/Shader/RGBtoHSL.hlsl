void RGB_to_HSL_float(float3 In, out float3 Out)
{
    float r = In.x;
    float g = In.y;
    float b = In.z;

    float maxc = max(max(r, g), b);
    float minc = min(min(r, g), b);
    float delta = maxc - minc;

    float h = 0.0;
    float s = 0.0;
    float l = (maxc + minc) * 0.5;

    if (delta > 1e-6)
    {
        float denom = (l < 0.5) ? (maxc + minc) : (2.0 - maxc - minc);
        denom = max(denom, 1e-6);
        s = delta / denom;

        if (maxc == r)
        {
            h = (g - b) / delta + (g < b ? 6.0 : 0.0);
        }
        else if (maxc == g)
        {
            h = (b - r) / delta + 2.0;
        }
        else // maxc == b
        {
            h = (r - g) / delta + 4.0;
        }
        h /= 6.0;
    }

    Out = float3(h, s, l);
}
