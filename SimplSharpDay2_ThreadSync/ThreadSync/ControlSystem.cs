using System;
using Crestron.SimplSharp;                          				// For Basic SIMPL# Classes
using Crestron.SimplSharpPro;                       				// For Basic SIMPL#Pro classes
using Crestron.SimplSharpPro.CrestronThread;        	            // For Threading
using Crestron.SimplSharpPro.Diagnostics;		    		        // For System Monitor Access
using Crestron.SimplSharpPro.DeviceSupport;         	            // For Generic Device Support

namespace ThreadSync
{
    public class MutexParameters
    {
        public int ThreadNumber = 0;
        public int Timeout = 0;
        public MutexParameters(int threadNumber, int timeout)
        {
            ThreadNumber = threadNumber;
            Timeout = timeout;
        }
    }

    public class ControlSystem : CrestronControlSystem
    {
        private Thread[] myThread = new Thread[3];
        private CEvent myEvent;
        private CCriticalSection myCS = new CCriticalSection();
        private CMutex myMutex = new CMutex();

        #region Event Methods
        private void testEventCore(int s)
        {
            try
            {
                while (true)
                {
                    myEvent.Wait();
                    CrestronConsole.PrintLine("*** Event Running {0} ***", s);
                }
            }
            catch (Exception e)
            {
                CrestronConsole.PrintLine("*** Error: {0} - {1} ***", s, e.Message);
            }
        }
        private object TestCEvent(object o)
        {
            testEventCore((int)o);
            return 1;
        }
        private void createTestCEventThreads()
        {
            myThread[0] = new Thread(TestCEvent, 1);
            myThread[1] = new Thread(TestCEvent, 2);
            myThread[2] = new Thread(TestCEvent, 3);
        }
        private void TestEventCmd(string parameters)
        {
            switch (parameters.ToUpper())
            {
                case "SET":
                    myEvent.Set();
                    break;
                case "RESET":
                    myEvent.Reset();
                    break;
                case "CLOSE":
                    myEvent.Close();
                    break;
                case "CREATE":
                    killThreads();
                    myEvent = new CEvent();
                    createTestCEventThreads();
                    break;
                case "CREATE FALSE FALSE":
                    killThreads();
                    myEvent = new CEvent(false, false);
                    createTestCEventThreads();
                    break;
                case "CREATE FALSE TRUE":
                    killThreads();
                    myEvent = new CEvent(false, true);
                    createTestCEventThreads();
                    break;
                case "CREATE TRUE FALSE":
                    killThreads();
                    myEvent = new CEvent(true, false);
                    createTestCEventThreads();
                    break;
                case "CREATE TRUE TRUE":
                    killThreads();
                    myEvent = new CEvent(true, true);
                    createTestCEventThreads();
                    break;
                default:
                    break;
            }
        }
        #endregion

        #region CCritical Section Methods
        private void testCSCore(Object s)
        {
            try
            {
                myCS.Enter();
                while (true)
                {
                    CrestronConsole.PrintLine("*** Critical Section Running {0} ***", s.ToString());
                }
            }
            catch (Exception e)
            {
                CrestronConsole.PrintLine("*** Error: {0} - {1} ***", s.ToString(), e.Message);
            }
            finally
            {
                myCS.Leave();  //always in finally
            }
        }
        private object TestCS(object o)
        {
            testCSCore(o);
            return 1;
        }
        private void TestCSCmd(string parameters)
        {
            string[] strParams = parameters.Split(' ');
            switch (strParams[0].ToUpper())
            {
                case "START":
                    if (myThread[Convert.ToInt32(strParams[1]) - 1] != null)
                        myThread[Convert.ToInt32(strParams[1]) - 1].Abort();
                    myThread[Convert.ToInt32(strParams[1]) - 1] = new Thread(TestCS, strParams[1]);
                    break;
                case "STOP":
                    if (myThread[Convert.ToInt32(strParams[1]) - 1] != null)
                        myThread[Convert.ToInt32(strParams[1]) - 1].Abort();
                    break;
                case "ENTER":
                    myCS.Enter();
                    break;
                case "LEAVE":
                    myCS.Leave();
                    break;
                default:
                    break;

            }
        }
        #endregion

        #region CMutex Methods
        private void testCMCore(int s, int t)
        {
            bool i = false;
            try
            {
                if (t ==0 ? myMutex.WaitForMutex(): myMutex.WaitForMutex(t) )
                {
                    i = true;
                    while (true)
                        CrestronConsole.PrintLine("*** Mutex {0} Running ***", s);
                }
                else
                {
                    i = false;
                        CrestronConsole.PrintLine("*** Mutex {0} Timed Out {1}ms ***", s, t);

                }

            }
            catch (Exception e)
            {
                CrestronConsole.PrintLine("*** Error: {0} - {1} ***", s, e.Message);
            }
            finally
            {
                if (i)
                    myMutex.ReleaseMutex();
            }
        }

        private object TestCM(object o)
        {
            MutexParameters myCMP = (MutexParameters)o;
            testCMCore(myCMP.ThreadNumber, myCMP.Timeout);

            return false;
        }
        private void TestCMCmd(string parameters)
        {
            string[] strParams = parameters.Split(' ');
            switch (strParams[0].ToUpper())
            {
                case "START":
                    if (myThread[Convert.ToInt32(strParams[1]) - 1] != null)
                        myThread[Convert.ToInt32(strParams[1]) - 1].Abort();
                    myThread[Convert.ToInt32(strParams[1]) - 1] = new Thread(TestCM, new MutexParameters(Convert.ToInt32(strParams[1]), Convert.ToInt32(strParams[2])));
                    break;
                case "STOP":
                    if (myThread[Convert.ToInt32(strParams[1]) - 1] != null)
                        myThread[Convert.ToInt32(strParams[1]) - 1].Abort();
                    break;
                case "WAIT":
                    myMutex.WaitForMutex();
                    break;
                case "RELEASE":
                    myMutex.ReleaseMutex();
                    break;
            }
        }

        #endregion
        
        private void killThreads()
        {
            if (myThread[0] != null)
                myThread[0].Abort();
            if (myThread[1] != null)
                myThread[1].Abort();
            if (myThread[2] != null)
                myThread[2].Abort();
            myThread[0] = null;
            myThread[1] = null;
            myThread[2] = null;
        }
        public ControlSystem()
            : base()
        {
            Thread.MaxNumberOfUserThreads = 20;
            CrestronConsole.AddNewConsoleCommand(TestEventCmd, "testevent", "testevent", ConsoleAccessLevelEnum.AccessOperator);
            CrestronConsole.AddNewConsoleCommand(TestCSCmd, "TestCS", "TestCS", ConsoleAccessLevelEnum.AccessOperator);
            CrestronConsole.AddNewConsoleCommand(TestCMCmd, "TestCM", "TestCM", ConsoleAccessLevelEnum.AccessOperator);
        }
        public override void InitializeSystem()
        {
            // This should always return   
        }
    }
}
