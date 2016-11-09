using System;
using System.Text;                                      // For StringBuilder
using System.Collections.Generic;                       // For KeyValuePair in HTML
using Crestron.SimplSharp;                          	// For Basic SIMPL# Classes
using Crestron.SimplSharpPro;                       	// For Basic SIMPL#Pro classes
using Crestron.SimplSharpPro.CrestronThread;        	// For Threading
using Crestron.SimplSharpPro.Diagnostics;		    	// For System Monitor Access
using Crestron.SimplSharpPro.DeviceSupport;         	// For Generic Device Support
using Crestron.SimplSharpPro.Keypads;                   // For our C2N-CBD-P
using Crestron.SimplSharpPro.UI;
using Crestron.SimplSharpPro.EthernetCommunication;     // For EISC

namespace SimplSharpDay1
{
    public class ControlSystem : CrestronControlSystem
    {
        #region GlobalVariables
        private C2nCbdP myKeypad;        //C2N-CBD-P
        private IROutputPort myPort;     //Named IR Port
        private CrestronQueue<String> rxQueue = new CrestronQueue<String>();
        private Thread rxHandler;        //Separate Thread to Handle Incoming Serial Data after gather was made.
        private XpanelForSmartGraphics myPanel; //Xpanel Exercise
        private EthernetIntersystemCommunications myEISC;
        private SigGroup MySigGroup;
        #endregion
        public ControlSystem()
            : base()
        {
            try
            {
                Thread.MaxNumberOfUserThreads = 20;
                //Hello World Crestron Console
                CrestronConsole.AddNewConsoleCommand(HelloPrinting, "HelloWorld","Prints Hello & the text that follows", ConsoleAccessLevelEnum.AccessOperator);
                //Subscribe to the controller events (System, Program, and Ethernet)
                CrestronEnvironment.SystemEventHandler += new SystemEventHandler(ControlSystem_ControllerSystemEventHandler);
                CrestronEnvironment.ProgramStatusEventHandler += new ProgramStatusEventHandler(ControlSystem_ControllerProgramEventHandler);
                CrestronEnvironment.EthernetEventHandler += new EthernetEventHandler(ControlSystem_ControllerEthernetEventHandler);
                CrestronConsole.PrintLine("DefaultConstructor Complete"); //Hello World
                #region Keypad
                if (this.SupportsCresnet)    //Make sure the system has CresNet
                {
                    myKeypad = new C2nCbdP(0x25, this);
                    myKeypad.ButtonStateChange += new ButtonEventHandler(myKeypad_ButtonStateChange);
                    if (myKeypad.NumberOfVersiPorts > 0) //VersiPort addtion
                    {
                        for (uint i = 1; i < 2; i++)
                        {
                            myKeypad.VersiPorts[i].SetVersiportConfiguration(eVersiportConfiguration.DigitalInput);
                            myKeypad.VersiPorts[i].VersiportChange += new VersiportEventHandler(ControlSystem_VersiportChange);
                        }
                    }

                    if (myKeypad.Register() != eDeviceRegistrationUnRegistrationResponse.Success) // Hello World Keypad
                        ErrorLog.Error("Error Registering Keypad on ID 0x25: {0}", myKeypad.RegistrationFailureReason);
                    else
                    {
                        myKeypad.Button[1].Name = eButtonName.Up;
                        myKeypad.Button[2].Name = eButtonName.Down;
                    }

                }
                #endregion
                #region KeypadWithQuery
                //Define Keypad with Device Query
                if (this.SupportsCresnet)
                {
                    var QueryResponse = CrestronCresnetHelper.Query();
                    if (QueryResponse == CrestronCresnetHelper.eCresnetDiscoveryReturnValues.Success)
                    {
                        //foreach (CrestronCresnetHelper.DiscoveredDeviceElement Item in CrestronCresnetHelper.DiscoveredElementsList)  //Gets a little long so we do the var in instead and it works it out
                        foreach (var Item in CrestronCresnetHelper.DiscoveredElementsList)
                        {
                            if (Item.DeviceModel.ToUpper().Contains("C2N-CBD"))
                            {
                                if (myKeypad == null) //Check to make sure we have not done created it already.
                                {
                                    myKeypad = new C2nCbdP(Item.CresnetId, this);
                                    myKeypad.ButtonStateChange += new ButtonEventHandler(myKeypad_ButtonStateChange);
                                    if (myKeypad.Register() != eDeviceRegistrationUnRegistrationResponse.Success)
                                    {
                                        ErrorLog.Error("Error Registering Keypad: {0}", myKeypad.RegistrationFailureReason);
                                        myKeypad = null;
                                    }
                                }
                            }
                        }
                    }
                }
                #endregion
                #region IR
                if (this.SupportsIROut)
                {
                    if (ControllerIROutputSlot.Register() != eDeviceRegistrationUnRegistrationResponse.Success)
                        ErrorLog.Error("Error Registering IR Slot: {0}", ControllerIROutputSlot.DeviceRegistrationFailureReason);
                    else
                    {
                        myPort = IROutputPorts[1];
                        myPort.LoadIRDriver(@"\NVRAM\AppleTV.ir");
                        foreach (string s in myPort.AvailableStandardIRCmds())
                            CrestronConsole.PrintLine("AppleTV Std: {0}", s);
                        foreach (string s in myPort.AvailableIRCmds())
                            CrestronConsole.PrintLine("AppleTV Std: {0}", s);

                    }
                }
                #endregion
                #region VersiPort
                if (this.SupportsVersiport)
                {
                    for (uint i = 1; i <= 2; i++)
                    {
                        if (this.VersiPorts[i].Register() != eDeviceRegistrationUnRegistrationResponse.Success)
                            ErrorLog.Error("Error Registering Versiport 1: {0}", this.VersiPorts[i].DeviceRegistrationFailureReason);
                        else
                            this.VersiPorts[i].SetVersiportConfiguration(eVersiportConfiguration.DigitalOutput);
                    }
                }
                #endregion
                #region ComPorts
                if (this.SupportsComPort)
                {
                    for (uint i = 1; i <= 2; i++)
                    {
                        this.ComPorts[i].SerialDataReceived += new ComPortDataReceivedEvent(ControlSystem_SerialDataReceived);
                        if (this.ComPorts[i].Register() != eDeviceRegistrationUnRegistrationResponse.Success)
                            ErrorLog.Error("Error Registering ComPort{0}: {1}", i, ComPorts[i].DeviceRegistrationFailureReason);
                        else
                        {
                            this.ComPorts[i].SetComPortSpec(ComPort.eComBaudRates.ComspecBaudRate19200,
                                                            ComPort.eComDataBits.ComspecDataBits8,
                                                            ComPort.eComParityType.ComspecParityNone,
                                                            ComPort.eComStopBits.ComspecStopBits1,
                                                            ComPort.eComProtocolType.ComspecProtocolRS232,
                                                            ComPort.eComHardwareHandshakeType.ComspecHardwareHandshakeNone,
                                                            ComPort.eComSoftwareHandshakeType.ComspecSoftwareHandshakeNone,
                                                            false);

                        }
                    }
                }
                #endregion1
                #region Touchpanel
                if (this.SupportsEthernet)
                {
                    myPanel = new XpanelForSmartGraphics(0x03, this);
                    myPanel.SigChange += new SigEventHandler(myPanel_SigChange);
                    if (myPanel.Register() != eDeviceRegistrationUnRegistrationResponse.Success)
                        ErrorLog.Error("Error in Registering xPanel: {0}", myPanel.RegistrationFailureReason);
                    else
                    {
                        myPanel.LoadSmartObjects(@"\NVRAM\xpnl.sgd");
                        CrestronConsole.PrintLine("Loaded SmartObjects: {0}", myPanel.SmartObjects.Count);
                        foreach (KeyValuePair<uint, SmartObject> mySmartObject in myPanel.SmartObjects)
                        {
                            mySmartObject.Value.SigChange += new SmartObjectSigChangeEventHandler(Value_SigChange);
                        }
                        MySigGroup = CreateSigGroup(1, eSigType.String);
                        MySigGroup.Add(myPanel.StringInput[1]);
                        MySigGroup.Add(myPanel.StringInput[2]);
                    }
                }
                #endregion
                #region EISC
                if (this.SupportsEthernet)
                {
                    myEISC = new EthernetIntersystemCommunications(0x04, "127.0.0.2", this);
                    myEISC.SigChange += new SigEventHandler(myEISC_SigChange);
                    if (myEISC.Register() != eDeviceRegistrationUnRegistrationResponse.Success)
                        ErrorLog.Error("Error in Registering EISC: {0}", myEISC.RegistrationFailureReason);
                    else
                        myEISC.SigChange -= myEISC_SigChange;
                }
                #endregion
            }
            catch (Exception e)
            {
                ErrorLog.Error("Error in the constructor: {0}", e.Message);
            }
        }
    
