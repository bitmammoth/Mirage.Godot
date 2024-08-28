/*
MIT License

Copyright (c) 2021 James Frowen

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using Godot;

namespace Mirage.Serialization
{
    /// <summary>
    /// Packs a vector3 using <see cref="ZigZag"/> and <see cref="VarIntBlocksPacker"/>
    /// </summary>
    public sealed class VarVector3Packer
    {
        private readonly VarFloatPacker _xPacker;
        private readonly VarFloatPacker _yPacker;
        private readonly VarFloatPacker _zPacker;

        public VarVector3Packer(Vector3 precision, int blocksize)
        {
            _xPacker = new VarFloatPacker((float)precision.X, blocksize);
            _yPacker = new VarFloatPacker((float)precision.Y, blocksize);
            _zPacker = new VarFloatPacker((float)precision.Z, blocksize);
        }

        public void Pack(NetworkWriter writer, Vector3 position)
        {
            _xPacker.Pack(writer, (float)position.X);
            _yPacker.Pack(writer, (float)position.Y);
            _zPacker.Pack(writer, (float)position.Z);
        }

        public Vector3 Unpack(NetworkReader reader)
        {
            Vector3 value = default;
            value.X = _xPacker.Unpack(reader);
            value.Y = _yPacker.Unpack(reader);
            value.Z = _zPacker.Unpack(reader);
            return value;
        }
    }
}
