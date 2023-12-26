using System;
using System.IO;

namespace Wkx
{
    internal class SpatialiteWriter : WkbWriter
    {

        bool firstRun = true;

        //protected static readonly byte[] doubleNaN = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xf8, 0x7f };

        //protected BinaryWriter wkbWriter;

        internal new byte[] Write(Geometry geometry)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                wkbWriter = new BinaryWriter(memoryStream);
                WriteInternal(geometry);
                wkbWriter.Write((byte)0xFE);

                return memoryStream.ToArray();
            }
        }


        private void WriteInternal(Geometry geometry, Geometry parentGeometry = null)
        {
            // Write header
            if (firstRun)
            {
                wkbWriter.Write(false);
                wkbWriter.Write(true);
                wkbWriter.Write(geometry.Srid.Value);
                var mbr = geometry.GetBoundingBox();
                wkbWriter.Write((double)mbr.XMin);
                wkbWriter.Write((double)mbr.YMin);
                wkbWriter.Write((double)mbr.XMax);
                wkbWriter.Write((double)mbr.YMax);
                wkbWriter.Write((byte)0x7C);
                firstRun = false;
            }
            var dimension = parentGeometry != null ? parentGeometry.Dimension : geometry.Dimension;
            WriteWkbType(geometry.GeometryType, dimension, null);
            WriteWkbGeometry(geometry, dimension);


        }







        #region wkb



        protected void WriteWkbGeometry(Geometry geometry, Dimension dimension)
        {
            switch (geometry.GeometryType)
            {
                case GeometryType.Point: WritePoint(geometry as Point, dimension); break;
                case GeometryType.LineString: WriteLineString(geometry as LineString); break;
                case GeometryType.Polygon: WritePolygon(geometry as Polygon); break;
                case GeometryType.MultiPoint: WriteMultiPoint(geometry as MultiPoint); break;
                case GeometryType.MultiLineString: WriteMultiLineString(geometry as MultiLineString); break;
                case GeometryType.MultiPolygon: WriteMultiPolygon(geometry as MultiPolygon); break;
                case GeometryType.GeometryCollection: WriteGeometryCollection(geometry as GeometryCollection); break;
                // Not supported by Spatialite
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

        //private void WriteWkbType(GeometryType geometryType, Dimension dimension)
        //{
        //    uint dimensionType = 0;

        //    switch (dimension)
        //    {
        //        case Dimension.Xyz: dimensionType = 1000; break;
        //        case Dimension.Xym: dimensionType = 2000; break;
        //        case Dimension.Xyzm: dimensionType = 3000; break;
        //    }
        //    wkbWriter.Write((uint)(dimensionType + (uint)geometryType));
        //}

        private void WritePoint(Point point, Dimension dimension)
        {
            WriteDouble(point.X);
            WriteDouble(point.Y);

            if (dimension == Dimension.Xyz || dimension == Dimension.Xyzm)
                WriteDouble(point.Z);

            if (dimension == Dimension.Xym || dimension == Dimension.Xyzm)
                WriteDouble(point.M);
        }

        private void WriteDouble(double? value)
        {
            if (value.HasValue)
                wkbWriter.Write(value.Value);
            else
                wkbWriter.Write(doubleNaN);
        }

        private void WriteLineString(LineString lineString)
        {
            wkbWriter.Write(lineString.Points.Count);

            foreach (Point point in lineString.Points)
                WritePoint(point, lineString.Dimension);
        }

        private void WritePolygon(Polygon polygon)
        {
            if (polygon.IsEmpty)
            {
                wkbWriter.Write(0);
                return;
            }

            wkbWriter.Write(1 + polygon.InteriorRings.Count);

            wkbWriter.Write(polygon.ExteriorRing.Points.Count);
            foreach (Point point in polygon.ExteriorRing.Points)
                WritePoint(point, polygon.Dimension);

            foreach (LinearRing interiorRing in polygon.InteriorRings)
            {
                wkbWriter.Write(interiorRing.Points.Count);
                foreach (Point point in interiorRing.Points)
                    WritePoint(point, polygon.Dimension);
            }
        }

        private void WriteMultiPoint(MultiPoint multiPoint)
        {
            wkbWriter.Write(multiPoint.Geometries.Count);

            foreach (Point point in multiPoint.Geometries)
            {
                wkbWriter.Write((byte)0x69);
                WriteInternal(point, multiPoint);
            }
        }

        private void WriteMultiLineString(MultiLineString multiLineString)
        {
            wkbWriter.Write(multiLineString.Geometries.Count);

            foreach (LineString lineString in multiLineString.Geometries)
            {
                wkbWriter.Write((byte)0x69);
                WriteInternal(lineString, multiLineString);
            }
        }

        private void WriteMultiPolygon(MultiPolygon multiPolygon)
        {
            wkbWriter.Write(multiPolygon.Geometries.Count);

            foreach (Polygon polygon in multiPolygon.Geometries)
            {
                wkbWriter.Write((byte)0x69);
                WriteInternal(polygon, multiPolygon);
            }
        }

        private void WriteGeometryCollection(GeometryCollection geometryCollection)
        {
            wkbWriter.Write((uint)geometryCollection.Geometries.Count);

            foreach (Geometry geometry in geometryCollection.Geometries)
            {
                wkbWriter.Write((byte)0x69);
                WriteInternal(geometry);
            }
        }



        #endregion

    }
}
