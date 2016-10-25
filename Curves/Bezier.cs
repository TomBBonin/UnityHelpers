/* 
 * Curve tool for Unity to easily visualize smooth curves and make actors follow them.  
 * If you want to be able to see the curves in the Unity Scene, call Draw()
 * from a monobehaviour class's OnDrawGizmos(). 
 * 
 * https://github.com/tombbonin
 */

using UnityEngine;

namespace Curves
{
    public class Bezier : Curve
    {
        private enum Type { Linear, Cubic, Quadratic }
        private readonly Type _curveType;
        private CurvePoint[] _curvePoints;

        public Bezier(int resolution, Vector3[] controlPoints) : base(resolution, controlPoints)
        {
            _curveType = DeduceType(controlPoints.Length);
            MeasureCurve();
        }

        private static Type DeduceType(int nbControlPoints)
        {
            switch (nbControlPoints)
            {
                case 2: return Type.Linear;
                case 3: return Type.Quadratic;
                case 4: return Type.Cubic;
                default: Debug.LogError("Unsupported amount of Control points"); return Type.Cubic;
            }
        }

        public override void MeasureCurve()
        {
            Vector3 tangent;
            Vector3 curvature;

            var prevPos = ControlPoints[0];
            var currPos = Vector3.zero;
            for (int i = 0; i <= Resolution; i++)
            {
                float t = 1f / Resolution * i;
                currPos = Evaluate(t, out tangent, out curvature);
                Length += Vector3.Distance(prevPos, currPos);
                prevPos = currPos;
            }
        }

        protected static Vector3 Evaluate_Linear(float t, Vector3 p0, Vector3 p1)
        {
            var pos = p0 + t * (p1 - p0);
            return pos;
        }

        protected static Vector3 Evaluate_Quadratic(float t, Vector3 p0, Vector3 p1, Vector3 p2)
        {
            var u = 1f - t;
            var uu = u * u;
            var tt = t * t;

            Vector3 position = uu * p0 + (2f * u * t * p1) + (tt * p2);
            return position;
        }

        protected static Vector3 Evaluate_Quadratic(float t, Vector3 p0, Vector3 p1, Vector3 p2, out Vector3 tangent)
        {
            var u = 1f - t;

            tangent = 2f * u * (p1 - p0) + 2f * t * (p2 - p1);
            return Evaluate_Quadratic(t, p0, p1, p2);
        }

        protected static Vector3 Evaluate_Quadratic(float t, Vector3 p0, Vector3 p1, Vector3 p2, out Vector3 tangent, out Vector3 curvature)
        {
            curvature = 2f * (p2 - 2f * p1 + p0);
            return Evaluate_Quadratic(t, p0, p1, p2, out tangent);
        }

        protected static Vector3 Evaluate_Cubic(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            var u = 1f - t;
            var uu = u * u;
            var uuu = u * u * u;
            var tt = t * t;
            var ttt = t * t * t;

            Vector3 position = uuu * p0 + 3f * uu * t * p1 + 3f * u * tt * p2 + ttt * p3;
            return position;
        }

        protected static Vector3 Evaluate_Cubic(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, out Vector3 tangent)
        {
            var u = 1f - t;
            var uu = u * u;
            var tt = t * t;

            tangent = 3f * uu * (p1 - p0) + 6f * u * t * (p2 - p1) + 3f * tt * (p3 - p2);
            return Evaluate_Cubic(t, p0, p1, p2, p3);
        }

        protected static Vector3 Evaluate_Cubic(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, out Vector3 tangent, out Vector3 curvature)
        {
            float u = 1f - t;

            curvature = 6f * u * (p2 - 2f * p1 + p0) + 6f * t * (p3 - 2f * p2 + p1);
            return Evaluate_Cubic(t, p0, p1, p2, p3, out tangent);
        }

        public override Vector3 Evaluate(float t, out Vector3 tangent, out Vector3 curvature)
        {
            var p0 = ControlPoints[0];
            var p1 = ControlPoints[1];

            var position = Vector3.zero;
            tangent = Vector3.zero;
            curvature = Vector3.zero;

            switch (_curveType)
            {
                case Type.Linear: position = Evaluate_Linear(t, p0, p1); break;
                case Type.Quadratic:
                    {
                        var p2 = ControlPoints[2];
                        position = Evaluate_Quadratic(t, p0, p1, p2, out tangent, out curvature);
                        break;
                    }
                case Type.Cubic:
                    {
                        var p2 = ControlPoints[2];
                        var p3 = ControlPoints[3];
                        position = Evaluate_Cubic(t, p0, p1, p2, p3, out tangent, out curvature);
                        break;
                    }
            }
            return position;
        }

        public static Vector3[] GetCurvePositions(int resolution, Vector3[] controlPoints)
        {
            var bezier = new Bezier(resolution, controlPoints);
            return GetCurvePositions(bezier, resolution, controlPoints);
        }

        public override CurvePoint[] GetCurvePoints()
        {
            // avoids rebuilding entire curve on every call, should be cleared if we wanted to rebuild the curve (if control points move for example)
            if(_curvePoints != null)
                return _curvePoints;

            // First for loop goes through each control point, the second subdivides the path between CPs based on resolution
            var distanceOnCurve = 0f;
            var prevPoint = new CurvePoint { Position = ControlPoints[0] };
            var nbPoints = ControlPoints.Length * Resolution;
            var points = new CurvePoint[nbPoints];
            var step = 1f / (nbPoints - 1);
            for (var i = 0; i < nbPoints; i++)
            {
                var t = i * step;
                var point = new CurvePoint();
                point.Position = Evaluate(t, out point.Tangent, out point.Curvature);
                point.Bank = GetBankAngle(point.Tangent, point.Curvature, MaxBankAngle);

                // Currently breaks if 3 consecutive points are colinear, to be improved with second pass on curve
                point.Normal = Vector3.Cross(point.Curvature, point.Tangent).normalized;
                if (Vector3.Dot(point.Normal, prevPoint.Normal) < 0)
                    point.Normal *= -1;

                distanceOnCurve += Vector3.Distance(point.Position, prevPoint.Position);
                point.DistanceOnCurve = distanceOnCurve;
                points[i] = point;
                prevPoint = point;
            }
            FixNormals(ref points);
            _curvePoints = points;
            return points;
        }

        public static CurvePoint[] GetCurvePoints(int resolution, Vector3[] controlPoints)
        {
            var curve = new Bezier(resolution, controlPoints);
            return curve.GetCurvePoints();
        }
    } 
}
