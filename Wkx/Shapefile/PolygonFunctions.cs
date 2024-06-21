using System;
using System.Collections.Generic;
using System.Linq;

namespace Wkx
{
    //internal partial class ShapefileReader
    //{
    //}

    public static class PolygonFunctions
    {

        private static double SignedArea(List<Point> coords, bool fast = false)
        {
            // Return the signed area enclosed by a ring using the linear time algorithm.
            // A value >= 0 indicates a counter-clockwise oriented ring.
            // A faster version is possible by setting 'fast' to True, which returns 2x the area,
            // e.g. if you're only interested in the sign of the area.

            var xs = coords.Select(coord => coord.X).ToList();
            var ys = coords.Select(coord => coord.Y).ToList();

            xs.Add(xs[1]);
            ys.Add(ys[1]);

            double area2 = 0;
            for (int i = 1; i < coords.Count; i++)
            {
                area2 += xs[i].Value * (ys[i + 1].Value - ys[i - 1].Value);
            }

            if (fast)
            {
                return area2;
            }
            else
            {
                return area2 / 2.0;
            }
        }

        public static bool IsCw(List<Point> coords)
        {
            // Returns True if a polygon ring has clockwise orientation, determined by a negatively signed area.
            double area2 = SignedArea(coords, fast: true);
            return area2 < 0;
        }



        public static List<Point> Rewind(List<Point> coords)
        {
            coords.Reverse();
            return coords;
        }

        private static BoundingBox RingBbox(List<Point> coords)
        {
            var xs = new List<double>();
            var ys = new List<double>();
            for (int i = 0; i < coords.Count; i++)
            {
                xs.Add(coords[i].X.Value);
                ys.Add(coords[i].Y.Value);
            }
            var minX = xs.Min();
            var minY = ys.Min();
            var maxX = xs.Max();
            var maxY = ys.Max();
            return new BoundingBox(minX, minY, maxX, maxY);
        }

        public static bool BboxOverlap(BoundingBox bbox1, BoundingBox bbox2)
        {
            var xMin1 = bbox1.XMin;
            var yMin1 = bbox1.YMin;
            var xMax1 = bbox1.XMax;
            var yMax1 = bbox1.YMax;
            var xMin2 = bbox2.XMin;
            var yMin2 = bbox2.YMin;
            var xMax2 = bbox2.XMax;
            var yMax2 = bbox2.YMax;
            return xMin1 <= xMax2 && xMax1 >= xMin2 && yMin1 <= yMax2 && yMax1 >= yMin2;
        }

        public static bool BboxContains(BoundingBox bbox1, BoundingBox bbox2)
        {
            var xMin1 = bbox1.XMin;
            var yMin1 = bbox1.YMin;
            var xMax1 = bbox1.XMax;
            var yMax1 = bbox1.YMax;
            var xMin2 = bbox2.XMin;
            var yMin2 = bbox2.YMin;
            var xMax2 = bbox2.XMax;
            var yMax2 = bbox2.YMax;
            return xMin1 < xMin2 && xMax1 > xMax2 && yMin1 < yMin2 && yMax1 > yMax2;
        }

        public static bool RingContainsPoint(List<Point> coords, Point p)
        {
            double tx = p.X.Value;
            double ty = p.Y.Value;

            // get initial test bit for above/below X axis
            var vtx0 = coords[0];
            bool yflag0 = (vtx0.Y >= ty);

            bool inside_flag = false;
            for (int i = 1; i < coords.Count; i++)
            {
                var vtx1 = coords[i];
                bool yflag1 = (vtx1.Y >= ty);
                // check if endpoints straddle (are on opposite sides) of X axis
                // (i.e. the Y's differ); if so, +X ray could intersect this edge.
                if (yflag0 != yflag1)
                {
                    bool xflag0 = (vtx0.X >= tx);
                    // check if endpoints are on same side of the Y axis (i.e. X's
                    // are the same); if so, it's easy to test if edge hits or misses.
                    if (xflag0 == (vtx1.X >= tx))
                    {
                        // if edge's X values both right of the point, must hit
                        if (xflag0)
                        {
                            inside_flag = !inside_flag;
                        }
                    }
                    else
                    {
                        // compute intersection of pgon segment with +X ray, note
                        // if >= point's X; if so, the ray hits it.
                        if ((vtx1.X - (vtx1.Y - ty) * (vtx0.X - vtx1.X) / (vtx0.Y - vtx1.Y)) >= tx)
                        {
                            inside_flag = !inside_flag;
                        }
                    }
                }

                // move to next pair of vertices, retaining info as possible
                yflag0 = yflag1;
                vtx0 = vtx1;
            }
                
            return inside_flag;
        }


