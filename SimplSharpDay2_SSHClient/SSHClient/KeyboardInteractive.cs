using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.Ssh;

namespace SSHClient
{
    public class KeyboardInteractive
    {
        ConnectionInfo = new KeyboardInteractiveConnectionInfo(address, (port > 0 ? port : 22), LoginUser);
        ConnectionInfo.AuthenticationPrompt += delegate(object sender, AuthenticationPromptEventArgs e)
            {
                //CrestronConsole.PrintLine("Got Auth Prompt");
                foreach (var prompt in e.Prompts)
                {
                    if (prompt.Request.Equals("Password: ", StringComparison.InvariantCultureIgnoreCase))
                    {
                        //CrestronConsole.PrintLine(prompt.Request);
                        //CrestronConsole.PrintLine(LoginPassword);
                        prompt.Response = LoginPassword;
                    }
                }
            };
    }
}