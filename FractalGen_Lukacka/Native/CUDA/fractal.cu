#include <cstdint>
#include <cuda_runtime.h>

struct Params {
    double xCenter;
    double yCenter;
    double zoom;
    int    width;
    int    height;
    int    maxIter;
    int    palette;       // 0=gray,1=rainbow,2=fire
};

__device__ int mandelbrot(double cx, double cy, int maxIter)
{
    double x = 0.0, y = 0.0, xx = 0.0, yy = 0.0;
    int i = 0;
    while (xx + yy <= 4.0 && i < maxIter)
    {
        y = 2 * x * y + cy;
        x = xx - yy + cx;
        xx = x * x;
        yy = y * y;
        ++i;
    }
    return i;
}

__global__ void renderKernel(const Params p, uint8_t* rgb)
{
    int px = blockIdx.x * blockDim.x + threadIdx.x;
    int py = blockIdx.y * blockDim.y + threadIdx.y;
    if (px >= p.width || py >= p.height) return;

    double scale = 1.5 / p.zoom;   // view fits in [-1.5,1.5] at zoom 1
    double xMin = p.xCenter - scale;
    double yMin = p.yCenter - scale;

    double cx = xMin + (double)px / p.width * 2.0 * scale;
    double cy = yMin + (double)py / p.height * 2.0 * scale;

    int iter = mandelbrot(cx, cy, p.maxIter);

    // --- very small, fixed palettes -------------
    uint8_t r, g, b;
    if (iter == p.maxIter) { r = g = b = 0; } // in the set → black
    else
    {
        double t = (double)iter / p.maxIter;   // 0…1
        switch (p.palette)
        {
        case 0: r = g = b = (uint8_t)(t * 255); break;           // grayscale
        case 1: r = (uint8_t)(t * 255); g = (uint8_t)((1 - t) * 255); b = 128; break; // rainbow-ish
        default: r = (uint8_t)(t * 255); g = (uint8_t)(t * t * 255); b = 0;  // fire
        }
    }

    int idx = (py * p.width + px) * 3;
    rgb[idx] = r;
    rgb[idx + 1] = g;
    rgb[idx + 2] = b;
}

// ----------- exported C function ---------------
extern "C" __declspec(dllexport)
int GenerateFractal(const Params * pHost, uint8_t * rgbHost)
{
    Params  p = *pHost;                  // copy to device later
    size_t  imgBytes = p.width * p.height * 3;

    uint8_t* dRgb = nullptr;
    Params* dPar = nullptr;

    cudaError_t err = cudaSuccess;
    if ((err = cudaMalloc(&dRgb, imgBytes)) != cudaSuccess) return err;
    if ((err = cudaMalloc(&dPar, sizeof(Params))) != cudaSuccess) return err;

    cudaMemcpy(dPar, &p, sizeof(Params), cudaMemcpyHostToDevice);

    dim3 block(16, 16);
    dim3 grid((p.width + 15) / 16, (p.height + 15) / 16);
    renderKernel << <grid, block >> > (*dPar, dRgb);
    if ((err = cudaGetLastError()) != cudaSuccess) return err;

    cudaMemcpy(rgbHost, dRgb, imgBytes, cudaMemcpyDeviceToHost);

    cudaFree(dRgb); cudaFree(dPar);
    return 0;           // success
}
