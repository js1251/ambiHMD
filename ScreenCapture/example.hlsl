Texture2D Input : register(t0);

RWStructuredBuffer<float3> Output : register(u0);

[numthreads(32, 32, 1)]
void main(uint3 id : SV_DispatchThreadID) {
    // TODO: Input.GetDimensions()
    int2 res = 1;
    if (any(id.xy >= res)) return;

    // Output[myindex] = asdaasd;

    // TODO: Gather average somehow
}