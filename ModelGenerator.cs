using System;
using System.Reflection;
using Org.Kevoree.Annotation;
using System.Collections.Generic;
using System.Linq;
using Org.Kevoree.Log;
using org.kevoree;
using org.kevoree.factory;
using Mono.Options;
using System.IO;
using System.Text.RegularExpressions;



namespace Org.Kevoree.ModelGenerator
{
	public class ModelGenerator
	{
		private static void Main(string[] args) {

            string packageName = null;
            string packageVersion = null;
            bool bShowHelp = false;
            string nugetRepositoryPath = "";
            string remoteRepositoryPath = "https://packages.nuget.org/api/v2";
            string kevoreeRegistryUrl = "http://registry.kevoree.org";

            var optionSet = new OptionSet()
            {
                { "package.name=", "the nuget package name", n => packageName = n },
                { "package.version=", "init script path", p => packageVersion = p },
                { "nuget.localRepository.path", "nuget local repository path", pa => nugetRepositoryPath = pa  },
                { "nuget.repository.url", "nuget remote repository url", rr => remoteRepositoryPath = rr  },
                { "kevoree.registry.url", "kevoree registry url", kr => kevoreeRegistryUrl = kr  },
                { "h|help=", "displays help message", v => bShowHelp = true }
            };

            try
            {
                optionSet.Parse(args);
                if (bShowHelp)
                {
                    showHelp(optionSet);
                }
                else
                {
                    if (packageName == null) {
                        throw new OptionException("Package name is required.", "package.name");
                    }

                    if(packageVersion == null || !new Regex("^\\d+\\.\\d+\\.\\d+$").IsMatch(packageVersion)) {
                        throw new OptionException("Package version is required (and should formated like X.Y.Z).", "package.version");
                    }

                    if (nugetRepositoryPath == null)
                    {
                        nugetRepositoryPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                        Directory.CreateDirectory(nugetRepositoryPath);
                    }
                    var componentloaded = new NugetLoader.NugetLoader(nugetRepositoryPath).LoadRunnerFromPackage<Runner>(packageName, packageVersion, remoteRepositoryPath);
                    componentloaded.AnalyseAndPublish(packageName, packageVersion, kevoreeRegistryUrl);

                }
            }
            catch (OptionException e)
            {
                showError(e);
            }
		}

        private static void showError(OptionException e)
        {
            Console.Write("kevoree-dotnet-model-generator: ");
            Console.WriteLine(e.Message);
            Console.WriteLine("Try `kevoree-dotnet-model-generator --help' for more information.");
        }

        private static void showHelp(OptionSet optionSet)
        {
            Console.WriteLine("Usage: kevoree-dotnet [OPTIONS]+");
            Console.WriteLine("Options:");
            optionSet.WriteOptionDescriptions(Console.Out);
        }
	}
}