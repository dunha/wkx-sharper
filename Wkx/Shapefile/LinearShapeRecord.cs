using System;
using System.Collections.Generic;
using System.Text;

namespace Wkx
{
    internal class LinearShapeRecord
    {

        public class Coordinate
        {
            public double X { get; set; }
            public double Y { get; set; }

            public Coordinate() { }
            public Coordinate(double x, double y)
            {
                X = x;
                Y = y;
            }
        }

        public int RecordNumber { get; set; }
        public int RecordOffset { get; set; }
        public int ContentLengthWords { get; set; }
        public int ContentLengthBytes { get; set; }
        public int ShapeType { get; set; }
        public int PartCount { get; set; }
        public int PointCount { get; set; }
        public List<double> Zarray { get; set; }
        public List<double> Marray { get; set; }
        public List<int> Parts { get; set; }
        public List<Coordinate> Coordinates { get; set; }
        public Dimension WkxDimension { get; set; }
        public List<Point> Points { get; set; }







        protected GeometryType ReadGeometryType(uint esriType, uint partCount)
        {
            var type = esriType % 10;
            switch (type)
            {
                case 0: throw new IndexOutOfRangeException("Null shapes not supported");
                case 1: return GeometryType.Point;
                case 3:
                    return partCount == 1 ? GeometryType.LineString : GeometryType.MultiLineString;
                case 5:
                    return partCount == 1 ? GeometryType.Polygon : GeometryType.MultiPolygon;
                case 8:
                    return GeometryType.MultiPoint;
                default:
                    throw new IndexOutOfRangeException("Unsupported Shape type");

            }

            //return (GeometryType)(type & 0XFF);
        }

        protected Dimension ReadDimension(double[] envelope)
        {
            var z = envelope[4] + envelope[5];
            var m = envelope[6] + envelope[7];
            if (z > 0 & m > 0) return Dimension.Xyzm;
            if (z > 0 & m == 0) return Dimension.Xyz;
            if (z == 0 & m > 0) return Dimension.Xym;
            return Dimension.Xy;
        }

    }
}
