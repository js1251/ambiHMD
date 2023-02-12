Texture2D Input : register(t0);

RWStructuredBuffer<uint> Output : register(u0);

SamplerState LinearSampler{
    Filter = MIN_MAG_MIP_LINEAR;
    AddressU = Clamp;
    AddressV = Clamp;
};

[numthreads(32, 32, 1)]
void main(uint3 id : SV_DispatchThreadID) {
    uint2 res = 1;
    // Input.GetDimensions();
    if (any(id.xy >= res)) {
        return;
    }

    // sample texture
    float4 texel = Input.SampleLevel(LinearSampler, float2(0.5f, 0.5f), 0);
    
    //uint r = asuint(texel.x * 255.0f);
    //uint g = asuint(texel.y * 255.0f);
    //uint b = asuint(texel.z * 255.0f);
    //uint a = asuint(texel.w * 255.0f);
    
    uint a = uint(texel.w * 255.0f);
    uint r = uint(texel.x * 255.0f);
    uint g = uint(texel.y * 255.0f);
    uint b = uint(texel.z * 255.0f);
    
    uint entry = (b << 24) | (g << 16) | (r << 8) | a;
    
    Output[0] = entry;

    // TODO: Gather average somehow
}

