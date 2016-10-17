/* 
 * Base class used to build smooth curves in Unity and easily visualize them aswell as
 * make actors follow them.
 * 
 * https://github.com/tombbonin
 */

using UnityEngine;

namespace Curves
{
    public class CurvePoint
    {
        public Vector3 Position;
        public Vector3 Tangent;
        public Vector3 Curvature;
        public Vector3 Normal;
        public float Bank;
        public float DistanceOnCurve;
    }

    public abstract class Curve
    {
        public Vector3[] ControlPoints;
        public int Resolution;
        public float Length;

        public Color SplineColor;
        public Color NormalColor;
        public Color TangentColor;
        public Color CurvatureColor;
        public Color BankColor;
        public static readonly float MaxBankAngle = 45f;

        protected Curve(int resolution, Vector3[] controlPoints)
        {
            ControlPoints = controlPoints;
            Resolution = resolution;

            SplineColor = Color.blue;
            NormalColor = Color.red;
            TangentColor = Color.cyan;
            CurvatureColor = Color.green;
            BankColor = Color.magenta;
        }

        public abstract void MeasureCurve();

        public abstract Vector3 Evaluate(float t, out Vector3 tangent, out Vector3 curvature);

        public abstract CurvePoint[] GetCurvePoints();

        public void Draw()
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

        public static float GetBankAngle(Vector3 tangent, Vector3 curvature, float maxBank)
        {
            float bank =
                Mathf.Clamp01((1 - Vector3.Dot(tangent.normalized, curvature.normalized))*
                              (curvature.magnitude/tangent.magnitude)*0.3f);
            float angle = Mathf.Lerp(0, maxBank, bank);
            if (Vector3.Dot(Vector3.Cross(Vector3.up, curvature), tangent) < 0)
                angle *= -1;
            return angle;
        }

        protected static Vector3[] GetCurvePositions(Curve curve, int resolution, Vector3[] controlPoints)
        {
            var pointsOnCurve = new Vector3[resolution + 1];
            pointsOnCurve[0] = controlPoints[0];
            var tangent = Vector3.zero;
            var curvature = Vector3.zero;
            for (var j = 1; j <= resolution; j++)
            {
                var t = 1f/resolution*j;
                pointsOnCurve[j] = curve.Evaluate(t, out tangent, out curvature);
            }
            return pointsOnCurve;
        }

        protected static void FixNormals(ref CurvePoint[] curvePoints)
        {
            for (var i = 0; i < curvePoints.Length; i++)
            {
                var point = curvePoints[i];
                if (point.Normal == Vector3.zero)
                {
                     // look forward until we find a non zero normal
                    int j = i;
                    while (j < curvePoints.Length)
                    {
                        if (curvePoints[j].Normal != Vector3.zero)
                            break;
                        j++;
                    }

                    if (j < curvePoints.Length)
                    {
                        while (i < j)
                        {
                            curvePoints[i].Normal = curvePoints[j].Normal;
                            i++;
                        }
                    }
                }
            }
        }
    }
}