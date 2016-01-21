using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

public class FunctionTimer 
{
    private static List<System.TimeSpan> _timeForFunction;
    private static Stopwatch _stopwatch;
    private static string _timed_Function_Name;

    public static void INIT_FUNCTION_TIMER()
    {
        _timeForFunction = new List<System.TimeSpan>();
        _stopwatch = new Stopwatch();
    }

    public static void START_FUNCTION_TIMER(string functionName)
    {
        _timed_Function_Name = functionName;
        _stopwatch.Start();
    }

    public static void STOP_FUNCTION_TIMER()
    {
        _stopwatch.Stop();
        _timeForFunction.Add(_stopwatch.Elapsed);
        _stopwatch.Reset();
    }

    public static void DISPLAY_FUNCTION_TIMER_AVERAGE()
    {
        if (_timeForFunction.Count == 0)
        {
            UnityEngine.Debug.Log("No Timed Function");
            return;
        }

        System.TimeSpan avgTime = System.TimeSpan.Zero;
        for (int i = 0; i < _timeForFunction.Count; i++)
        {
            avgTime += _timeForFunction[i];
        }
        double result = (avgTime.TotalMilliseconds / _timeForFunction.Count);
        UnityEngine.Debug.Log(_timed_Function_Name + "() - Average Time = " + string.Format("{0:0.##}", result) + "ms Over " + _timeForFunction.Count + " iterations.");

        _timeForFunction.Clear();
        _timed_Function_Name = "";
        _stopwatch.Reset();
    }
}

