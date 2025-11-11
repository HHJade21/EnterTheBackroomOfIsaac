float HueToRGB(float p, float q, float t)
{
    if (t < 0.0)
    {
        t += 1.0;
    }
    if (t > 1.0)
    {
        t -= 1.0;
    }
    if (t < 1.0 / 6.0)
    {
        return p + (q - p) * 6.0 * t;
    }
    if (t < 1.0 / 2.0)
    {
        return q;
    }
    if (t < 2.0 / 3.0)
    {
        return p + (q - p) * (2.0 / 3.0 - t) * 6.0;
    }
    return p;
}

void HSL_to_RGB_float(float3 In, out float3 Out)
{
    float h = In.x;
    float s = In.y;
    float l = In.z;

    float r;
    float g;
    float b;

    if (s <= 1e-6)
    {
        r = g = b = l;
    }
    else
    {
        float q = (l < 0.5) ? (l * (1.0 + s)) : (l + s - l * s);
        float p = 2.0 * l - q;

        float hk = frac(h);

        r = HueToRGB(p, q, hk + 1.0 / 3.0);
        g = HueToRGB(p, q, hk);
        b = HueToRGB(p, q, hk - 1.0 / 3.0);
    }

    Out = float3(r, g, b);
}

