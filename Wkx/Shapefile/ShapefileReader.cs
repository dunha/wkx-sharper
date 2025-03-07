using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using static System.Net.Mime.MediaTypeNames;

namespace Wkx
{
    internal class ShapefileReader : WkbReader
    {
        private const double NODATAMAX = -1E+38;

        internal ShapefileReader(Stream stream)
            : base(stream)
        {
        }

        /// <summary>
        /// Reads content section bytes of Shapefile Shape record (skipping first 8 bytes)
        /// Can read Srid as Int32 by prefixing the bytearray with 5 bytes
        /// composed of the Srid presence identifier 0xFE and 4 byte Int32 Srid (little endian)
        /// </summary>
        /// <returns>Geometry</returns>
        /// <exception cref="NotSupportedException"></exception>
        internal new Geometry Read()
        {
            wkbReader.IsBigEndian = false;
            int? srid = 0;
            if (wkbReader.ReadByte() == 0xFE)
            {
                srid = wkbReader.ReadInt32();
            }
            else
            {
                wkbReader.BaseStream.Position = 0;
            }
            var esriType = wkbReader.ReadInt32();
            var (geometryType, dimension) = GetGeometryType(esriType);

            Geometry geometry;
            switch (geometryType)
            {
                case GeometryType.Point: geometry = ReadPoint(dimension); break;
                case GeometryType.LineString: geometry = ReadLine(dimension); break;
                case GeometryType.Polygon: geometry = ReadPolygon(dimension); break;
                case GeometryType.MultiPoint: geometry = ReadMultiPoint(dimension); break;
                default: throw new NotSupportedException(geometryType.ToString());
            }
            geometry.Srid = srid;
            return geometry;
        }





        protected (GeometryType geometryType, Dimension dimension)  GetGeometryType(int esriType)
        {
            var type = esriType % 10;
            var d = esriType / 10;
            GeometryType geom;
            Dimension dim;
            switch (type)
            {
                case 0: throw new IndexOutOfRangeException("Null shapes not supported");
                case 1: geom = GeometryType.Point; break;
                case 3: geom = GeometryType.LineString; break;
                case 5: geom = GeometryType.Polygon; break;
                case 8: geom = GeometryType.MultiPoint; break; 
                default:
                    throw new IndexOutOfRangeException($"Unsupported Shape type {type}");
            }
            switch (d)
            {
                case 0: dim = Dimension.Xy; break;
                case 1: dim = Dimension.Xyzm; break;
                case 2: dim = Dimension.Xym; break;
                default:
                    throw new IndexOutOfRangeException($"Unsupported dimension {d}");
            }
            return (geom, dim);

            //return (GeometryType)(type & 0XFF);
        }


        protected override Dimension ReadDimension(uint esriType)
        {
            var dimension = esriType / 10;
            switch (dimension)
            {
                case 0: return Dimension.Xy;
                case 1: return Dimension.Xyzm;
                case 2: return Dimension.Xym;
                default:
                    throw new IndexOutOfRangeException("Unsupported Dimension type");
            }
        }


        private Point ReadPoint(Dimension dimension)
        {
            switch (dimension)
            {
                case Dimension.Xy: return new Point(wkbReader.ReadDouble(), wkbReader.ReadDouble());
                case Dimension.Xyz: return new Point(wkbReader.ReadDouble(), wkbReader.ReadDouble(), wkbReader.ReadDouble());
                case Dimension.Xym: return new Point(wkbReader.ReadDouble(), wkbReader.ReadDouble(), null, wkbReader.ReadDouble());
                case Dimension.Xyzm: return new Point(wkbReader.ReadDouble(), wkbReader.ReadDouble(), wkbReader.ReadDouble(), wkbReader.ReadDouble());
                default: throw new NotSupportedException(dimension.ToString());
            }
        }


        private (int PartCount, List<int> Parts, List<Point> Points) ReadLinearRecord(Dimension dimension)
        {
            wkbReader.BaseStream.Position = 36;
            var numParts = wkbReader.ReadInt32();
            var numPoints = wkbReader.ReadInt32();
            var zArray = new List<double>();
            var mArray = new List<double>();
            var parts = new List<int>();
            if (numParts == 0 || numPoints == 0) return (0, null, null);
            for (int i = 0; i < numParts; i++)
            {
                parts.Add(wkbReader.ReadInt32());
            }
            var coordinates = new List<double[]>();
            for (int i = 0; i < numPoints; i++)
            {
                coordinates.Add(new double[] { wkbReader.ReadDouble(), wkbReader.ReadDouble() });
            }
            if (dimension == Dimension.Xym || dimension == Dimension.Xyzm)
            {
                if (dimension == Dimension.Xyzm || dimension == Dimension.Xyz)
                {
                    _ = wkbReader.ReadDouble();
                    _ = wkbReader.ReadDouble();
                    //zArray = new double[numPoints];
                    for (int i = 0; i < numPoints; i++)
                    {
                        zArray.Add(wkbReader.ReadDouble());
                    }
                }
                if (dimension == Dimension.Xyzm || dimension == Dimension.Xym)
                {
                    var mMin = wkbReader.ReadDouble();
                    var mMax = wkbReader.ReadDouble();
                    if (mMin > NODATAMAX || mMax > NODATAMAX)
                    {
                        //mArray = new double[numPoints];
                        //hasM = true;
                        for (int i = 0; i < numPoints; i++)
                        {
                            mArray.Add(wkbReader.ReadDouble());
                        }

                    }
                    else
                    {
                        if (dimension == Dimension.Xyzm)
                            dimension = Dimension.Xyz;
                        if (dimension == Dimension.Xym)
                            dimension = Dimension.Xy;
                        wkbReader.BaseStream.Position += numPoints * 8;
                    }

                }
                //record.Marray = new double[numPoints];

            }
            var points = new List<Point>();
            //foreach (var coord in record.Coordinates)
            for (int i = 0; i < coordinates.Count; i++)
            {
                var coord = coordinates[i];
                switch (dimension)
                {
                    case Dimension.Xy:
                        //pt = new double[2] { points[i][0], points[i][0] };
                        points.Add(new Point(coord[0], coord[1]));
                        break;
                    case Dimension.Xyz:
                        points.Add(new Point(coord[0], coord[1], zArray[i]));
                        break;
                    case Dimension.Xym:
                        points.Add(new Point(coord[0], coord[1], null, mArray[i]));
                        break;
                    case Dimension.Xyzm:
                        points.Add(new Point(coord[0], coord[1], zArray[i], mArray[i]));
                        break;
                    default:
                        break;
                }

            }
            //record.WkxDimension = dimension;
            return (numParts, parts, points);

        }


