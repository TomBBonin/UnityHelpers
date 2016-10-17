/* 
 * Curve tool for Unity to easily visualize smooth curves
 * and make actors follow them. Large parts of this script (specifically Evaluate functions) were inspired by
 * Nick Hall's work on splines that can be found at https://github.com/nickhall/Unity-Procedural.
 * 
 * If you want to be able to see the curves in the Unity Scene, call Draw()
 * from a MonoBehaviour class's OnDrawGizmos(). 
 * 
 * https://github.com/tombbonin
 */

using UnityEngine;
using System.Collections.Generic;

namespace Curves
{
    public class CatmullRom : Curve
    {
        public bool CloseLoop;
        private float[] CPDists;

        public CatmullRom(int resolution, Vector3[] controlPoints, bool closeLoop = false) : base(resolution, controlPoints)
        {
            CloseLoop = closeLoop;
            MeasureCurve();
        }

        public override void MeasureCurve()
        {
            var pointsOnCurve = GetCurvePoints();
            var nbPoints = CloseLoop ? ControlPoints.Length : ControlPoints.Length - 1;
            CPDists = new float[nbPoints + 1];
            var idx = 0;
            for (var i = 0; i < pointsOnCurve.Length; i += Resolution)
            {
                CPDists[idx] = pointsOnCurve[i].DistanceOnCurve;
                idx++;
            }
            Length = pointsOnCurve[pointsOnCurve.Length - 1].DistanceOnCurve;
        }

        private static Vector3 Evaluate(Vector3 p0, Vector3 p1, Vector3 tanP0, Vector3 tanP2, float t)
        {
            // Catmull-Rom splines are Hermite curves with special tangent values.
            // Hermite curve formula:
            // (2t^3 - 3t^2 + 1) * p0 + (t^3 - 2t^2 + t) * m0 + (-2t^3 + 3t^2) * p1 + (t^3 - t^2) * m1
            // For points p0 and p1 passing through points m0 and m1 interpolated over t = [0, 1]
            // Tangent M[k] = (P[k+1] - P[k-1]) / 2
            // With [] indicating subscript
            Vector3 position = (2.0f * t * t * t - 3.0f * t * t + 1.0f) * p0
                             + (t * t * t - 2.0f * t * t + t) * tanP0
                             + (-2.0f * t * t * t + 3.0f * t * t) * p1
                             + (t * t * t - t * t) * tanP2;
            return position;
        }

        private static Vector3 Evaluate(Vector3 p0, Vector3 p1, Vector3 tanP0, Vector3 tanP1, float t, out Vector3 tangent)
        {
            // Calculate tangents
            // p'(t) = (6t² - 6t)p0 + (3t² - 4t + 1)m0 + (-6t² + 6t)p1 + (3t² - 2t)m1
            tangent = (6 * t * t - 6 * t) * p0
                   + (3 * t * t - 4 * t + 1) * tanP0
                   + (-6 * t * t + 6 * t) * p1
                   + (3 * t * t - 2 * t) * tanP1;
            return Evaluate(p0, p1, tanP0, tanP1, t);
        }

        private static Vector3 Evaluate(Vector3 p0, Vector3 p1, Vector3 tanP0, Vector3 tanP1, float t, out Vector3 tangent, out Vector3 curvature)
        {
            // Calculate second derivative (curvature)
            // p''(t) = (12t - 6)p0 + (6t - 4)m0 + (-12t + 6)p1 + (6t - 2)m1
            curvature = (12 * t - 6) * p0
                     + (6 * t - 4) * tanP0
                     + (-12 * t + 6) * p1
                     + (6 * t - 2) * tanP1;
            return Evaluate(p0, p1, tanP0, tanP1, t, out tangent);
        }

        public override Vector3 Evaluate(float t, out Vector3 tangent, out Vector3 curvature)
        {
            var posOnCurve = t * Length;
            var i = 0;
            while (i < CPDists.Length - 1 && posOnCurve >= CPDists[i])
                i++;
            i--;

            var p0Dist = CPDists[i];
            var p1Dist = CPDists[i + 1];
            var localT = (posOnCurve - p0Dist) / (p1Dist - p0Dist);

            var p0 = ControlPoints[i];
            var p1 = ControlPoints[GetClampedPointIdx(i + 1)];
            var m0 = 0.5f * (p1 - ControlPoints[GetClampedPointIdx(i - 1)]);
            var m1 = 0.5f * (ControlPoints[GetClampedPointIdx(i + 2)] - p0);

            return Evaluate(p0, p1, m0, m1, localT, out tangent, out curvature);
        }