        private static Point RingSample(List<Point> coords, bool ccw = false)
        {
            // Return a sample point guaranteed to be within a ring, by efficiently
            // finding the first centroid of a coordinate triplet whose orientation
            // matches the orientation of the ring and passes the point-in-ring test.
            // The orientation of the ring is assumed to be clockwise, unless ccw
            // (counter-clockwise) is set to True.
                
            var triplet = new List<Point>();

            IEnumerable<Point> IterCoords()
            {
                // iterate full closed ring
                foreach (var p in coords)
                {
                    yield return p;
                }

                // finally, yield the second coordinate to the end to allow checking the last triplet
                yield return coords[1];
            }

            foreach (var p in IterCoords())
            {
                // add point to triplet (but not if duplicate)
                if (!triplet.Contains(p))
                {
                    triplet.Add(p);
                }

                // new triplet, try to get sample
                if (triplet.Count == 3)
                {
                    // check that triplet does not form a straight line (not a triangle)
                    var isStraightLine = (triplet[0].Y - triplet[1].Y) * (triplet[0].X - triplet[2].X) == (triplet[0].Y - triplet[2].Y) * (triplet[0].X - triplet[1].X);
                    if (!isStraightLine)
                    {
                        // get triplet orientation
                        var closedTriplet = triplet.Concat(new List<Point> { triplet[0] }).ToList();
                        var tripletCcw = !IsCw(closedTriplet);
                            
                        // check that triplet has the same orientation as the ring (means triangle is inside the ring)
                        if (ccw == tripletCcw)
                        {
                            // get triplet centroid
                            var xs = triplet.Select(t => t.X).ToList();
                            var ys = triplet.Select(t => t.Y).ToList();
                            var xMean = xs.Sum() / 3.0;
                            var yMean = ys.Sum() / 3.0;
                            var pt = new Point(xMean.Value, yMean.Value);
                            // check that triplet centroid is truly inside the ring
                            if (RingContainsPoint(coords, (pt)))
                            {
                                return (pt);
                            }
                        }
                    }

                    // failed to get sample point from this triplet
                    // remove oldest triplet coord to allow iterating to next triplet
                    triplet.RemoveAt(0);
                }
            }

            throw new Exception("Unexpected error: Unable to find a ring sample point.");
        }

        public static bool RingContainsRing(List<Point> coords1, List<Point> coords2)
        {
            // Returns True if all vertexes in coords2 are fully inside coords1.
            return coords2.All(p2 => RingContainsPoint(coords1, p2));
        }

