using System;
using System.Text;                  
using System.Collections.Generic;   // For KeyValuePair can use var instead
using Crestron.SimplSharp;          // For Basic SIMPL# Classes
using Crestron.SimplSharp.Scheduler;// Inside reference SimplSharpTimerEventInterface of course...


namespace CTI
{
    public delegate void RelayEventHandler(int i); 

    public class BuiltInSchedulerExample
    {

        private ScheduledEventGroup myGroup;
        private ScheduledEvent[] myEvent = new ScheduledEvent[3];
        
        public event RelayEventHandler RelayEvent;
        
        public BuiltInSchedulerExample()
        {
            myGroup = new ScheduledEventGroup("Mike");
        }

        public void Clear()
        {
            myGroup.ClearAllEvents();
        }
        public void RetrieveEvents()
        {
            myGroup.RetrieveAllEvents();
            foreach (KeyValuePair<string, ScheduledEvent> kvp in myGroup.ScheduledEvents)
                CrestronConsole.PrintLine("Event Read: {0}, {1}", kvp.Value.Name, kvp.Value.DateAndTime.Hour + ":" + kvp.Value.DateAndTime.Minute);
        }
        public void InitializeEvents()
        {
            myGroup.RetrieveAllEvents();
            if(myGroup.ScheduledEvents.ContainsKey("Relay 1") == false)
            {
                myEvent[1] = new ScheduledEvent("Relay 1", myGroup);
                myEvent[1].Description = "Relay 1";
                myEvent[1].DateAndTime.SetRelativeEventTime(0, 5);
                myEvent[1].Acknowledgeable = true;
                myEvent[1].Persistent = true;
                myEvent[1].AcknowledgeExpirationTimeout.Hour = 10;
                myEvent[1].UserCallBack += new ScheduledEvent.UserEventCallBack(Scheduler_UserCallBack);
                myEvent[1].Enable();
                CrestronConsole.PrintLine("Event 1 Created {0} {1}:{2}", myEvent[1].Name, myEvent[1].DateAndTime.Hour, myEvent[1].DateAndTime.Minute);
            }
            if (myGroup.ScheduledEvents.ContainsKey("Relay 2") == false)
            {
                myEvent[2] = new ScheduledEvent("Relay 2", myGroup);
                myEvent[2].DateAndTime.SetRelativeEventTime(0, 7);
                myEvent[2].Description = "Relay 2";
                myEvent[2].Acknowledgeable = true;
                myEvent[2].Persistent = false;
                myEvent[2].UserCallBack += new ScheduledEvent.UserEventCallBack(Scheduler_UserCallBack);
                myEvent[2].Enable();
                CrestronConsole.PrintLine("Event 2 Created: {0} {1}:{2}", myEvent[2].Name, myEvent[2].DateAndTime.Hour, myEvent[2].DateAndTime.Minute);
            }
        }
        public void Ack(int i)
        {
            if (myEvent[i] != null)
                myEvent[i].Acknowledge();
        }
        void Scheduler_UserCallBack(ScheduledEvent SchEvent, ScheduledEventCommon.eCallbackReason type)
        {
            if (SchEvent.Name == "Relay 1")
            {
                CrestronConsole.PrintLine("{0} == Relay 1 Clicked", DateTime.Now.ToString());
                RelayEvent(1);
            }
            if (SchEvent.Name == "Relay 2")
            {
                CrestronConsole.PrintLine("{0} == Relay 2 Clicked", DateTime.Now.ToString());
                CrestronConsole.PrintLine("Snooze Result: {0}",SchEvent.Snooze(2).ToString());
                RelayEvent(2);
            }
        }
    }
}