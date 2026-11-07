using System;
using System.IO;
using System.Management.Automation;
using System.Reflection;

namespace Microsoft.PowerShell.PSReadLine
{
    public class OnModuleImportAndRemove : IModuleAssemblyInitializer, IModuleAssemblyCleanup
    {
        public void OnImport()
        {
            AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;

            // Build the PSConsoleReadLine singleton (console setup, options, key bindings) on a background
            // thread while the rest of startup, usually the profile, runs. The host name is the one thing it
            // needs from the runspace, so read that here, on the importing thread.
            BackgroundInitialization.Start();
        }

        public void OnRemove(PSModuleInfo module)
        {
            AppDomain.CurrentDomain.AssemblyResolve -= ResolveAssembly;
        }

        /// <summary>
        /// Load the correct 'Polyfiller' assembly based on the runtime.
        /// </summary>
        private static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
        {
            if (args.Name != "Microsoft.PowerShell.PSReadLine.Polyfiller, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null")
            {
                return null;
            }

            string root = Path.GetDirectoryName(typeof(OnModuleImportAndRemove).Assembly.Location);
            string subd = (Environment.Version.Major >= 6) ? "net6plus" : "netstd";
            string path = Path.Combine(root, subd, "Microsoft.PowerShell.PSReadLine.Polyfiller.dll");

            return Assembly.LoadFrom(path);
        }
    }
}