        public static Geometry OrganizePolygonRings(List<LinearRing> rings, Dictionary<string, int> returnErrors = null)
        {
            // first iterate rings and classify as exterior or hole
            var exteriors = new List<List<Point>>();
            var holes = new List<List<Point>>();
            foreach (var ring in rings)
            {
                var pts = ring.Points;
                // shapefile format defines a polygon as a sequence of rings
                // where exterior rings are clockwise, and holes counterclockwise
                if (IsCw(pts))
                {
                    // ring is exterior
                    exteriors.Add(pts);
                }
                else
                {
                    // ring is a hole
                    holes.Add(pts);
                }
            }

            // if only one exterior, then all holes belong to that exterior
            if (exteriors.Count == 1)
            {
                // exit early
                return new Polygon(new LinearRing(exteriors[0]), holes.Select(h => new LinearRing(h)).ToList()) ;
            }

            // multiple exteriors, ie multi-polygon, have to group holes with correct exterior
            // shapefile format does not specify which holes belong to which exteriors
            // so have to do efficient multi-stage checking of hole-to-exterior containment
            else if (exteriors.Count > 1)
            {
                    var polys = new List<Polygon>();
                // exit early if no holes
                if (holes.Count == 0)
                {
                    foreach (var ext in exteriors)
                    {
                        polys.Add(new Polygon(ext));
                    }
                    //polys = poly;
                    return new MultiPolygon(polys);
                }

                // first determine each hole's candidate exteriors based on simple bbox contains test
                Dictionary<int, List<int>> holeExteriors = new Dictionary<int, List<int>>();
                var exteriorBboxes = new List<BoundingBox>();
                foreach (var ring in exteriors)
                {
                    exteriorBboxes.Add(RingBbox(ring));
                }
                for (int hole_i = 0; hole_i < holes.Count; hole_i++)
                {
                    var holeBbox = RingBbox(holes[hole_i]);
                    for (int ext_i = 0; ext_i < exteriorBboxes.Count; ext_i++)
                    {
                        if (BboxContains(exteriorBboxes[ext_i], holeBbox))
                        {
                            if (!holeExteriors.ContainsKey(hole_i))
                            {
                                holeExteriors[hole_i] = new List<int>();
                            }
                            holeExteriors[hole_i].Add(ext_i);
                        }
                    }
                }

                // then, for holes with still more than one possible exterior, do more detailed hole-in-ring test
                foreach (KeyValuePair<int, List<int>> holeExterior in holeExteriors.ToList())
                {
                    if (holeExterior.Value.Count > 1)
                    {
                        // get hole sample point
                        bool ccw = !IsCw(holes[holeExterior.Key]);
                        var holeSample = RingSample(holes[holeExterior.Key], ccw: ccw);
                        // collect new exterior candidates
                        List<int> newExteriorCandidates = new List<int>();
                        foreach (int ext_i in holeExterior.Value)
                        {
                            // check that hole sample point is inside exterior
                            bool holeInExterior = RingContainsPoint(exteriors[ext_i], holeSample);
                            if (holeInExterior)
                            {
                                newExteriorCandidates.Add(ext_i);
                            }
                        }

                        // set new exterior candidates
                        holeExteriors[holeExterior.Key] = newExteriorCandidates;
                    }
                }

                // if still holes with more than one possible exterior, means we have an exterior hole nested inside another exterior's hole
                foreach (KeyValuePair<int, List<int>> holeExterior in holeExteriors.ToList())
                {
                    if (holeExterior.Value.Count > 1)
                    {
                        // exterior candidate with the smallest area is the hole's most immediate parent
                        int ext_i = holeExterior.Value.OrderBy(x => Math.Abs(SignedArea(exteriors[x], fast: true))).First();
                        holeExteriors[holeExterior.Key] = new List<int> { ext_i };
                    }
                }

                // separate out holes that are orphaned (not contained by any exterior)
                List<int> orphanHoles = new List<int>();
                foreach (KeyValuePair<int, List<int>> holeExterior in holeExteriors.ToList())
                {
                    if (holeExterior.Value.Count == 0)
                    {
                        orphanHoles.Add(holeExterior.Key);
                        holeExteriors.Remove(holeExterior.Key);
                    }
                }

                // each hole should now only belong to one exterior, group into exterior-holes polygons
                //var polys = new List<Polygon>();
                for (int ext_i = 0; ext_i < exteriors.Count; ext_i++)
                {
                    var poly = new List<List<Point>>();
                    // find relevant holes
                    var polyHoles = new List<LinearRing>();
                    foreach (KeyValuePair<int, List<int>> holeExterior in holeExteriors.ToList())
                    {
                        // hole is relevant if previously matched with this exterior
                        if (holeExterior.Value[0] == ext_i)
                        {
                            polyHoles.Add(new LinearRing(holes[holeExterior.Key]));
                        }
                    }
                    polys.Add(new Polygon(new LinearRing(exteriors[ext_i]), polyHoles));
                }

                // add orphan holes as exteriors
                foreach (int hole_i in orphanHoles)
                {
                    var ext = holes[hole_i];
                    // add as single exterior without any holes
                    //var poly = new List<List<Point>>();
                    //poly.Add(ext);
                    polys.Add(new Polygon(new LinearRing(Rewind(ext))));
                }

                if (orphanHoles.Count > 0 && returnErrors != null)
                {
                    returnErrors["polygon_orphaned_holes"] = orphanHoles.Count;
                }

                return new MultiPolygon(polys);
            }

            // no exteriors, be nice and assume due to incorrect winding order
            else
            {
                if (returnErrors != null)
                {
                    returnErrors["polygon_only_holes"] = holes.Count;
                }
                exteriors = holes;
                // add as single exterior without any holes
                if (exteriors.Count > 1)
                {
                    return new Polygon(Rewind(exteriors[0]));
                }
                else
                {
                    var polys = new List<Polygon>();
                    foreach (var ext in exteriors)
                    {
                        //var poly = new List<List<Point>>();
                        polys.Add(new Polygon(Rewind(ext)));
                        
                    }
                    return new MultiPolygon(polys);
                }
            }
        }

    }








}

