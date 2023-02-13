Texture2D Input : register(t0);

RWStructuredBuffer<uint> Output : register(u0);

SamplerState LinearSampler{
    Filter = MIN_MAG_MIP_LINEAR;
    AddressU = Clamp;
    AddressV = Clamp;
};

[numthreads(1, 1, 1)] // TODO: poor design
void main(uint3 id : SV_DispatchThreadID) {
    
    uint2 tempRes = 1;
    if (any(id.xy >= tempRes)) {
        return;
    }
    
    // TODO: this is just a temporary MVP    
    uint2 threads = uint2(2, 6); // TODO: dynamic!
    
    // sample middle of texture
    float4 texel = Input.SampleLevel(LinearSampler, float2(0.5f, 0.5f), 0);
    uint a = uint(texel.w * 255.0f);
    uint r = uint(texel.x * 255.0f);
    uint g = uint(texel.y * 255.0f);
    uint b = uint(texel.z * 255.0f);
    
    // crush down into single int
    uint entry = (b << 24) | (g << 16) | (r << 8) | a;
    
    for (uint y = 0; y < threads.y; y++) {
        for (uint x = 0; x < threads.x; x++) {
            Output[y * threads.x + x] = entry;
        }
    }
    
    /*
    uint width;
    uint height;
    uint numLevels;
    Input.GetDimensions(0, width, height, numLevels);
    
    // how much of the texture to sweep in %
    float2 sweepPercentage = float2(1.0f / threads.x, 1.0f / threads.y);
    float2 offset = sweepPercentage * uint2(id.x, id.y);
    float2 stepSize = float2(1.0f / width, 1.0f / height);
    uint2 steps = uint2((sweepPercentage.x * width) / stepSize.x, sweepPercentage.y * height / stepSize.y);
    
    float4 sum;
    
    // TODO: this is horribly inefficient
    for (uint x = 0; x < steps.x; x++) {
        for (uint y = 0; y < steps.y; y++) {
            sum += Input.SampleLevel(LinearSampler, float2(offset.x + x, offset.y + y), 0);
        }
    }
   
    float4 avg = sum / (steps.x * steps.y);
    
    // split individual ints ranging between 0 and 255
    uint a = uint(avg.w * 255.0f);
    uint r = uint(avg.x * 255.0f);
    uint g = uint(avg.y * 255.0f);
    uint b = uint(avg.z * 255.0f);
    
    // crush down into single int
    uint entry = (b << 24) | (g << 16) | (r << 8) | a;
    
    Output[id.y * id.x + id.x] = entry;
    */
}

