using System.IO;
using SharpDX.Direct3D11;
using SharpDX.Direct3D9;

namespace Composition.WindowsRuntimeHelpers {
    public sealed class ComputeShaderCompileHelper {
        private readonly byte[] _shaderByteCode;

        public ComputeShaderCompileHelper(FileSystemInfo shaderFileInfo) {
            // compile shader bytecode
            var compilationResult =
                ShaderBytecode.CompileFromFile(shaderFileInfo.FullName, "main", "cs_5_0", ShaderFlags.Debug);

            // read into byte array
            _shaderByteCode = new byte[compilationResult.Bytecode.BufferSize];
            compilationResult.Bytecode.Data.Read(_shaderByteCode, 0, compilationResult.Bytecode.BufferSize);
        }

        public ComputeShader CreateWithDevice(SharpDX.Direct3D11.Device device) {
            return new ComputeShader(device, _shaderByteCode);
        }
    }
}