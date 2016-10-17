/* 
 * Curve tool for Unity to easily visualize smooth curves and make actors follow them.  
 * If you want to be able to see the curves in the Unity Scene, call Draw()
 * from a monobehaviour class's OnDrawGizmos(). 
 * 
 * https://github.com/tombbonin
 */

using System.Collections.Generic;
using UnityEngine;

namespace Curves
{
    public class Bezier : Curve
    {
        private enum Type { Linear, Cubic, Quadratic }
        private readonly Type _curveType;

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
            // First for loop goes through each control point, the second subdivides the path between CPs based on resolution
            var distanceOnCurve = 0f;
            var prevPos = new CurvePoint { Position = ControlPoints[0] };
            var nbPositions = ControlPoints.Length * Resolution;
            var positions = new CurvePoint[nbPositions + 1];
            var step = 1f / nbPositions;
            for (var i = 0; i <= nbPositions; i++)
            {
                var t = i * step;
                var posOnCurve = new CurvePoint();
                posOnCurve.Position = Evaluate(t, out posOnCurve.Tangent, out posOnCurve.Curvature);
                posOnCurve.Bank = GetBankAngle(posOnCurve.Tangent, posOnCurve.Curvature, MaxBankAngle);

                // Currently breaks if 3 consecutive points are colinear, to be improved with second pass on curve
                posOnCurve.Normal = Vector3.Cross(posOnCurve.Curvature, posOnCurve.Tangent).normalized;
                if (Vector3.Dot(posOnCurve.Normal, prevPos.Normal) < 0)
                    posOnCurve.Normal *= -1;

                distanceOnCurve += Vector3.Distance(posOnCurve.Position, prevPos.Position);
                posOnCurve.DistanceOnCurve = distanceOnCurve;
                positions[i] = posOnCurve;
                prevPos = posOnCurve;
            }
            FixNormals(ref positions);
            return positions;
        }

        public override void Draw()
        {
            // call in OnDrawGizmos
            if (ControlPoints == null)
                return;

            foreach (var point in ControlPoints)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(point, 0.5f);
            }

            Gizmos.color = SplineColor;
            var prevPos = ControlPoints[0];
            var currPos = Vector3.zero;
            var tangent = Vector3.zero;
            var prevNormal = Vector3.zero;
            var normal = Vector3.zero;
            var curvature = Vector3.zero;
            for (int i = 1; i <= Resolution; i++)
            {
                float t = 1f / Resolution * i;
                currPos = Evaluate(t, out tangent, out curvature);
                normal = Vector3.Cross(curvature, tangent).normalized;
                if (Vector3.Dot(normal, prevNormal) < 0)
                    normal *= -1;

                float bankAngle = GetBankAngle(tangent, curvature, MaxBankAngle);

                Gizmos.color = SplineColor;
                Gizmos.DrawLine(prevPos, currPos);
                Gizmos.color = TangentColor;
                Gizmos.DrawRay(currPos, tangent.normalized);
                Gizmos.color = CurvatureColor;
                Gizmos.DrawRay(currPos, curvature.normalized);
                Gizmos.color = NormalColor;
                Gizmos.DrawRay(currPos, normal);
                Gizmos.color = BankColor;
                Gizmos.DrawRay(currPos, Quaternion.AngleAxis(bankAngle, tangent) * Vector3.up);
                prevPos = currPos;
                prevNormal = normal;
            }
        }
    }
}
