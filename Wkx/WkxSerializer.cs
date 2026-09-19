using System;
using System.Collections.Generic;
using System.Text;

namespace Wkx
{

    /// <summary>
    /// Static convenience methods for serializing to WKX Geometry objects and deserializing from WKX Geometry objects <br/> 
    /// To and from WKT, EWKT, WKB, EWKB, Spatialite, and Shapefile formats. <br/>
    /// </summary>
    public static class WkxSerializer
    {

        #region To Geometry	

        /// <summary>
        /// Create a WKX Geometry from a WKT string <br/>
        /// An Exception will be thrown if the WKT string is not valid WKT. <br/>
        /// </summary>
        /// <param name="wkt"></param>
        /// <param name="srid"></param>
        /// <returns>WKX <see cref="Geometry"/></returns>
        public static Geometry FromWkt(this string wkt, int srid = 0) => (Geometry)FromString(wkt, "wkt", srid);
        /// <summary>
        /// Create a WKX Geometry from a EWKT string. <br/>
        /// An Exception will be thrown if the EWKT string is not valid EWKT. <br/>
        /// </summary>
        /// <param name="ewkt"></param>
        /// <returns>WKX <see cref="Geometry"/></returns>
        public static Geometry FromEwkt(this string ewkt) => FromString(ewkt, "ewkt");
        /// <summary>
        /// Create a WKX Geometry from a WKB byte array. Optionally specify the SRID to assign to the geometry. <br/>
        /// An Exception will be thrown if the WKB array is not valid WKB. <br/>
        /// </summary>
        /// <param name="wkb"></param>
        /// <param name="srid"></param>
        /// <returns>WKX <see cref="Geometry"/></returns>
        public static Geometry FromWkb(this byte[] wkb, int srid = 0) => FromBinary(wkb, "wkb");
        /// <summary>
        /// Create a WKX Geometry from a EWKB byte array. <br/>
        /// An Exception will be thrown if the EWKB array is not valid EWKB. <br/>
        /// </summary>
        /// <param name="ewkb"></param>
        /// <returns>WKX <see cref="Geometry"/></returns>
        public static Geometry FromEwkb(this byte[] ewkb) => FromBinary(ewkb, "ewkb");
        /// <summary>
        /// Create a WKX Geometry from a Spatialite byte array. <br/>
        /// An Exception will be thrown if the Spatialite array is not valid Spatialite. <br/>
        /// </summary>
        /// <param name="spatialite"></param>
        /// <returns>WKX <see cref="Geometry"/></returns>
        public static Geometry FromSpatialite(this byte[] spatialite) => FromBinary(spatialite, "spb");
        /// <summary>
        /// Create a WKX Geometry from a Shapefile byte array. Optionally specify the SRID to assign to the geometry. <br/>
        /// Shapefile geometries are a binary format extracts of individual geometries from Shapefile.shp files. <br/>
        /// An Exception will be thrown if the Shapefile array is not valid Shapefile. <br/>
        /// </summary>
        /// <param name="shp"></param>
        /// <param name="srid"></param>
        /// <returns>WKX <see cref="Geometry"/></returns>
        public static Geometry FromShapefile(this byte[] shp, int srid = 0) => FromBinary(shp, "shp", srid);

        /// <summary>
        /// Deserialize from string formats to a WKX Geometry using the specified serializer. <br/>
        /// </summary>
        /// <param name="wkt"></param>
        /// <param name="serializer"></param>
        /// <param name="srid"></param>
        /// <returns>WKX <see cref="Geometry"/></returns>
        private static Geometry FromString(this string wkt, string serializer, int srid = 0)
        {
            Geometry geom = null;
            try
            {
                switch (serializer)
                {

                    case "wkt":
                        geom = Geometry.Deserialize<WktSerializer>(wkt);
                        geom.Srid = srid;
                        return geom;
                    case "ewkt":
                        geom = Geometry.Deserialize<EwktSerializer>(wkt);
                        return geom;
                }

                return geom;
            }
            catch (ArgumentException ex)
            {
                var msg = $"{ex.Message}. Input may be corrupt or not valid {serializer.ToUpper()}";
                return default;
            }
        }

