using System;
using System.Text.RegularExpressions;

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
                componentloaded.AnalyseAndPublish(options.TypeDefName, options.TypeDefVersion, options.TypeDefPackage, options.PackageName, options.PackageVersion, options.KevoreeRegistryUrl);
            }
        }
    }
}