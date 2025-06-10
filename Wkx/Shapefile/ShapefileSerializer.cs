using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Wkx;

namespace Wkx
{
    public class ShapefileSerializer : IGeometrySerializer
    {
        public Geometry Deserialize(Stream stream)
        {
            return new ShapefileReader(stream).Read();
        }

        public Geometry Deserialize(Stream stream, int? srid = 0)
        {
            return new ShapefileReader(stream, srid).Read();
        }

        public void Serialize(Geometry geometry, Stream stream)
        {
            byte[] buffer = new ShapefileWriter().Write(geometry);
            stream.Write(buffer, 0, buffer.Length);
        }
    }
}
