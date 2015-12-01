using System;

namespace Org.Kevoree.ModelGenerator
{
    public class ModelGenerator
    {
        private static void Main(string[] args)
        {

            var options = new CommandLineOptions();

            if (CommandLine.Parser.Default.ParseArguments(args, options))
            {
                /*
                 * Since it's the only "business" validation needed we keep it simple
                 */
                var componentloaded = new NugetLoader.NugetLoader(options.NugetLocalRepositoryPath).LoadRunnerFromPackage<Runner>(options.PackageName, options.PackageVersion, options.NugetRepositoryUrl);
                if (componentloaded != null)
                {
                    try
                    {
						componentloaded.AnalyseAndPublish(options.TypeDefName, options.TypeDefVersion, options.TypeDefPackage, options.PackageName, options.PackageVersion, options.OutputPath);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error while send the model :\n" + e);
                    }
                }
                else {
                    Console.WriteLine(options.PackageName + ":" + options.PackageVersion + " failed to load.");
                }
            }
        }
    }
}