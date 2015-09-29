using System;
using System.Reflection;
using Org.Kevoree.Annotation;
using System.Collections.Generic;
using System.Linq;
using Org.Kevoree.Log;
using org.kevoree;
using org.kevoree.factory;
using System.IO;
using System.Text.RegularExpressions;

namespace Org.Kevoree.ModelGenerator
{
	public class ModelGenerator
	{
		private static void Main(string[] args) {

            var options = new CommandLineOptions();
            
            if (CommandLine.Parser.Default.ParseArguments(args, options)) { 
                
                if (!new Regex("^\\d+\\.\\d+\\.\\d+$").IsMatch(options.PackageVersion))
                {
                        
                    Console.WriteLine(options.GetUsage());
                }
                else
                {

                    if (options.NugetLocalRepositoryPath == null || options.NugetLocalRepositoryPath == "")
                    {
                        options.NugetLocalRepositoryPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                        Directory.CreateDirectory(options.NugetLocalRepositoryPath);
                    }
                    var componentloaded = new NugetLoader.NugetLoader(options.NugetLocalRepositoryPath).LoadRunnerFromPackage<Runner>(options.PackageName, options.PackageVersion, options.NugetRepositoryUrl);
                    componentloaded.AnalyseAndPublish(options.PackageName, options.PackageVersion, options.KevoreeRegistryUrl);
                }
            }
		}
	}
}