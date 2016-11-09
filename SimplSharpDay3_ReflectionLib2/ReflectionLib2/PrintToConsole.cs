using System;
using Crestron.SimplSharp;                          				// For Basic SIMPL# Classes
using Crestron.SimplSharpPro;                       				// For Basic SIMPL#Pro classes

namespace ReflectionLib2
{
    public class PrintToConsole
    {
        public PrintToConsole()
        {
            // nothing to do here
        }

        public void PrintSomething(String str)
        {
            CrestronConsole.Print(str);
        }
    }
}
