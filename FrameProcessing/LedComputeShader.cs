using System.IO;
using SharpDX.Direct3D11;
using SharpDX.Direct3D9;

namespace FrameProcessing {
    public sealed class LedComputeShader {
        private readonly byte[] _shaderByteCode;

        public LedComputeShader(FileSystemInfo shaderFileInfo) {
            // compile shader bytecode
            var compilationResult =
                ShaderBytecode.CompileFromFile(shaderFileInfo.FullName, "main", "cs_5_0", ShaderFlags.Debug);

            // read into byte array
            _shaderByteCode = new byte[compilationResult.Bytecode.BufferSize];
            compilationResult.Bytecode.Data.Read(_shaderByteCode, 0, compilationResult.Bytecode.BufferSize);
        }

        internal ComputeShader GetComputeShader(SharpDX.Direct3D11.Device device) {
            return new ComputeShader(device, _shaderByteCode);
        }
    }
}