        private int GetClampedPointIdx(int pointIdx)
        {
            //Clamp the list positions to allow looping
            //start over again when reaching the end or beginning
            if (pointIdx < 0)
                return CloseLoop ? ControlPoints.Length + pointIdx : 0;
            if (pointIdx >= ControlPoints.Length)
                return CloseLoop ? pointIdx % ControlPoints.Length : ControlPoints.Length - 1;
            return pointIdx;
        }

        public static Vector3[] GetCurvePositions(int resolution, Vector3[] controlPoints, bool closeLoop = false)
        {
            var catmullRom = new CatmullRom(resolution, controlPoints, closeLoop);
            return GetCurvePositions(catmullRom, resolution, controlPoints);
        }

        public override CurvePoint[] GetCurvePoints()
        {
            // First for loop goes through each control point, the second subdivides the path between CPs based on resolution
            var distanceOnCurve = 0f;
            var prevPos = new CurvePoint { Position = ControlPoints[0] };
            // If we are looping, we are adding an extra segment, so we need an extra point
            var nbPoints = CloseLoop ? ControlPoints.Length : ControlPoints.Length - 1;
            var positions = new List<CurvePoint>(nbPoints * Resolution + 1);
            Vector3 p0 = Vector3.zero, p1 = Vector3.zero, m0 = Vector3.zero, m1 = Vector3.zero;
            for (var i = 0; i < nbPoints; i++)
            {
                p0 = ControlPoints[i];
                p1 = ControlPoints[GetClampedPointIdx(i + 1)];
                m0 = 0.5f * (p1 - ControlPoints[GetClampedPointIdx(i - 1)]);
                m1 = 0.5f * (ControlPoints[GetClampedPointIdx(i + 2)] - p0);
                // Second for loop actually creates the spline for this particular segment
                for (var j = 0; j < Resolution; j++)
                    AddPointOnCurve(ref positions, p0, p1, m0, m1, (float)j / Resolution, ref distanceOnCurve, ref prevPos);
            }
            // we have to manually add the last point on the spline
            AddPointOnCurve(ref positions, p0, p1, m0, m1, 1f, ref distanceOnCurve, ref prevPos);

            var posArray = positions.ToArray();
            FixNormals(ref posArray);
            return posArray;
        }

        private static void AddPointOnCurve(ref List<CurvePoint> positions, Vector3 p0, Vector3 p1, Vector3 m0, Vector3 m1, float t, ref float distanceOnCurve, ref CurvePoint prevPos)
        {
            var posOnCurve = new CurvePoint();
            posOnCurve.Position = Evaluate(p0, p1, m0, m1, t, out posOnCurve.Tangent, out posOnCurve.Curvature);
            posOnCurve.Bank = GetBankAngle(posOnCurve.Tangent, posOnCurve.Curvature, MaxBankAngle);

            // Currently breaks if 3 consecutive points are colinear, to be improved with second pass on curve
            posOnCurve.Normal = Vector3.Cross(posOnCurve.Curvature, posOnCurve.Tangent).normalized;
            if (Vector3.Dot(posOnCurve.Normal, prevPos.Normal) < 0)
                posOnCurve.Normal *= -1;

            distanceOnCurve += Vector3.Distance(posOnCurve.Position, prevPos.Position);
            posOnCurve.DistanceOnCurve = distanceOnCurve;
            positions.Add(posOnCurve);
            prevPos = posOnCurve;
        }

        public static CurvePoint[] GetCurvePoints(int resolution, Vector3[] controlPoints, bool closeLoop = false)
        {
            var catmullRom = new CatmullRom(resolution, controlPoints, closeLoop);
            return catmullRom.GetCurvePoints();
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

            var pointsOnCurve = GetCurvePoints();
            var prevPos = pointsOnCurve[0];
            foreach (var curvePos in pointsOnCurve)
            {
                Gizmos.color = SplineColor;
                Gizmos.DrawLine(prevPos.Position, curvePos.Position);
                Gizmos.color = TangentColor;
                Gizmos.DrawRay(curvePos.Position, curvePos.Tangent.normalized);
                Gizmos.color = CurvatureColor;
                Gizmos.DrawRay(curvePos.Position, curvePos.Curvature.normalized);
                Gizmos.color = NormalColor;
                Gizmos.DrawRay(curvePos.Position, curvePos.Normal);
                Gizmos.color = BankColor;
                Gizmos.DrawRay(curvePos.Position, Quaternion.AngleAxis(curvePos.Bank, curvePos.Tangent) * Vector3.up);
                prevPos = curvePos;
            }
        }
    }
}