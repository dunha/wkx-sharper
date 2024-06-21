using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;

namespace Wkx
{
    internal class ShapefileWriter : WkbWriter
    {

        //private const double NODATAMAX = -1E+38;
        //private const double nullM = -3.1050361846014175E+231;
        private static readonly double nullM = BitConverter.ToDouble( new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFE }, 0);
        //private BinaryWriter geomWriter;
        //private MemoryStream geometry;
        bool hasZ;
        bool mIsNull;
        bool hasM;

        //bool hasXNullm;

        internal new byte[] Write(Geometry geometry)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                wkbWriter = new BinaryWriter(memoryStream);
                WriteWkbGeometry(geometry);

                return memoryStream.ToArray();
            }
        }


        //private void WriteInternal(Geometry geometry)
        //{
        //    // Write header
        //    //WriteWkbType(geometry.GeometryType);
        //    WriteWkbGeometry(geometry);


        //}







        #region wkb



        protected void WriteWkbGeometry(Geometry geometry)
        {
            switch (geometry.GeometryType)
            {
                case GeometryType.Point: WritePoint(geometry as Point); break;
                case GeometryType.LineString:
                case GeometryType.MultiLineString:
                    WritePolyLine(geometry);
                    break;
                case GeometryType.Polygon:
                case GeometryType.MultiPolygon:
                    WritePolygon(geometry);
                    break;
                case GeometryType.MultiPoint: WriteMultiPoint(geometry as MultiPoint); break;
                // Not supported by Shapefile
                //case GeometryType.GeometryCollection: WriteGeometryCollection(geometry as GeometryCollection); break;
                //case GeometryType.CircularString: WriteCircularString(geometry as CircularString); break;
                //case GeometryType.CompoundCurve: WriteCompoundCurve(geometry as CompoundCurve); break;
                //case GeometryType.CurvePolygon: WriteCurvePolygon(geometry as CurvePolygon); break;
                //case GeometryType.MultiCurve: WriteMultiCurve(geometry as MultiCurve); break;
                //case GeometryType.MultiSurface: WriteMultiSurface(geometry as MultiSurface); break;
                //case GeometryType.PolyhedralSurface: WritePolyhedralSurface(geometry as PolyhedralSurface); break;
                //case GeometryType.Tin: WriteTin(geometry as Tin); break;
                //case GeometryType.Triangle: WriteTriangle(geometry as Triangle); break;
                default: throw new NotSupportedException(geometry.GeometryType.ToString());
            }

        }


        protected int EsriType(GeometryType geometryType, Dimension dimension)
        {
            int esriType;
            switch (geometryType)
            {
                case GeometryType.Point:
                    esriType = 1;
                    break;
                case GeometryType.LineString:
                case GeometryType.MultiLineString:
                    esriType = 3;
                    break;
                case GeometryType.Polygon:
                case GeometryType.MultiPolygon:
                    esriType = 5;
                    break;
                case GeometryType.MultiPoint:
                    esriType = 8;
                    break;
                default:
                    throw new NotSupportedException(geometryType.ToString());
            }
            hasZ = false;
            hasM = false;
            mIsNull = false;
            switch (dimension)
            {
                case Dimension.Xy:
                    break;
                case Dimension.Xyz:
                    esriType += 10;
                    hasZ = true;
                    hasM = true;
                    mIsNull = true;
                    break;
                case Dimension.Xym:
                    hasM = true;
                    esriType += 20;
                    break;
                case Dimension.Xyzm:
                    esriType += 10;
                    hasZ = true;
                    hasM = true;
                    break;
                default:
                    break;
            }
            return esriType;

        }




        private void WritePoint(Point point)
        {
            wkbWriter.Write(EsriType(GeometryType.Point, point.Dimension));
            WriteDouble(point.X);
            WriteDouble(point.Y);

            switch (point.Dimension)
            {
                case Dimension.Xyz:
                    WriteDouble(point.Z);
                    WriteM(null);
                    break;
                case Dimension.Xym:
                    WriteDouble(point.M);
                    break;
                case Dimension.Xyzm:
                    WriteDouble(point.Z);
                    WriteDouble(point.M);
                    break;
                default:
                    break;
            }
        }

        private void WriteDouble(double? value)
        {
            if (value.HasValue)
                wkbWriter.Write(value.Value);
            else
                wkbWriter.Write(doubleNaN);
        }


        private void WriteM(double? value)
        {
            if (value.HasValue)
                wkbWriter.Write(value.Value);
            else
                wkbWriter.Write(nullM);
        }


        private double[] BoxToArray(BoundingBox box)
        {
            return new double[] { box.XMin, box.YMin, box.XMax, box.YMax };
        }

        private void WritePolyLine(Geometry polyline)
        {
            var box = BoxToArray(polyline.GetBoundingBox());
            var parts = new List<int>();
            //var hasZ = polyline.Dimension == Dimension.Xyz || polyline.Dimension == Dimension.Xyzm ? true : false; 
            //var mIsNull = polyline.Dimension == Dimension.Xyz ? true : false;
            //var hasM = polyline.Dimension == Dimension.Xy ? false : true; ;
            var ords = new List<double>();
            var esriType = EsriType(GeometryType.LineString, polyline.Dimension);
            var polyLines = polyline.GetType() == typeof(LineString) ?
                new MultiLineString(new List<LineString>() { polyline as LineString }) :
                polyline as MultiLineString;
            var numParts = polyLines.Geometries.Count;
            var numPoints = polyLines.Geometries.Sum(p => p.Points.Count());
            var zArray = new List<double?>();
            var mArray = new List<double?>();
            var partOffset = 0;

            foreach (var line in polyLines.Geometries)
            {
                parts.Add(partOffset);
                foreach (var point in line.Points)
                {
                    ords.Add((double)point.X);
                    ords.Add((double)point.Y);
                    if (point.Z.HasValue) zArray.Add(point.Z);
                    if (point.M.HasValue && !mIsNull)
                        if (mIsNull) mArray.Add(point.M);
                        else mArray.Add(nullM);

                }
                partOffset += line.Points.Count;
            }
            //parts.RemoveAt(parts.Count - 1);

            wkbWriter.Write(esriType);
            for (int i = 0; i < 4; i++)
                wkbWriter.Write(box[i]);

            wkbWriter.Write(numParts);
            wkbWriter.Write(numPoints);
            for (int i = 0; i < numParts; i++)
                wkbWriter.Write(parts[i]);

            for (int i = 0; i < ords.Count; i++)
                wkbWriter.Write(ords[i]);

            if (hasZ)
            {
                wkbWriter.Write((double)zArray.Min());
                wkbWriter.Write((double)zArray.Max());
                for (int i = 0; i < numPoints; i++)
                    wkbWriter.Write((double)zArray[i]);
            }
            if (hasM)
            {
                wkbWriter.Write((double)mArray.Min());
                wkbWriter.Write((double)mArray.Max());
                for (int i = 0; i < numPoints; i++)
                    wkbWriter.Write((double)mArray[i]);
            }


        }



        private void WritePolygon(Geometry polygon)
        {
            if (polygon.IsEmpty)
            {
                //wkbWriter.Write(0);
                return;
            }
            var box = BoxToArray(polygon.GetBoundingBox());
            var parts = new List<int>();
            //var hasZ = polyline.Dimension == Dimension.Xyz || polyline.Dimension == Dimension.Xyzm ? true : false; 
            //var mIsNull = polyline.Dimension == Dimension.Xyz ? true : false;
            //var hasM = polyline.Dimension == Dimension.Xy ? false : true; ;
            var ords = new List<double>();
            var esriType = EsriType(GeometryType.LineString, polygon.Dimension);
            var polygons = polygon.GetType() == typeof(Polygon) ?
                new MultiPolygon(new List<Polygon>() { polygon as Polygon }) :
                polygon as MultiPolygon;
            var rings = new List<LinearRing>();
            foreach (var poly in polygons.Geometries)
            {
                if (!PolygonFunctions.IsCw(poly.ExteriorRing.Points))
                    PolygonFunctions.Rewind(poly.ExteriorRing.Points);
                rings.Add(poly.ExteriorRing);
                foreach (var inner in poly.InteriorRings)
                {
                    if (PolygonFunctions.IsCw(inner.Points))
                        PolygonFunctions.Rewind(inner.Points);
                    rings.Add(inner);

                }
            }
            var numParts = rings.Count;
            var numPoints = rings.Sum(p => p.Points.Count());
            var zArray = new List<double?>();
            var mArray = new List<double?>();

            var partOffset = 0;
            foreach (var ring in rings)
            {
                parts.Add(partOffset);
                foreach (var point in ring.Points)
                {
                    ords.Add((double)point.X);
                    ords.Add((double)point.Y);
                    if (point.Z.HasValue) zArray.Add(point.Z);
                    if (point.M.HasValue && !mIsNull)
                        if (mIsNull) mArray.Add(point.M);
                        else mArray.Add(nullM);

                }
                partOffset += ring.Points.Count;

            }
            //parts.RemoveAt(parts.Count - 1);

            wkbWriter.Write(esriType);
            for (int i = 0; i < 4; i++)
                wkbWriter.Write(box[i]);

            wkbWriter.Write(numParts);
            wkbWriter.Write(numPoints);
            for (int i = 0; i < numParts; i++)
                wkbWriter.Write(parts[i]);

            for (int i = 0; i < ords.Count; i++)
                wkbWriter.Write(ords[i]);

            if (hasZ)
            {
                wkbWriter.Write((double)zArray.Min());
                wkbWriter.Write((double)zArray.Max());
                for (int i = 0; i < numPoints; i++)
                    wkbWriter.Write((double)zArray[i]);
            }
            if (hasM)
            {
                wkbWriter.Write((double)mArray.Min());
                wkbWriter.Write((double)mArray.Max());
                for (int i = 0; i < numPoints; i++)
                    wkbWriter.Write((double)mArray[i]);
            }

        }

        private void WriteMultiPoint(MultiPoint multiPoint)
        {
            var esriType = EsriType(GeometryType.MultiPoint, multiPoint.Dimension);
            var box = BoxToArray(multiPoint.GetBoundingBox());

            wkbWriter.Write(esriType);
            for (int i = 0; i < 4; i++)
                wkbWriter.Write(box[i]);
            wkbWriter.Write(multiPoint.Geometries.Count);
            foreach (var point in multiPoint.Geometries)
            {
                wkbWriter.Write((double)point.X);
                wkbWriter.Write((double)point.Y);
            }
            if (hasZ)
            {
                wkbWriter.Write((double)multiPoint.Geometries.Min(p => p.Z));
                wkbWriter.Write((double)multiPoint.Geometries.Max(p => p.Z));
                foreach (var point in multiPoint.Geometries)
                {
                    wkbWriter.Write((double)point.Z);
                }
            }
            if (hasM)
            {
                if (!mIsNull)
                {
                    wkbWriter.Write((double)multiPoint.Geometries.Min(p => p.M));
                    wkbWriter.Write((double)multiPoint.Geometries.Max(p => p.M));
                    foreach (var point in multiPoint.Geometries)
                    {
                        wkbWriter.Write((double)point.M);
                    }
                }
                else
                    wkbWriter.Write(nullM);

            }


        }




        #endregion



    }
}
