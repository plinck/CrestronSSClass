using System;
using Crestron.SimplSharp;                          	// For Basic SIMPL# Classes
using Crestron.SimplSharpPro;                       	// For Basic SIMPL#Pro classes
using Crestron.SimplSharpPro.CrestronThread;        	// For Threading
using Crestron.SimplSharpPro.Diagnostics;		    	// For System Monitor Access
using Crestron.SimplSharpPro.DeviceSupport;         	// For Generic Device Support
using Crestron.SimplSharp.Net.Http;                     // For HTTP Client
using Newtonsoft.Json;                       // For Json Deserializer

// All notes are there to assist the trainer and don't need replicating for the student exercise
namespace SimplSharpDay3_Json
{
    public class ControlSystem : CrestronControlSystem
    {
        JsonCollector JC = new JsonCollector();
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
                CrestronConsole.AddNewConsoleCommand(GetPosts, "GetPosts", "Use 1 or 2 to perform each function", ConsoleAccessLevelEnum.AccessOperator);
            }
            catch (Exception e)
            {
                ErrorLog.Error("Error in the constructor: {0}", e.Message);
            }
        }
        void GetPosts(string number)
        {
            switch (number)
            {
                case "1":
                    JC.Connect(@"http://jsonplaceholder.typicode.com/posts/1", Convert.ToInt16(number));
                    break;
                case "2":
                    JC.Connect(@"http://jsonplaceholder.typicode.com/posts", Convert.ToInt16(number));
                    break;
                default:
                    CrestronConsole.ConsoleCommandResponse("Incorrect input, please use 1 for one response and 2 for multiple");
                    break;
            }
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
    public class JsonCollector
    {
        public void Connect(string url, int i)
        {
            HttpClient myJsonClient = new HttpClient(); // Create the client
            HttpClientResponse myJsonResponse;          // Create a holder for the response
            HttpClientRequest myJsonRequest;            // Create the request
            myJsonRequest = new HttpClientRequest();    // Instanciate the request
            myJsonClient.TimeoutEnabled = true;         // HttpClient settings these can be left at default or setup as needed
            myJsonClient.Timeout = 5;
            myJsonClient.KeepAlive = false;
            myJsonRequest.Url.Parse(url);               // take the full URL and break it down for the request to decide how to handle it
            myJsonResponse = myJsonClient.Dispatch(myJsonRequest); // This is when we send the message and the response catches the reply.
            //CrestronConsole.PrintLine(myJsonResponse.ContentString); // Start with this and let them ensure they are getting something back.
            Deserialize(myJsonResponse.ContentString, i);   
        }
        public void Deserialize(string data, int i)
        {

            if (i == 1) //Version 1 we can build this without the if, it'll work for 1 but not for 2 let's make that mistake first
            {
                RootObject oData = JsonConvert.DeserializeObject<RootObject>(data);
                CrestronConsole.PrintLine("{0}, {1}", oData.userId, oData.title);
            }
            else if (i == 2) //Version 2, show them this method instead, notice this only works for 2 and not for 1.
            {
                RootObject[] oData = JsonConvert.DeserializeObject<RootObject[]>(data);
                foreach (var o in oData)
                    CrestronConsole.PrintLine("{0}, {1}", o.userId, o.title);
            }
            //Finish by putting in both methods and pass down the parameter for the IF/Else
        }
    }
    public class RootObject // Use Json2cSharp.com to get this object. 
    {
        public int userId { get; set; }
        public int id { get; set; }
        public string title { get; set; }
        public string body { get; set; }
    }
}