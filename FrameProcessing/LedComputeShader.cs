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
            var ms = new MemoryStream();
            compilationResult.Bytecode.Data.CopyTo(ms);
            _shaderByteCode = ms.ToArray();
        }

        internal ComputeShader GetComputeShader(SharpDX.Direct3D11.Device device) {
            return new ComputeShader(device, _shaderByteCode);
        }
    }
}