        /// <summary>
        /// Deserialize from binary formats to a WKX Geometry using the specified serializer. <br/>
        /// </summary>
        /// <param name="wkb"></param>
        /// <param name="serializer"></param>
        /// <param name="srid"></param>
        /// <returns>WKX <see cref="Geometry"/></returns>
        private static Geometry FromBinary(this byte[] wkb, string serializer, int srid = 0)
        {
            Geometry geom = null;
            try
            {
                switch (serializer)
                {

                    case "wkb":
                        geom = Geometry.Deserialize<WkbSerializer>(wkb);
                        geom.Srid = srid;
                        return geom;
                    case "ewkb":
                        geom = Geometry.Deserialize<EwkbSerializer>(wkb);
                        return geom;
                    case "spb":
                        geom = Geometry.Deserialize<SpatialiteSerializer>(wkb);
                        return geom;
                    case "shp":
                        geom = Geometry.Deserialize<ShapefileSerializer>(wkb);
                        //geom.Srid = srid;
                        return geom;
                }

                return geom;
            }
            catch (ArgumentException ex)
            {
                var msg = $"{ex.Message}. Input may be corrupt or invalid geometry";
                return default;
            }
        }

        #endregion


        #region From Geometry

        /// <summary>
        /// Serialize a WKX Geometry to a WKT string. SRID is ignored <br/>
        /// </summary>
        /// <param name="geom"></param>
        /// <returns>WKT string</returns>
        public static string ToWkt(this Geometry geom) => ToWkString(geom, "wkt");
         /// <summary>
        /// Serialize a WKX Geometry to a EWKT string. <br/>
        /// </summary>
        /// <param name="geom"></param>
        /// <returns>EWKT string</returns>
        public static string ToEwkt(this Geometry geom) => ToWkString(geom, "ewkt");
        /// <summary>
        /// Serialize a WKX Geometry to a WKB byte array. SRID is ignored <br/>
        /// </summary>
        /// <param name="geom"></param>
        /// <returns>WKB byte array</returns>
        public static byte[] ToWkb(this Geometry geom) => ToWkBinary(geom, "wkb");
        /// <summary>
        /// Serialize a WKX Geometry to a EWKB byte array. <br/>
        /// </summary>
        /// <param name="geom"></param>
        /// <returns>EWKB byte array</returns>
        public static byte[] ToEwkb(this Geometry geom) => ToWkBinary(geom, "ewkb");
        /// <summary>
        /// Serialize a WKX Geometry to a Spatialite byte array. <br/>
        /// </summary>
        /// <param name="geom"></param>
        /// <returns>Spatialite byte array</returns>
        public static byte[] ToSpatialite(this Geometry geom) => ToWkBinary(geom, "spb");
        /// <summary>
        /// Serialize a WKX Geometry to a Shapefile byte array. SRID is ignored <br/>
        /// </summary>
        /// <param name="geom"></param>
        /// <returns>Shapefile byte array</returns>
        public static byte[] ToShapefile(this Geometry geom) => ToWkBinary(geom, "shp");


        /// <summary>
        /// Serialize a WKX Geometry to a WKT or EWKT string using the specified serializer. <br/>
        /// </summary>
        /// <param name="geom"></param>
        /// <param name="serializer"></param>
        /// <returns>Well Known Text Geometry</returns>
        private static string ToWkString(this Geometry geom, string serializer)
        {
            try
            {
                switch (serializer)
                {
                    case "wkt":
                        return geom.SerializeString<WktSerializer>();
                    case "ewkt":
                        return geom.SerializeString<EwktSerializer>();
                }
                return string.Empty;
            }
            catch (ArgumentException ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Serialize a WKX Geometry to a WKB, EWKB, Spatialite, or Shapefile byte array using the specified serializer. <br/>
        /// </summary>
        /// <param name="geom"></param>
        /// <param name="serializer"></param>
        /// <returns>Well Known Binary Geometry</returns>
        public static byte[] ToWkBinary(this Geometry geom, string serializer)
        {
            try
            {
                switch (serializer)
                {
                    case "wkb":
                        return geom.SerializeByteArray<WkbSerializer>();
                    case "ewkt":
                        return geom.SerializeByteArray<EwkbSerializer>();
                    case "spb":
                        return geom.SerializeByteArray<SpatialiteSerializer>();
                    case "shp":
                        return geom.SerializeByteArray<ShapefileSerializer>();
                }
                return null;
            }
            catch (ArgumentException ex)
            {
                throw;
            }
        }

        #endregion




    }
}
