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
        private float _speed;
        private float _positionAlongCurve;
        private float _maxDrift;
        private float _maxBankAngle;
        private float _bankSmoothing;
        private float _prevBankAngle;
        private Vector3 _prevPosition;
        private Quaternion _prevRotation;

        public void StartMoving(float startTime, Curve curve, out Vector3 position, out Quaternion rotation, float speed, float maxDrift = 0f, float maxBank = 0f, float bankSmoothing = 0.001f)
        {
            Curve = curve;
            _speed = speed;
            _maxDrift = maxDrift;
            _maxBankAngle = maxBank;
            _bankSmoothing = bankSmoothing;
            _startTime = startTime;
            _positionAlongCurve = 0f;
            _prevPosition = curve.ControlPoints[0];
            _prevRotation = Quaternion.identity;
            MoveAlongCurve(startTime, out position, out rotation);
        }

        public void MoveAlongCurve(float time, out Vector3 position, out Quaternion rotation)
        {
            position = _prevPosition;
            rotation = _prevRotation;

            if (Curve == null)
            {
                Debug.LogError("No assigned Path to follow!");
                return;
            }

            _positionAlongCurve = _speed * (time - _startTime);
            var curveCompletion = _positionAlongCurve / Curve.Length;

            if (curveCompletion >= 1f)
            {
                Evt_ReachedEnd(_startTime + Curve.Length / _speed); // send up time at which missile hit
                return;
            }

            // Update pos
            Vector3 tangent;
            Vector3 curvature;
            position = Curve.Evaluate(curveCompletion, out tangent, out curvature);

            float bankAngle = 0f;
            if (_maxBankAngle > 0 && _bankSmoothing > 0f)
            {
                // Compute Bank
                var targetBankAngle = Curve.GetBankAngle(tangent, curvature, _maxBankAngle);
                bankAngle = Mathf.Lerp(_prevBankAngle, targetBankAngle, Time.deltaTime / _bankSmoothing);
                _prevBankAngle = bankAngle;
            }

            if (_maxDrift > 0)
            {
                // Compute Drift
                var futurePosOnCurveDist = _positionAlongCurve + (_speed * _maxDrift);
                futurePosOnCurveDist = Mathf.Clamp(futurePosOnCurveDist, 0f, Curve.Length);
                var futurePosOnCurve = Curve.Evaluate(futurePosOnCurveDist / Curve.Length, out tangent, out curvature);

                Debug.DrawLine(position, futurePosOnCurve);
                rotation = Quaternion.LookRotation((futurePosOnCurve - position).normalized);
            }
            else if (tangent.normalized != Vector3.zero)
            {
                rotation = Quaternion.LookRotation(tangent.normalized);
            }

            if (bankAngle != 0)
                rotation *= Quaternion.AngleAxis(bankAngle, Vector3.forward);

            _prevPosition = position;
            _prevRotation = rotation;
        }

        public void MoveAlongCurve(float time, Transform view)
        {
            Vector3 pos;
            Quaternion rot;
            MoveAlongCurve(time, out pos, out rot);
            view.position = pos;
            view.rotation = rot;
        }
    }
}