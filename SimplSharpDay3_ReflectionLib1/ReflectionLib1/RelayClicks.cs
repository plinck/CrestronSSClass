using System;
using Crestron.SimplSharp;                          				// For Basic SIMPL# Classes
using Crestron.SimplSharpPro;                       				// For Basic SIMPL#Pro classes
using Crestron.SimplSharpPro.CrestronThread;

namespace ReflectionLib1
{
    public class RelayClicks
    {
        private Thread myThread;
        public CrestronControlSystem cs;

        public RelayClicks()
        {
        }

        public void Initialize()
        {
            if (cs.SupportsRelay)
            {
                if (!cs.RelayPorts[1].Registered)
                {
                    if (cs.RelayPorts[1].Register() != eDeviceRegistrationUnRegistrationResponse.Success)
                    {
                        ErrorLog.Error(String.Format("Error Registering Relay Port: {0}", cs.RelayPorts[1].DeviceRegistrationFailureReason));
                    }
                }
            }

        }

        public void StartClicking(int interval)
        {
            CrestronConsole.PrintLine("StartClicking");
            myThread = new Thread(Clicking, interval, Thread.eThreadStartOptions.Running);
        }

        public void StopClicking()
        {
            CrestronConsole.PrintLine("StopClicking");
            myThread.Abort();
        }

        private object Clicking(object obj)
        {
            int timeout = (int)obj;
            while (true && cs.RelayPorts[1].Registered)
            {
                cs.RelayPorts[1].State = !cs.RelayPorts[1].State;
                Thread.Sleep(timeout);
            }
            return null;
        }
    }
}
