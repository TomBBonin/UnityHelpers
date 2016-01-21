using UnityEngine;
using System.Collections;

public class MyMaths 
{
    public static float GetSquaredDistance(Vector2 pointA, Vector2 pointB)
    {
        return (((pointA.x - pointB.x) * (pointA.x - pointB.x)) + ((pointA.y - pointB.y) * (pointA.y - pointB.y)));
    }

    public static float GetEuclideanDistance(Vector2 pointA, Vector2 pointB)
    {
        return (Mathf.Abs(pointA.x - pointB.x) + Mathf.Abs(pointA.y - pointB.y));
    }

    public static bool DoSegmentsIntersect(Vector2 segOneA, Vector2 segOneB, Vector2 segTwoA, Vector2 segTwoB)
    {
        Vector2 a = segOneB - segOneA;
        Vector2 b = segTwoA - segTwoB;
        Vector2 c = segOneA - segTwoA;

        float alphaNumerator = b.y * c.x - b.x * c.y;
        float alphaDenominator = a.y * b.x - a.x * b.y;
        float betaNumerator = a.x * c.y - a.y * c.x;
        float betaDenominator = a.y * b.x - a.x * b.y;

        bool doIntersect = true;

        if (alphaDenominator == 0 || betaDenominator == 0)
            doIntersect = false;
        else
        {
            if (alphaDenominator > 0)
            {
                if (alphaNumerator < 0 || alphaNumerator > alphaDenominator)
                    doIntersect = false;
            }
            else if (alphaNumerator > 0 || alphaNumerator < alphaDenominator)
                doIntersect = false;

            if (doIntersect && betaDenominator > 0)
            {
                if (betaNumerator < 0 || betaNumerator > betaDenominator)
                    doIntersect = false;
            }
            else if (betaNumerator > 0 || betaNumerator < betaDenominator)
                doIntersect = false;
        }
        return doIntersect;
    }

    public static Vector2 GetSegmentNormalTowardsPos(Vector2 segA, Vector2 segB, Vector2 pos)
    {
        //if we define dx=x2-x1 and dy=y2-y1, then the normals are (-dy, dx) and (dy, -dx).
        float dx = segB.x - segA.x;
        float dy = segB.y - segA.y;

        Vector2 normalA = new Vector2(-dy, dx);
        Vector2 normalB = new Vector2(dy, -dx);

        Vector2 posA = segA + normalA;
        Vector2 posB = segA + normalB;

        if (GetSquaredDistance(pos, posA) < GetSquaredDistance(pos, posB))
            return normalA.normalized;
        else
            return normalB.normalized;
    }
    
    public static Vector2 GetSegmentCenter(Vector2 segA, Vector2 segB)
    {
        return ((segB - segA) / 2) + segA;
    }

    public static bool IsPowerOfTwo(ulong x)
    {
        return (x & (x - 1)) == 0;
    }

    public static bool IsPowerOfTwo(int x)
    {
        if (x <= 0) return false;
        return IsPowerOfTwo((ulong)x);
    }

    public static bool ABCAreAligned2D(Vector2 pointA, Vector2 pointB, Vector2 pointC, float epsilon)
    {
        float crossproduct = (pointC.y - pointA.y) * (pointB.x - pointA.x) - (pointC.x - pointA.x) * (pointB.y - pointA.y);
        return (Mathf.Abs(crossproduct) < epsilon);
    }

    public static bool IsPointCBetweenAB2D(Vector2 pointA, Vector2 pointB, Vector2 pointC, float epsilon)
    {
        if (!ABCAreAligned2D(pointA, pointB, pointC, epsilon)) return false;

        float dotproduct = (pointC.x - pointA.x) * (pointB.x - pointA.x) + (pointC.y - pointA.y) * (pointB.y - pointA.y);
        if (dotproduct < 0) return false;

        float squaredlengthba = (pointB.x - pointA.x) * (pointB.x - pointA.x) + (pointB.y - pointA.y) * (pointB.y - pointA.y);
        if (dotproduct > squaredlengthba) return false;

        return true;
    }
}
