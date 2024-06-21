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
        public int? Srid { get; set; }
        //public bool HasM { get; set; }
        //public bool HasZ { get; set; }

        //private Record record = new Record();
        //private bool valid = false;
        //private bool hasM = false;
        //private Dimension wkxDimension;
        private const double NODATAMAX = -1E+38;

        internal ShapefileReader(Stream stream)
            : base(stream)
        {
        }

        internal ShapefileReader(Stream stream, int? srid)
            : base(stream)
        {
            Srid = srid;
        }

        internal new Geometry Read()
        {
            //if ( !valid) Header = ReadHeader();
            //valid = true;
            //wkbReader.IsBigEndian = false;
            //var recordNumber = wkbReader.ReadInt32();
            //var contentLengthWords = wkbReader.ReadInt32();
            //var contentLengthBytes = contentLengthWords * 2;
            wkbReader.IsBigEndian = false;
            var esriType = wkbReader.ReadInt32();
            var (geometryType, dimension) = GetGeometryType(esriType);

            Geometry geometry;
            switch (geometryType)
            {
                case GeometryType.Point: geometry = ReadPoint(dimension); break;
                case GeometryType.LineString: geometry = ReadLine(dimension); break;
                case GeometryType.Polygon: geometry = ReadPolygon(dimension); break;
                case GeometryType.MultiPoint: geometry = ReadMultiPoint(dimension); break;
                //case GeometryType.MultiLineString: geometry = ReadMultiLineString(geom.dimension); break;
                //case GeometryType.MultiPolygon: geometry = ReadMultiPolygon(geom.dimension); break;
                // Not supported by Spatialite
                //case GeometryType.GeometryCollection: geometry = ReadGeometryCollection(dimension); break;
                //case GeometryType.CircularString: geometry = ReadCircularString(dimension); break;
                //case GeometryType.CompoundCurve: geometry = ReadCompoundCurve(dimension); break;
                //case GeometryType.CurvePolygon: geometry = ReadCurvePolygon(dimension); break;
                //case GeometryType.MultiCurve: geometry = ReadMultiCurve(dimension); break;
                //case GeometryType.MultiSurface: geometry = ReadMultiSurface(dimension); break;
                //case GeometryType.PolyhedralSurface: geometry = ReadPolyhedralSurface(dimension); break;
                //case GeometryType.Tin: geometry = ReadTin(dimension); break;
                //case GeometryType.Triangle: geometry = ReadTriangle(dimension); break;
                default: throw new NotSupportedException(geometryType.ToString());
            }
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

        //protected GeometryType ReadGeometryType(uint esriType, uint partCount)
        //{
        //    var type = esriType % 10;
        //    switch (type)
        //    {
        //        case 0: throw new IndexOutOfRangeException("Null shapes not supported");
        //        case 1: return GeometryType.Point;
        //        case 3: 
        //            return partCount == 1 ? GeometryType.LineString : GeometryType.MultiLineString ;
        //        case 5: 
        //            return partCount == 1 ? GeometryType.Polygon : GeometryType.MultiPolygon ;
        //        case 8: 
        //            return GeometryType.MultiPoint;
        //        default:
        //            throw new IndexOutOfRangeException("Unsupported Shape type");

        //    }

        //    //return (GeometryType)(type & 0XFF);
        //}

        //protected Dimension ReadDimension(double[] envelope)
        //{
        //    var z = envelope[4] + envelope[5];
        //    var m = envelope[6] + envelope[7];
        //    if (z > 0 & m > NODATAMAX) return Dimension.Xyzm;
        //    if (z > 0 & m < NODATAMAX) return Dimension.Xyz;
        //    if (z == 0 & m > 0) return Dimension.Xym;
        //    return Dimension.Xy;
        //}

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
            //if ((type & EwkbFlags.HasZ) == EwkbFlags.HasZ && (type & EwkbFlags.HasM) == EwkbFlags.HasM)
            //    return Dimension.Xyzm;
            //else if ((type & EwkbFlags.HasZ) == EwkbFlags.HasZ)
            //    return Dimension.Xyz;
            //else if ((type & EwkbFlags.HasM) == EwkbFlags.HasM)
            //    return Dimension.Xym;

            //return Dimension.Xy;
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


        //private LineString ReadLineString(Dimension dimension)
        //{
        //    LineString lineString = new LineString();

        //    //uint pointCount = wkbReader.ReadUInt32();
        //    for (int i = 0; i < points.Count; i++)
        //        lineString.Points.Add(MakePoint(dimension, points[i]));
        //    return lineString;
        //}


        //private Polygon ReadPolygon(Dimension dimension)
        //{
        //    var record = ReadLinearRecord(dimension);
        //    var rings = new List<LinearRing>();
        //    //var exteriorRings = new List<LinearRing>();
        //    //var holes = new List<LinearRing>();
        //    //var polys = new List<List<LinearRing>>();
        //    if (record.PartCount == 0) return null;
        //    for (int i = 0; i < record.PartCount; i++)
        //    {
        //        rings.Add(new LinearRing(record.Points.GetRange(record.Parts[0], record.Parts[i+1] - record.Parts[i])));
        //    }
        //    PolygonFunctions.OrganizePolygonRings(rings)
        //    foreach (var ring in rings)
        //    {
        //        if(IsCCW(ring))
        //        {
        //            holes.Add(ring);
        //        }
        //        else
        //        {
        //            exteriorRings.Add(ring);
        //        }

        //    }
            



            //Polygon polygon = new Polygon();

            //uint ringCount = wkbReader.ReadUInt32();

            //if (ringCount > 0)
            //{
            //    uint exteriorRingCount = wkbReader.ReadUInt32();
            //    for (int i = 0; i < exteriorRingCount; i++)
            //        polygon.ExteriorRing.Points.Add(ReadPoint(dimension));

            //    for (int i = 1; i < ringCount; i++)
            //    {
            //        polygon.InteriorRings.Add(new LinearRing());

            //        uint interiorRingCount = wkbReader.ReadUInt32();
            //        for (int j = 0; j < interiorRingCount; j++)
            //            polygon.InteriorRings[i - 1].Points.Add(ReadPoint(dimension));
            //    }
            //}
            //if (dimension == Dimension.Xyzm && polygon.ExteriorRing.Points.TrueForAll(p => p.M == 0) )
            //{
                //if (polygon.InteriorRings.Count > 0 && (polygon.InteriorRings.TrueForAll(pg => pg.Points.TrueForAll(p => p.M == 0))))
                //{

                //}
                //var pg = new Polygon();
                //pg.Dimension = Dimension.Xyz; 
                //foreach (var p in lineString.Points)
                //{
                //    ls.Points.Add(new Point((double)p.X, (double)p.Y, p.Z, null));
                //    return ls;
                //}
        //    }

        //    return polygon;
        //}

        //private MultiLineString ReadMultiLineString(Dimension dimension)
        //{
        //    MultiLineString multiLineString = new MultiLineString();

        //    uint lineStringCount = wkbReader.ReadUInt32();
        //    var points = record.Points;
        //    for (int i = 0; i < record.Parts.Length; i++)
        //    {
        //        var line = points.Take(record.Parts[i + 1] - record.Parts[i]);
        //        multiLineString.Geometries.Add(Read<LineString>());
        //    }
                

        //    return multiLineString;
        //}

        //private MultiLineString ReadMultiLineString(LinearShapeRecord record)
        //{
        //    MultiLineString multiLineString = new MultiLineString();

        //    uint lineStringCount = wkbReader.ReadUInt32();

        //    for (int i = 0; i < lineStringCount; i++)
        //        multiLineString.Geometries.Add(Read<LineString>());

        //    return multiLineString;
        //}

        //private MultiPolygon ReadMultiPolygon(Dimension dimension)
        //{
        //    MultiPolygon multiPolygon = new MultiPolygon();

        //    uint polygonCount = wkbReader.ReadUInt32();

        //    for (int i = 0; i < polygonCount; i++)
        //        multiPolygon.Geometries.Add(Read<Polygon>());

        //    return multiPolygon;
        //}

        //private GeometryCollection ReadGeometryCollection(Dimension dimension)
        //{
        //    GeometryCollection geometryCollection = new GeometryCollection();

        //    uint geometryCount = wkbReader.ReadUInt32();

        //    for (int i = 0; i < geometryCount; i++)
        //        geometryCollection.Geometries.Add(Read());

        //    return geometryCollection;
        //}


    }



}

