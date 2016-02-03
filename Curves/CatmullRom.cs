/* 
 * Curve tool for Unity to easily visualize smooth curves
 * and make actors follow them. A lot of the math was found online, 
 * specifically the static Evaluate functions. If i come across 
 * the original post i'll make sure to give due credit here.
 * 
 * If you want to be able to see the curves in Unity, call OnDrawGizmos in
 * from a monobehaviour class. 
 * If you want to be able to create the curves in editor, change the controlPoints to 
 * Transform and use empty game objects as points.
 * 
 * https://github.com/tombbonin
 */ 

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CatmullRom //: MonoBehaviour //if you want the OnDrawGizmos to be called automatically
{
    public Vector3[]    ControlPoints; // Transform[] for in Editor curve editing, you'll have to change code in the evaluate funcs
	public bool 		CloseLoop;
	public int 			SplineResolution;
	public Color 		SplineColor;
	public Color 		TangentColor;
	public Color 		NormalColor;
	public Color 		CurvatureColor;
	public Color 		BankColor;
	public float        MaxBankAngle = 45f;
    public float[]      CPDists;
    public float        Length { get { return CPDists[CPDists.Length-1]; } }

    public void MeasureCurve()
    {
        List<CatmullRomCurvePosition> positionsOnCurve = GetAllCurveSubDivisionPositions();
        int nbPoints = CloseLoop ? ControlPoints.Length : ControlPoints.Length - 1;
        CPDists = new float[nbPoints + 1];
        int idx = 0;
        for (int i = 0; i < positionsOnCurve.Count; i+= SplineResolution)
        {
            CPDists[idx] = positionsOnCurve[i].DistanceOnCurve;
            idx++;
        }
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
				 		+ 		  (t * t * t - 2.0f * t * t + t)    * tanP0
				 		+ (-2.0f * t * t * t + 3.0f * t * t) 	    * p1
						+ 		  (t * t * t -        t * t) 	    * tanP2;
		return position;
	}

    private static Vector3 Evaluate(Vector3 p0, Vector3 p1, Vector3 tanP0, Vector3 tanP1, float t, out Vector3 tangent)
	{
		// Calculate tangents
		// p'(t) = (6t² - 6t)p0 + (3t² - 4t + 1)m0 + (-6t² + 6t)p1 + (3t² - 2t)m1
		tangent = (6 * t * t - 6 * t) 	  * p0
			   +  (3 * t * t - 4 * t + 1) * tanP0
			   + (-6 * t * t + 6 * t) 	  * p1
			   +  (3 * t * t - 2 * t)     * tanP1;
		return Evaluate(p0, p1, tanP0, tanP1, t);
	}

    private static Vector3 Evaluate(Vector3 p0, Vector3 p1, Vector3 tanP0, Vector3 tanP1, float t, out Vector3 tangent, out Vector3 curvature)
	{
		// Calculate second derivative (curvature)
		// p''(t) = (12t - 6)p0 + (6t - 4)m0 + (-12t + 6)p1 + (6t - 2)m1
		curvature = (12 * t - 6) * p0
			     +  (6  * t - 4) * tanP0
				 + (-12 * t + 6) * p1
				 +  (6  * t - 2) * tanP1;
		return Evaluate(p0, p1, tanP0, tanP1, t, out tangent);
	}

	public Vector3 Evaluate(float posOnCurve, out Vector3 tangent, out Vector3 curvature) 
	{
		int i = 0;
		while(i < CPDists.Length -1 && posOnCurve >= CPDists[i])
			i++;
		i--;

		float p0Dist = CPDists [i];
		float p1Dist = CPDists [i+1];
		float localT = (posOnCurve - p0Dist) / (p1Dist - p0Dist);

		Vector3 p0 = 		      ControlPoints[i];
		Vector3 p1 = 			  ControlPoints[GetClampedPointIdx(i + 1)];
        Vector3 m0 = 0.5f * (p1 - ControlPoints[GetClampedPointIdx(i - 1)]);
		Vector3 m1 = 0.5f * (     ControlPoints[GetClampedPointIdx(i + 2)] - p0);

		return Evaluate(p0, p1, m0, m1, localT, out tangent, out curvature);
	}
		
	public void OnDrawGizmos()
	{
        if (ControlPoints == null)
            return;

        foreach (Vector3 point in ControlPoints)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(point, 2f);
        }

        List<CatmullRomCurvePosition> positionsOnCurve = GetAllCurveSubDivisionPositions();
        CatmullRomCurvePosition prevPos = positionsOnCurve[0];
        foreach(CatmullRomCurvePosition curvePos in positionsOnCurve)
        {
            Gizmos.color = SplineColor;
            Gizmos.DrawLine(prevPos.Pos, curvePos.Pos);
            Gizmos.color = TangentColor;
            Gizmos.DrawRay(curvePos.Pos, curvePos.Tangent.normalized * 20f);
            Gizmos.color = CurvatureColor;
            Gizmos.DrawRay(curvePos.Pos, curvePos.Curvature.normalized * 20f);
            Gizmos.color = NormalColor;
            Gizmos.DrawRay(curvePos.Pos,curvePos.Normal * 5f);

            float angle = GetBankAngle(curvePos.Tangent, curvePos.Curvature, MaxBankAngle);
            var upwards = Quaternion.AngleAxis(angle, curvePos.Tangent) * Vector3.up;
            Gizmos.color = BankColor;
            Gizmos.DrawRay(curvePos.Pos, upwards * 20f);

            prevPos = curvePos;
        }
    }

	public static float GetBankAngle(Vector3 tangent, Vector3 curvature, float maxBank)
	{
		float bank = Mathf.Clamp01((1 - Vector3.Dot(tangent.normalized, curvature.normalized)) * (curvature.magnitude / tangent.magnitude) * 0.3f);
		float angle = Mathf.Lerp (0, maxBank, bank);
		if (Vector3.Dot(Vector3.Cross(Vector3.up, curvature), tangent) < 0)
			angle *= -1;
		return angle;
	}
	
    private int GetClampedPointIdx(int pointIdx)  
	{
        //Clamp the list positions to allow looping
        //start over again when reaching the end or beginning
        if (pointIdx < 0)
			return CloseLoop ? ControlPoints.Length + pointIdx: 0;
        if (pointIdx >= ControlPoints.Length)
            return CloseLoop ? pointIdx % ControlPoints.Length : ControlPoints.Length - 1;
        return pointIdx;
	}

    public List<CatmullRomCurvePosition> GetAllCurveSubDivisionPositions(/*bool rebuild*/)
    {
        /* By adding a private List<CatmullRomCurvePosition> _positionsOnCurve;
         * variable to this Catmull Rom, we can prevent recomputing the entire curve, if it isnt needed.
         * When drawing gizmos for example. If the points or resolution change though, this must be rebuilt
         * depending on your project, think about it!
         *if(_positionsOnCurve != null && !rebuild)
         *    return _positionsOnCurve;
         */

        // First for loop goes through each control point, the second subdivides the path between CPs based on resolution
        float distanceOnCurve = 0f;
        CatmullRomCurvePosition prevPos = new CatmullRomCurvePosition();
        prevPos.Pos = ControlPoints[0];
        // If we are looping, we are adding an extra segment, so we need an extra point
        int nbPoints = CloseLoop ? ControlPoints.Length : ControlPoints.Length - 1;
        List<CatmullRomCurvePosition> positions = new List<CatmullRomCurvePosition>(nbPoints * SplineResolution + 1);
        Vector3 p0 = Vector3.zero, p1 = Vector3.zero, m0 = Vector3.zero, m1 = Vector3.zero;
        for (int i = 0; i < nbPoints; i++)
        {
            p0 =              ControlPoints[i];
            p1 =              ControlPoints[GetClampedPointIdx(i + 1)];
            m0 = 0.5f * (p1 - ControlPoints[GetClampedPointIdx(i - 1)]);
            m1 = 0.5f * (     ControlPoints[GetClampedPointIdx(i + 2)] - p0);
            // Second for loop actually creates the spline for this particular segment
            for (int j = 0; j < SplineResolution; j++)
                AddPosOnCurve(ref positions, p0, p1, m0, m1, (float)j / SplineResolution, ref distanceOnCurve, ref prevPos);
        }
        // we have to manually add the last point on the spline
        AddPosOnCurve(ref positions, p0, p1, m0, m1, 1f, ref distanceOnCurve, ref prevPos);

        /* return _positionsOnCurve = positions; */ // If we are storing these positions, they need to be assigned and returned
        return positions;
    }

    private void AddPosOnCurve(ref List<CatmullRomCurvePosition> positions, Vector3 p0, Vector3 p1, Vector3 m0, Vector3 m1, float t, ref float distanceOnCurve, ref CatmullRomCurvePosition prevPos)
    {
        CatmullRomCurvePosition posOnCurve = new CatmullRomCurvePosition();
        posOnCurve.Pos = CatmullRom.Evaluate(p0, p1, m0, m1, t, out posOnCurve.Tangent, out posOnCurve.Curvature);
        posOnCurve.Bank = GetBankAngle(posOnCurve.Tangent, posOnCurve.Curvature, MaxBankAngle);

        // Hack to try to avoid borked look rotations when this dot == 1, taking any ideas! This seems to improve
        // but not 100% at all
        float dot = Vector3.Dot(posOnCurve.Tangent.normalized, Vector3.up);
        if(dot >= 0.98 || dot <= -0.98)
            posOnCurve.Normal = Vector3.Cross(posOnCurve.Tangent, Vector3.forward).normalized;
        else
            posOnCurve.Normal = Vector3.Cross(posOnCurve.Tangent, Vector3.up).normalized;

        distanceOnCurve += Vector3.Distance(posOnCurve.Pos, prevPos.Pos);
        posOnCurve.DistanceOnCurve = distanceOnCurve;
        positions.Add(posOnCurve);
        prevPos = posOnCurve;
    }
}

