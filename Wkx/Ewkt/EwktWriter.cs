namespace Wkx
{
    internal class EwktWriter : WktWriter
    {
        internal override string Write(Geometry geometry, bool skipType = false)
        {
            return string.Concat("SRID=", geometry.Srid ?? 0, ";", base.Write(geometry, skipType));
        }

        protected override void WriteWktType(GeometryType geometryType, Dimension dimension, bool isEmpty, bool skipType = false)
        {
            if (!skipType)
            {
                wktBuilder.Append(geometryType.ToString().ToUpperInvariant());

                if (dimension == Dimension.Xym)
                    wktBuilder.Append("M");
            }

            if (isEmpty)
                wktBuilder.Append(" EMPTY");
        }
    }
}