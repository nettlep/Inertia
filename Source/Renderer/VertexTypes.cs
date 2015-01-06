using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Xml;
using System.Diagnostics;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace Inertia
{
    /// <summary>
    /// Vertex format which includes position & texture UV
    /// </summary>
    public struct SVertexPNDT
    {
        public Vector3 Position;
        public Vector3 Normal;
        public uint Diffuse;
        public Vector2 Texture;

        public SVertexPNDT(float x, float y, float z, float nx, float ny, float nz, float a, float r, float g, float b, float u, float v)
        {
            Position.X = x;
            Position.Y = y;
            Position.Z = z;
            Normal.X = nx;
            Normal.Y = ny;
            Normal.Z = nz;
            uint ia = (uint)(a * 255) & 0xff;
            uint ir = (uint)(r * 255) & 0xff;
            uint ig = (uint)(g * 255) & 0xff;
            uint ib = (uint)(b * 255) & 0xff;
            Diffuse = (ia << 24) | (ir << 16) | (ig << 8) | ib;
            Texture.X = u;
            Texture.Y = v;
        }

        public static readonly VertexFormats FVF_Flags = VertexFormats.Position | VertexFormats.Normal | VertexFormats.Diffuse | VertexFormats.Texture1;
    };
}
