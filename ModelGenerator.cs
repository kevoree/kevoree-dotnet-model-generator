using System;
using System.Reflection;
using Org.Kevoree.Annotation;
using System.Collections.Generic;
using System.Linq;
using Org.Kevoree.Log;
using org.kevoree;
using org.kevoree.factory;
using System.Text.RegularExpressions;
using Org.Kevoree.ModelGenerator.Nuget;
using Org.Kevoree.NugetLoader;


namespace Org.Kevoree.ModelGenerator.Test
{
	public class ModelGenerator
	{
		public ModelGenerator ()
		{
		}

		private static void Main() {
            string packageName = "org-kevoree-yield-version";
            string packageVersion = "7.0.3";
            var componentloaded = new NugetLoader.NugetLoader(@"C:\Users\mleduc\Desktop\package_solution\").LoadRunnerFromPackage<Runner>(packageName, packageVersion);
            componentloaded.Init();
            componentloaded.AnalyseAndPublish();
			//var assembly = Assembly.LoadFile ("aa");
			//var assemblyTypes = assembly.GetTypes ();   
		}       
	}

}