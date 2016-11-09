using System;
using Crestron.SimplSharp;                          				// For Basic SIMPL# Classes
using Crestron.SimplSharpPro;                       				// For Basic SIMPL#Pro classes
using Crestron.SimplSharpPro.CrestronThread;        	// For Threading
using Crestron.SimplSharpPro.Diagnostics;		    		// For System Monitor Access
using Crestron.SimplSharpPro.DeviceSupport;         	// For Generic Device Support
using Crestron.SimplSharpPro.UI;
using System.Collections.Generic;
using Crestron.SimplSharp.CrestronIO;
using Crestron.SimplSharp.Reflection;


namespace XpanelSample
{
    public class ControlSystem : CrestronControlSystem
    {
        // Define local variables ...

        private XpanelForSmartGraphics myPanel;
        private String Assembly1Path = @"\NVRAM\ReflectionLib1.dll";
        private String Assembly2Path = @"\NVRAM\ReflectionLib2.dll";
        private Assembly myAssembly;
        private CType myType;
        object myInstance;
        


        /// <summary>
        /// Constructor of the Control System Class. Make sure the constructor always exists.
        /// If it doesn't exit, the code will not run on your 3-Series processor.
        /// </summary>
        public ControlSystem()
            : base()
        {

            // Set the number of threads which you want to use in your program - At this point the threads cannot be created but we should
            // define the max number of threads which we will use in the system.
            // the right number depends on your project; do not make this number unnecessarily large
            Thread.MaxNumberOfUserThreads = 20;
            if (this.SupportsEthernet)
            {
                myPanel = new XpanelForSmartGraphics(0x3, this);
                if (myPanel.Register() != eDeviceRegistrationUnRegistrationResponse.Success)
                    ErrorLog.Error("Error Registering Xpanel");
                myPanel.LoadSmartObjects(@"\NVRAM\Xpnl.sgd");
                foreach (KeyValuePair<uint, SmartObject> mySmartObject in myPanel.SmartObjects)
                {
                    mySmartObject.Value.SigChange += new SmartObjectSigChangeEventHandler(Value_SigChange);
                }
            }

        }

        private void MyStuff(IDimmer x)
        {
            x.DimmingLoads[1].FullOff();
        }

        void Value_SigChange(GenericBase currentDevice, SmartObjectEventArgs args)
        {
            if (args.SmartObjectArgs.ID == 1) // D-Pad
            {
                if (args.Sig.Name.ToUpper() == "OK" )
                {
                    if (args.Sig.BoolValue)
                    {
                        if (myAssembly.FullName.ToUpper().Contains("REFLECTIONLIB1"))
                        {
                            myInstance = myAssembly.CreateInstance("ReflectionLib1.RelayClicks");
                            myType = myInstance.GetType();
                            FieldInfo finfo = myType.GetField("cs");
                            finfo.SetValue(myInstance, this);
                            MethodInfo minfo = myType.GetMethod("Initialize");
                            minfo.Invoke(myInstance, new object[] { });
                            minfo = myType.GetMethod("StartClicking");
                            minfo.Invoke(myInstance, new object[] {5000});
                        }
                        if (myAssembly.FullName.ToUpper().Contains("REFLECTIONLIB2"))
                        {
                            myInstance = myAssembly.CreateInstance("ReflectionLib2.PrintToConsole");
                            myType = myInstance.GetType();
                            MethodInfo minfo = myType.GetMethod("PrintSomething");
                            minfo.Invoke(myInstance, new object[] { "Hello World" });
                        }
                    }
                    else
                    {
                        if (myAssembly.FullName.ToUpper().Contains("REFLECTIONLIB1"))
                        {
                            MethodInfo minfo = myType.GetMethod("StopClicking");
                            minfo.Invoke(myInstance, new object[] { });
                            myInstance = null;
                        }
                    }
                    }
            }
            if (args.SmartObjectArgs.ID == 2) // Number Pad
            {
                if (args.Sig.Name.ToUpper() == "1" && args.Sig.BoolValue)
                {
                    myAssembly = Assembly.LoadFrom(Assembly1Path);
                    PrintContents();
                }

                if (args.Sig.Name.ToUpper() == "2" && args.Sig.BoolValue)
                {
                    myAssembly = Assembly.LoadFrom(Assembly2Path);
                    PrintContents();
                }

            }
        }

        private void PrintContents()
        {
            foreach (CType type in myAssembly.GetTypes())
            {
                CrestronConsole.PrintLine("Ctype: {0} ", type.FullName);
                foreach (ConstructorInfo constructor in type.GetConstructors())
                {
                    CrestronConsole.Print("{0} (", type.Name);
                    int cnt = constructor.GetParameters().Length - 1;
                    foreach (ParameterInfo param in constructor.GetParameters())
                    {
                        CrestronConsole.Print("{0} {1}", param.ParameterType, param.Name);
                        if (param.Position != cnt)
                            CrestronConsole.Print(", ");
                    }
                    CrestronConsole.PrintLine(")");


                }
                foreach (MethodInfo method in type.GetMethods())
                {
                    CrestronConsole.Print("{0} {1} (",
                                    method.ReturnType.Name,
                                    method.Name
                                    );
                    int cnt = method.GetParameters().Length - 1;
                    foreach (ParameterInfo param in method.GetParameters())
                    {
                        CrestronConsole.Print("{0} {1}", param.ParameterType, param.Name);
                        if (param.Position != cnt)
                            CrestronConsole.Print(", ");

                    }
                    CrestronConsole.PrintLine(")");
                }

                foreach (PropertyInfo property in type.GetProperties())
                {
                    CrestronConsole.PrintLine("{0} {1} get: {2} set: {3}", property.PropertyType.Name, property.Name, property.CanRead, property.CanWrite);
                    }

                foreach (FieldInfo field in type.GetFields())
                {
                    CrestronConsole.PrintLine("{0} {1}", field.FieldType.Name, field.Name);
                }
            }

        }


        /// <summary>
        /// Overridden function... Invoked before any traffic starts flowing back and forth between the devices and the 
        /// user program. 
        /// This is used to start all the user threads and create all events / mutexes etc.
        /// This function should exit ... If this function does not exit then the program will not start
        /// </summary>
        public override void InitializeSystem()
        {
            myAssembly = Assembly.GetExecutingAssembly();
            

        }
    }
}