        void HelloPrinting(String s)    //Build first part of Hello World Console Command.
        {
            CrestronConsole.ConsoleCommandResponse("Hello {0}", s);
        }

        void myEISC_SigChange(BasicTriList currentDevice, SigEventArgs args)
        {
            switch (args.Sig.Type)
            {
                case eSigType.Bool:
                    CrestronConsole.PrintLine("Boolean Received from EISC: {0}, {1}", args.Sig.Number, args.Sig.BoolValue);
                    break;
                case eSigType.UShort:
                    CrestronConsole.PrintLine("Ushort Received from EISC: {0}, {1}", args.Sig.Number, args.Sig.UShortValue);
                    break;
                case eSigType.String:
                    CrestronConsole.PrintLine("String Received from EISC: {0}, {1}", args.Sig.Number, args.Sig.StringValue);
                    break;
            }
        }
        void Value_SigChange(GenericBase currentDevice, SmartObjectEventArgs args)
        {
            MySigGroup.StringValue = String.Format("Event type: {0}, Signal: {1}, from SO: {2}", args.Sig.Type, args.Sig.Name, args.SmartObjectArgs.ID);
            myPanel.StringInput[1].StringValue = String.Format("Event Type: {0}, Signal: {1}, from Smart Object: {2}", args.Sig.Type, args.Sig.Name, args.SmartObjectArgs.ID);
        }
        void myPanel_SigChange(BasicTriList currentDevice, SigEventArgs args)
        {
            myPanel.StringInput[2].StringValue = String.Format("Panel Trigger: {0}, {1}", args.Sig.Type, args.Sig.Number);
        }
        void ControlSystem_SerialDataReceived(ComPort ReceivingComPort, ComPortSerialDataEventArgs args)
        {
            if (ReceivingComPort == ComPorts[2])
                rxQueue.Enqueue(args.SerialData);
        }
        void ControlSystem_VersiportChange(Versiport port, VersiportEventArgs args)
        {
            if (port == myKeypad.VersiPorts[1])
                CrestronConsole.PrintLine("Port 1: {0}", port.DigitalIn);
            if (port == myKeypad.VersiPorts[2])
                CrestronConsole.PrintLine("Port 2: {0}", port.DigitalIn);
        }
        void myKeypad_ButtonStateChange(GenericBase device, ButtonEventArgs args)
        {
            CrestronConsole.PrintLine("Keypad ID: {0:x}, ButtonNo: {1}, ButtonState: (2)", device.ID, args.Button.Number, args.Button.State);  // Keypad first excercise  If you do {0} will print decimal  :x does Hex.
            myEISC.StringInput[1].StringValue = String.Format("Keypad ButtonName: {0}, ButtonNo: {1}, State: {2}", args.Button.Name, args.Button.Number, args.Button.State);
            if (args.Button.State == eButtonState.Pressed)
            {
                this.VersiPorts[args.Button.Number].DigitalOut = true;
                switch (args.Button.Name)
                {
                    case eButtonName.Up:
                        myPort.Press("UP_ARROW");
                        ComPorts[1].Send("Test Transmission please ignore");
                        break;
                    case eButtonName.Down:
                        myPort.Press("DN_ARROW");
                        ComPorts[1].Send("\n");
                        break;
                    default:
                        CrestronConsole.PrintLine("Key Not Programmed: {0}", args.Button.Number);
                        break;
                }
            }
            if (args.Button.State == eButtonState.Released)
            {
                myPort.Release();
                this.VersiPorts[args.Button.Number].DigitalOut = false;
            }
        }
        public override void InitializeSystem()
        {
            try
            {
                CrestronConsole.PrintLine("InitializeSystem Complete"); // Hello World
                rxHandler = new Thread(Gather, null, Thread.eThreadStartOptions.Running);
            }
            catch (Exception e)
            {
                ErrorLog.Error("Error in InitializeSystem: {0}", e.Message);
            }
        }
        object Gather(object o)  //second part of the ComPort for the receive for the thread.
        {
            StringBuilder rxData = new StringBuilder();
            int Pos = -1;
            String rxGathered = String.Empty;
            while (true)
            {
                try
                {
                    String rxTemp = rxQueue.Dequeue();
                    if (rxTemp == null) // Exit the Loop and close the thread when we are done.  Set enqueue to null in the stopping program section.
                        return null;
                    rxData.Append(rxTemp);
                    rxGathered = rxData.ToString();

                    Pos = rxGathered.IndexOf("\n");
                    if (Pos >= 0)    //Zero base string so start at 0  //Hardware is 1 based.
                    {
                        rxGathered = rxGathered.Substring(0, Pos + 1);
                        CrestronConsole.PrintLine("Gather: {0}", rxGathered);
                        rxData.Remove(0, Pos + 1);
                    }
                }
                catch (Exception e)
                {
                    ErrorLog.Error("Exception in Gathering String: {0}", e.Message);
                }
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
                    rxQueue.Enqueue(null);      //Last part of the Com Part to kill the thread for the buffer.
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
}