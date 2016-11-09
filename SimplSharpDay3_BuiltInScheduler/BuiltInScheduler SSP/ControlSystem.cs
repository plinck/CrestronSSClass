using System;
using System.Text;
using Crestron.SimplSharp;                          	// For Basic SIMPL# Classes
using Crestron.SimplSharpPro;                       	// For Basic SIMPL#Pro classes
using Crestron.SimplSharpPro.CrestronThread;        	// For Threading
using Crestron.SimplSharpPro.Diagnostics;		    	// For System Monitor Access
using Crestron.SimplSharpPro.DeviceSupport;             // For Generic Device Support
using Crestron.SimplSharp.Scheduler;         	        // Inside reference SimplSharpTimerEventInterface of course...
using System.Collections.Generic;                       // For KeyValuePair can use var instead

//using CTI;                                            // Namespace from our Simpl# Library  - removed due to some in class not making seperate library.

namespace BuiltInScheduler_SSP
{
    public class ControlSystem : CrestronControlSystem
    {
        // Define local variables ...
        public BuiltInSchedulerExample myScheduler;
        public Relay[] myRelays;
        public ControlSystem()
            : base()
        {
            try
            {
                Thread.MaxNumberOfUserThreads = 20;

                //Subscribe to the controller events (System, Program, and Ethernet)
                CrestronEnvironment.SystemEventHandler += new SystemEventHandler(ControlSystem_ControllerSystemEventHandler);
                CrestronEnvironment.ProgramStatusEventHandler += new ProgramStatusEventHandler(ControlSystem_ControllerProgramEventHandler);
                CrestronEnvironment.EthernetEventHandler += new EthernetEventHandler(ControlSystem_ControllerEthernetEventHandler);

                CrestronConsole.AddNewConsoleCommand(ClearEvents, "ClearMyEvents", "Clears all user created scheduler events", ConsoleAccessLevelEnum.AccessOperator);
                CrestronConsole.AddNewConsoleCommand(AckEvent, "AckEvent", "Acknowledge event by using 1 or 2", ConsoleAccessLevelEnum.AccessOperator);
                CrestronConsole.AddNewConsoleCommand(InitializeEvents, "InitEvents", "Setup the events for our event group", ConsoleAccessLevelEnum.AccessOperator);

                // Create a new BuiltInSchedulerExample object
                myScheduler = new BuiltInSchedulerExample();
                // Retrieve the events
                myScheduler.RetrieveEvents();
                // Register the event handler for the relays
                myScheduler.RelayEvent += new CTI.RelayEventHandler(myScheduler_RelayEvent);

                //Use the following console command to see all user events 
                //SHOWALLEVENTS -I:BUILTINSCHEDULER_SSP -G:TOINE

                // Check if the controller supports realys
                if (this.SupportsRelay)
                {
                    // Create a new array sized for the relays on the controller + 1 as relays are 1 based not 0
                    myRelays = new Relay[this.RelayPorts.Count + 1];
                    // Register each relay in the control system
                    for (uint i = 1; i <= this.RelayPorts.Count; i++)
                    {
                        myRelays[i] = RelayPorts[i];
                        if (myRelays[i].Register() != eDeviceRegistrationUnRegistrationResponse.Success)
                            ErrorLog.Error("Error Registering Relay {0}: {1}", myRelays[i].ID, myRelays[i].DeviceRegistrationFailureReason);
                    }
                }
            }
            catch (Exception e)
            {
                ErrorLog.Error("Error in the constructor: {0}", e.Message);
            }
        }

        void myScheduler_RelayEvent(int i)
        {
            myRelays[i].Open();
            Thread.Sleep(1000);
            myRelays[i].Close();
        }
        void ClearEvents(string cmd)
        {
            myScheduler.Clear();
        }
        void InitializeEvents(string unused)
        {
            myScheduler.InitializeEvents();
        }
        void AckEvent(string EventNo)
        {
            int x = Convert.ToInt16(EventNo);
            if (x > 0)
                myScheduler.Ack(x);
            else
                CrestronConsole.PrintLine("Please use \"1\" or \"2\"");
        }
        public override void InitializeSystem()
        {
            try
            {

            }
            catch (Exception e)
            {
                ErrorLog.Error("Error in InitializeSystem: {0}", e.Message);
            }
        }
        void ControlSystem_ControllerEthernetEventHandler(EthernetEventArgs ethernetEventArgs)
        {
            switch (ethernetEventArgs.EthernetEventType)
            {//Determine the event type Link Up or Link Down
                case (eEthernetEventType.LinkDown):
                    //Next need to determine which adapter the event is for. 
                    //LAN is the adapter is the port connected to external networks.
                    if (ethernetEventArgs.EthernetAdapter == EthernetAdapterType.EthernetLANAdapter)
                    {
                        //
                    }
                    break;
                case (eEthernetEventType.LinkUp):
                    if (ethernetEventArgs.EthernetAdapter == EthernetAdapterType.EthernetLANAdapter)
                    {

                    }
                    break;
            }
        }
        void ControlSystem_ControllerProgramEventHandler(eProgramStatusEventType programStatusEventType)
        {
            switch (programStatusEventType)
            {
                case (eProgramStatusEventType.Paused):
                    //The program has been paused.  Pause all user threads/timers as needed.
                    break;
                case (eProgramStatusEventType.Resumed):
                    //The program has been resumed. Resume all the user threads/timers as needed.
                    break;
                case (eProgramStatusEventType.Stopping):
                    //The program has been stopped.
                    //Close all threads. 
                    //Shutdown all Client/Servers in the system.
                    //General cleanup.
                    //Unsubscribe to all System Monitor events
                    break;
            }

        }
        void ControlSystem_ControllerSystemEventHandler(eSystemEventType systemEventType)
        {
            switch (systemEventType)
            {
                case (eSystemEventType.DiskInserted):
                    //Removable media was detected on the system
                    break;
                case (eSystemEventType.DiskRemoved):
                    //Removable media was detached from the system
                    break;
                case (eSystemEventType.Rebooting):
                    //The system is rebooting. 
                    //Very limited time to preform clean up and save any settings to disk.
                    break;
            }

        }
    }
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
            if (myGroup.ScheduledEvents.ContainsKey("Relay 1") == false)
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
                CrestronConsole.PrintLine("Snooze Result: {0}", SchEvent.Snooze(2).ToString());
                RelayEvent(2);
            }
        }
    }
}