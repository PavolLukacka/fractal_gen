#include <cstdint>
#include <cmath>
#include <omp.h>

extern "C" struct Params  // identical to CUDA struct
{
    double xCenter, yCenter, zoom;
    int    width, height, maxIter, palette;
};

extern "C" __declspec(dllexport)
int GenerateFractal(const Params * p, uint8_t * rgb)
{
    double scale = 1.5 / p->zoom;
    double xMin = p->xCenter - scale;
    double yMin = p->yCenter - scale;

#pragma omp parallel for schedule(dynamic,16)
    for (int py = 0; py < p->height; ++py)
    {
        for (int px = 0; px < p->width; ++px)
        {
            double cx = xMin + (double)px / p->width * 2.0 * scale;
            double cy = yMin + (double)py / p->height * 2.0 * scale;

            double x = 0, y = 0, xx = 0, yy = 0;
            int    i = 0;
            while (xx + yy <= 4.0 && i < p->maxIter)
            {
                y = 2 * x * y + cy;
                x = xx - yy + cx;
                xx = x * x; yy = y * y;
                ++i;
            }

            uint8_t r, g, b;
            if (i == p->maxIter) { r = g = b = 0; }
            else {
                double t = (double)i / p->maxIter;
                switch (p->palette) {
                case 0: r = g = b = (uint8_t)(t * 255); break;
                case 1: r = (uint8_t)(t * 255); g = (uint8_t)((1 - t) * 255); b = 128; break;
                default:r = (uint8_t)(t * 255); g = (uint8_t)(t * t * 255); b = 0; break;
                }
            }
            int idx = (py * p->width + px) * 3;
            rgb[idx] = r; rgb[idx + 1] = g; rgb[idx + 2] = b;
        }
    }
    return 0;
}
