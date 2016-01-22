/* 
 * Example of how to setup an event system in Unity,
 * using your own Event enum and object to pass information
 * You can see an example in the GridBuilder Script. The Grid
 * has an event system setup to propagate tile clicks and hovers.
 * 
 * https://github.com/tombbonin
 */ 

using UnityEngine;
using System.Collections;

// Your Event information. In the HexGrid project i also
// pass the tile that triggered the event
public class MyEventInfo : System.EventArgs
{
    public MyEvents Event { get; set; }
}

// Your Event enum, I use it for mouse clicks for example, MouseL, MouseR, MouseM, etc
public enum MyEvents
{
    Event_1,
    Event_2,
    Event_3,
    Event_4,
    Event_5
}

// Add the following to the class you wish to use as an event dispatcher, and to which
// others will subscribe to catch the events
public class EventSystem_Emitter : MonoBehaviour 
{
    public event System.EventHandler<MyEventInfo> Event_1;
    public event System.EventHandler<MyEventInfo> Event_2;
    public event System.EventHandler<MyEventInfo> Event_3;
    public event System.EventHandler<MyEventInfo> Event_4;
    public event System.EventHandler<MyEventInfo> Event_5;

    public void DispatchEvent(MyEvents gridEvent)
    {
        System.EventHandler<MyEventInfo> eventToDispatch = null;
        switch (gridEvent)
        {
            case MyEvents.Event_1: { eventToDispatch = Event_1; break; }
            case MyEvents.Event_2: { eventToDispatch = Event_2; break; }
            case MyEvents.Event_3: { eventToDispatch = Event_3; break; }
            case MyEvents.Event_4: { eventToDispatch = Event_4; break; }
            case MyEvents.Event_5: { eventToDispatch = Event_5; break; }
        }

        if (eventToDispatch == null) return;

        var eventInfo = new MyEventInfo() { Event = gridEvent };
        eventToDispatch(this, eventInfo);
    }

    // when you want to fire an event just call Dispatch event with the right enum and it will propagate to all 
    // classes who have subscribed to Event_1. See below for an example of how to do that
    void Event_1_WasTriggered()
    {
        DispatchEvent(MyEvents.Event_1);
    }
}

public class EventSystem_Listenner : MonoBehaviour
{
    EventSystem_Emitter _emitter;

    // To suscribe to an event there is a shortened syntax as follows. You pass
    // it the name of the function you want to call when the event is triggered
    EventSystem_Listenner()
    {
        _emitter.Event_1 += HandleEvents123;    // Subscribe to Event1
        _emitter.Event_2 += HandleEvents123;    // Subscribe to Event2
        _emitter.Event_3 += HandleEvents123;    // Subscribe to Event3
        _emitter.Event_4 += HandleEvent4;       // Subscribe to Event4
        _emitter.Event_5 += HandleEvent5;       // Subscribe to Event5
    }

    void HandleEvents123(object sender, MyEventInfo eventInfo)
    {
        switch (eventInfo.Event)
        {
            case MyEvents.Event_1: { break; }
            case MyEvents.Event_2: { break; }
            case MyEvents.Event_3: { break; }
        }
    }
    void HandleEvent4(object sender, MyEventInfo eventInfo)
    {

    }
    void HandleEvent5(object sender, MyEventInfo eventInfo)
    {

    }
}
