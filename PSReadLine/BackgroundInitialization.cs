/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Runspaces;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Microsoft.PowerShell
{
    /// <summary>
    /// Builds the PSConsoleReadLine singleton (console setup, options, key bindings) on a background thread
    /// while the rest of startup, usually the profile, runs. This is a separate class because touching any static
    /// member of PSConsoleReadLine would run its type initializer right here.
    /// </summary>
    internal static class BackgroundInitialization
    {
        /// <summary>
        /// The host name, the one thing the constructor needs from the runspace, read on the importing thread
        /// (which has a runspace) for the constructor running on another.
        /// </summary>
        internal static string HostName { get; private set; }

        internal static void Start()
        {
            HostName = ReadHostNameInScope();
            if (HostName != null)
            {
                Task.Run(() => RuntimeHelpers.RunClassConstructor(typeof(PSConsoleReadLine).TypeHandle));
            }
        }

        /// <summary>
        /// The host name, read by a script block run in the importing command's scope rather than a new pipeline
        /// (which needs a PowerShell instance, a pipeline, and compiling a script with an attribute).
        /// Falls back to <see cref="ReadHostName"/>.
        /// </summary>
        private static string ReadHostNameInScope()
        {
            try
            {
                object result = ScriptBlock.Create("$Host").InvokeReturnAsIs();
                if ((result is PSObject wrapped ? wrapped.BaseObject : result) is PSHost host)
                {
                    return host.Name;
                }
            }
            catch
            {
            }

            return ReadHostName();
        }

        /// <summary>
        /// The current runspace's host name, or null if this thread has no runspace.
        /// </summary>
        internal static string ReadHostName()
        {
            // This works mostly by luck - we're not doing anything to guarantee the constructor for our
            // singleton is called on a thread with a runspace, but it is happening by coincidence.
            using (var ps = System.Management.Automation.PowerShell.Create(RunspaceMode.CurrentRunspace))
            {
                try
                {
                    var results = ps.AddScript("[System.Diagnostics.DebuggerHidden()]param() $Host", useLocalScope: true).Invoke<PSHost>();
                    PSHost host = results.Count == 1 ? results[0] : null;
                    return host?.Name;
                }
                catch
                {
                    return null;
                }
            }
        }
    }
}
