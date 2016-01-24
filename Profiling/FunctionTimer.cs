/* 
 * Function timer used for profiling in Unity
 * 
 * Use Example : 
 * 
 *  Update()
 *  {
 *      FunctionTimer.START_FUNCTION_TIMER("Function1");
 *      Function1();
 *      FunctionTimer.STOP_FUNCTION_TIMER("Function1");
 *      
 *      FunctionTimer.START_FUNCTION_TIMER("Function2");
 *      Function2();
 *      FunctionTimer.STOP_FUNCTION_TIMER("Function2");
 *  }
 *  
 *  WheneverYouWantToPrint()
 *  {
 *      FunctionTimer.DISPLAY_FUNCTION_TIMER_AVERAGE("Function1");
 *      FunctionTimer.DISPLAY_FUNCTION_TIMER_AVERAGE("Function2");
 *      
 *      // and if you are done using these 
 *      FunctionTimer.RESET();
 *  }
 * 
 * 
 * https://github.com/tombbonin
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

public class FunctionTimer 
{
    public class TimedFunction
    {
        public double TimeSum;
        public uint   Count;
        public Stopwatch SWatch;
        public TimedFunction() 
        { 
            TimeSum = 0; 
            Count = 0;
            SWatch = new Stopwatch();
            SWatch.Start();
        }
    }

    private static Dictionary<string, TimedFunction> functionTimes = new Dictionary<string, TimedFunction>();

    public static void START_FUNCTION_TIMER(string functionName)
    {
        TimedFunction timedFunc;
        if (!functionTimes.TryGetValue(functionName, out timedFunc))
            functionTimes.Add(functionName, timedFunc = new TimedFunction());
        timedFunc.SWatch.Start();
    }

    public static void STOP_FUNCTION_TIMER(string functionName)
    {
        TimedFunction timedFunc;
        if (!functionTimes.TryGetValue(functionName, out timedFunc))
        {
            UnityEngine.Debug.LogError("FunctionTimer::StopFunctionTimer() -- Cannot stop timing this function, did you forget to start it?");
            return;
        }

        timedFunc.SWatch.Stop();
        functionTimes[functionName].TimeSum += timedFunc.SWatch.Elapsed.TotalMilliseconds;
        functionTimes[functionName].Count += 1;
        timedFunc.SWatch.Reset();
    }

    public static void DISPLAY_FUNCTION_TIMER_AVERAGE(string functionName)
    {
        if (!functionTimes.ContainsKey(functionName))
        {
            UnityEngine.Debug.LogError("FunctionTimer::DisplayFunctionTimeAvg() -- Unknown function Name!");
            return;
        }

        TimedFunction timedFunc = functionTimes[functionName];

        double result = (timedFunc.TimeSum / timedFunc.Count);
        UnityEngine.Debug.Log(functionName + "() - Average Time = " + string.Format("{0:0.##}", result) + "ms Over " + timedFunc.Count + " iterations.");        
    }

    public static void RESET()
    {
        functionTimes.Clear();
    }
}

