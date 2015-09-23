using NuGet;
using Org.Kevoree.Annotation;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Registration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Org.Kevoree.ModelGenerator.Nuget
{
    class ComponentLoader
    {
        public static Runner loadComponent(string packageName, string packageVersion)
        {
            var cachePath = DirectoryNameManager.getShadowCopyPath(packageName, packageVersion);
            //var pluginPath = Path.Combine (AppDomain.CurrentDomain.SetupInformation.ApplicationBase,  "Plugins");
            var pluginPath = @"C:\Users\mleduc\Documents\GitHub\kevoree-dotnet-comp-yield-version\bin\Debug";
            

            // This creates a ShadowCopy of the MEF DLL's 
            // (and any other DLL's in the ShadowCopyDirectories)
            var setup = new AppDomainSetup
            {
                CachePath = cachePath,
                ShadowCopyFiles = "true",
                ShadowCopyDirectories = pluginPath
            };

            // Create a new AppDomain then create a new instance 
            // of this application in the new AppDomain.            
            AppDomain domain = AppDomain.CreateDomain("Host_AppDomain", AppDomain.CurrentDomain.Evidence, setup);
            var runner = (Runner)domain.CreateInstanceAndUnwrap(typeof(Runner).Assembly.FullName, typeof(Runner).FullName);
            return runner;
        }
    }
}