        private Geometry ReadLine(Dimension dimension)
        {
            var (PartCount, Parts, Points) = ReadLinearRecord(dimension);
            if (PartCount == 0) return null;
            if (PartCount == 1)
            {
                return new LineString(Points);
            }
            else
            {
                var lines = new List<LineString>();
                for (var i = 0; i < PartCount - 1; i++)
                {
                    lines.Add(new LineString(Points.GetRange(Parts[i], Parts[i + 1] - Parts[i])));
                }
                lines.Add(new LineString(Points.GetRange(Parts.Last(), Points.Count - 1 - Parts.Last())));
                return new MultiLineString(lines);
            }
        }

        private Geometry ReadPolygon(Dimension dimension)
        {
            var (PartCount, Parts, Points) = ReadLinearRecord(dimension);
            if (PartCount == 0) return null;
            if (PartCount == 1)
            {
                return new Polygon(Points);
            }
            else
            {
                var rings = new List<LinearRing>();
                for (var i = 0; i < PartCount - 1; i++)
                {
                    rings.Add(new LinearRing(Points.GetRange(Parts[i], Parts[i + 1] - Parts[i])));
                }
                rings.Add(new LinearRing(Points.GetRange(Parts.Last(), Points.Count - 1 - Parts.Last())));
                var errors = new Dictionary<string, int>();
                var geom = PolygonFunctions.OrganizePolygonRings(rings, errors);
                return geom;
            }
        }

        private MultiPoint ReadMultiPoint(Dimension dimension)
        {
            MultiPoint multiPoint = new MultiPoint();
            wkbReader.BaseStream.Position = 36;
            var pointCount = wkbReader.ReadInt32();
            var coords = new List<double[]>();
            var zArray = new List<double>();
            var mArray = new List<double>();
            for (int i = 0; i < pointCount; i++)
                coords.Add(new double[2] { wkbReader.ReadDouble(), wkbReader.ReadDouble() });
            if (dimension == Dimension.Xyzm || dimension == Dimension.Xym)
            {
                if (dimension == Dimension.Xyzm || dimension == Dimension.Xyz)
                {
                    _ = wkbReader.ReadDouble();
                    _ = wkbReader.ReadDouble();
                    zArray = new List<double>();
                    for (int i = 0; (i < pointCount); i++)
                    {
                        zArray.Add(wkbReader.ReadDouble());
                    }
                }
                if (dimension == Dimension.Xyzm || dimension == Dimension.Xym)
                {
                    var mMin = wkbReader.ReadDouble();
                    var mMax = wkbReader.ReadDouble();
                    //mArray = new List<double>();
                    if (mMin > NODATAMAX || mMax > NODATAMAX)
                    {
                        //mArray = new List<double>();
                        //hasM = true;
                        for (int i = 0; (i < pointCount); i++)
                        {
                            mArray.Add(wkbReader.ReadDouble());
                        }

                    }
                    else
                    {
                        if (dimension == Dimension.Xyzm)
                            dimension = Dimension.Xyz;
                        if (dimension == Dimension.Xym)
                            dimension = Dimension.Xy;
                        wkbReader.BaseStream.Position += pointCount * 8;
                    }

                }
                for (int i = 0; i < coords.Count; i++)
                {
                    var coord = coords[i];
                    switch (dimension)
                    {
                        case Dimension.Xy:
                            //pt = new double[2] { points[i][0], points[i][0] };
                            multiPoint.Geometries.Add(new Point(coord[0], coord[1]));
                            break;
                        case Dimension.Xyz:
                            multiPoint.Geometries.Add(new Point(coord[0], coord[1], zArray[i]));
                            break;
                        case Dimension.Xym:
                            multiPoint.Geometries.Add(new Point(coord[0], coord[1], null, mArray[i]));
                            break;
                        case Dimension.Xyzm:
                            multiPoint.Geometries.Add(new Point(coord[0], coord[1], zArray[i], mArray[i]));
                            break;
                        default:
                            break;
                    }

                }

            }
            multiPoint.Dimension = dimension;
            return multiPoint;
        }




    }



}

