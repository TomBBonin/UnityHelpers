/* 
 * Curve tool for Unity to easily visualize smooth curves and make actors follow them.  
 * If you want to be able to see the curves in the Unity Scene, call Draw()
 * from a monobehaviour class's OnDrawGizmos(). 
 * 
 * https://github.com/tombbonin
 */

using System;
using UnityEngine;

namespace Curves
{
    public class CurveFollower
    {
        public event Action<float> Evt_ReachedEnd = delegate { };
        public Curve Curve;
        private float _startTime;
        private float _positionAlongCurve;
        private float _maxDrift;
        private float _maxBankAngle;
        private float _bankSmoothing;
        private float _prevBankAngle;
        private Vector3 _prevPosition;
        private Quaternion _prevRotation;
        private float _prevTime;

        public void StartMoving(Curve curve, float startTime, out Vector3 position, out Quaternion rotation, float maxDrift = 0f, float maxBank = 0f, float bankSmoothing = 0.001f)
        {
            Curve = curve;
            _maxDrift = maxDrift;
            _maxBankAngle = maxBank;
            _bankSmoothing = bankSmoothing;
            _positionAlongCurve = 0f;
            _prevPosition = curve.ControlPoints[0];
            _prevRotation = Quaternion.identity;
            _startTime = startTime;
            _prevTime = startTime;
            MoveAlongCurve(_startTime, 0f, out position, out rotation);
        }

        public void MoveAlongCurve(float time, float speed, out Vector3 position, out Quaternion rotation)
        {
            position = _prevPosition;
            rotation = _prevRotation;

            if (Curve == null)
            {
                Debug.LogError("No assigned Path to follow!");
                return;
            }

            _positionAlongCurve += speed * (time - _prevTime); // alows for variable speed but involves deltas (cant be determined at a time with a single call)
            var linearTime = GetLinearTimeOnCurve(_positionAlongCurve);
            if (linearTime >= 1f)
            {
                Evt_ReachedEnd(_prevTime + (_positionAlongCurve - Curve.Length) / speed); // send up time at which end was reached
                return;
            }

            // Update pos
            Vector3 tangent;
            Vector3 curvature;
            position = Curve.Evaluate(linearTime, out tangent, out curvature);

            float bankAngle = 0f;
            if (_maxBankAngle > 0 && _bankSmoothing > 0f)
            {
                // Compute Bank
                var targetBankAngle = Curve.GetBankAngle(tangent, curvature, _maxBankAngle);
                bankAngle = Mathf.Lerp(_prevBankAngle, targetBankAngle, Time.deltaTime / _bankSmoothing);
                _prevBankAngle = bankAngle;
            }

            if (_maxDrift > 0 && speed > 0)
            {
                // Compute Drift
                var futurePosOnCurveDist = _positionAlongCurve + (speed * _maxDrift);
                futurePosOnCurveDist = Mathf.Clamp(futurePosOnCurveDist, 0f, Curve.Length);
                var futurePosOnCurve = Curve.Evaluate(GetLinearTimeOnCurve(futurePosOnCurveDist), out tangent, out curvature);

                Debug.DrawLine(position, futurePosOnCurve);
                rotation = Quaternion.LookRotation((futurePosOnCurve - position).normalized);
            }
            else if (tangent.normalized != Vector3.zero)
            {
                rotation = Quaternion.LookRotation(tangent.normalized);
            }

            if (bankAngle != 0)
                rotation *= Quaternion.AngleAxis(bankAngle, Vector3.forward);

            Debug.Log(Vector3.Distance(_prevPosition, position));
            _prevPosition = position;
            _prevRotation = rotation;
            _prevTime = time;
        }

        public void MoveAlongCurve(float time, float speed, Transform view)
        {  
            // wrapper for transform
            Vector3 pos;
            Quaternion rot;
            MoveAlongCurve(time, speed, out pos, out rot);
            view.position = pos;
            view.rotation = rot;
        }

        private float GetLinearTimeOnCurve(float posOnCurve)
        {
            // this is necessary because the points on the curve are not evenly sampled, if they were we could just use dist / curveLength
            var linearTime = 0f;
            var points = Curve.GetCurvePoints();
            var step = 1f / (points.Length - 1);
            for (var i = 0; i < points.Length - 1; i++)
            {
                var point = points[i];
                if (posOnCurve > point.DistanceOnCurve)
                {
                    var interpolation = Mathf.InverseLerp(point.DistanceOnCurve, points[i + 1].DistanceOnCurve, posOnCurve);
                    linearTime = Mathf.Lerp(step * i, step * (i + 1), interpolation);
                }
            }
            return linearTime;
        }
    }
}