public class CatmullRomCurvePosition
{
    public Vector3 Pos;
    public Vector3 Tangent;
    public Vector3 Curvature;
    public Vector3 Normal;
    public float   Bank;
    public float   DistanceOnCurve;
}

public class CatmullRomFollower
{
    protected CatmullRom _path;
    protected float _startTime;
    protected float _speed;
    protected float _positionAlongCurve;
    protected float _maxDrift;
    protected float _maxBankAngle;
    protected float _bankSmoothing;
    protected float _prevBankAngle;

    protected Transform _transform;

    public void MoveAlongCurve()
    {
        if (_path == null)
        {
            Debug.LogError("No assigned Path to follow!");
            return;
        }

        _positionAlongCurve = _speed * (Time.time - _startTime);
        float raceCompletion = _positionAlongCurve / _path.Length;

        if (raceCompletion > 1f)
        {
            StopMoving();
            return;
        }

        // Update pos
        Vector3 tangent;
        Vector3 curvature;
        _transform.position = _path.Evaluate(_positionAlongCurve, out tangent, out curvature);

        float bankAngle = 0f;
        if (_maxBankAngle > 0)
        {
            // Compute Bank
            float targetBankAngle = CatmullRom.GetBankAngle(tangent, curvature, _maxBankAngle);
            bankAngle = Mathf.Lerp(_prevBankAngle, targetBankAngle, Time.deltaTime / _bankSmoothing);
            _prevBankAngle = bankAngle;
        }

        if (_maxDrift > 0)
        {
            // Compute Drift
            float futurePosOnCurveDist = _positionAlongCurve + (_speed * _maxDrift);
            futurePosOnCurveDist = Mathf.Clamp(futurePosOnCurveDist, 0f, _path.Length);
            Vector3 futurePosOnCurve = _path.Evaluate(futurePosOnCurveDist, out tangent, out curvature);

            Debug.DrawLine(_transform.position, futurePosOnCurve);
            _transform.forward = (futurePosOnCurve - _transform.position).normalized;
        }
        else
        {
            _transform.forward = tangent.normalized;
        }

        if (bankAngle != 0)
            _transform.Rotate(Vector3.forward, bankAngle);
    }

    public void StartMoving(Transform transform, CatmullRom path, float speed, float maxDrift = 0f, float maxBank = 0f, float bankSmoothing = 0f, float timeOffset = 0f)
    {
        _transform = transform;
        _path = path;
        _speed = speed;
        _maxDrift = maxDrift;
        _maxBankAngle = maxBank;
        _bankSmoothing = bankSmoothing;

        _startTime = Time.time - timeOffset;
        _positionAlongCurve = 0f;
        MoveAlongCurve();
    }

    public void StopMoving()
    {
        _path = null;
        _speed = 0;
        _positionAlongCurve = 0f;
        _startTime = 0;
    }
}