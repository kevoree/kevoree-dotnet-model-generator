using CommandLine;
using CommandLine.Text;

namespace Org.Kevoree.ModelGenerator
{
    class CommandLineOptions
    {
        [Option("package.name", Required = true, HelpText = "Nuget package name.")]
        public string PackageName { get; set; }

        [Option("package.version", Required = true, HelpText = "Nuget package version (format = X.Y.Z).")]
        public string PackageVersion { get; set; }

        [Option("typeDef.name", Required = true, HelpText = "TypeDefinition name.")]
        public string TypeDefName { get; set; }

        [Option("typeDef.version", Required = true, HelpText = "Type definition version (format = X.Y.Z).")]
        public string TypeDefVersion { get; set; }

        [Option("typeDef.package", Required = true, HelpText = "Type definition package.")]
        public string TypeDefPackage { get; set; }

        [Option("nuget.local.repository.path", Required = false, HelpText = "Nuget local repository.")]
        public string NugetLocalRepositoryPath { get; set; }

        [Option("nuget.repository.url", DefaultValue = "https://packages.nuget.org/api/v2", Required = false, HelpText = "Nuget remote repository.")]
        public string NugetRepositoryUrl { get; set; }

        [Option("kevoree.registry.url", DefaultValue = "http://registry.kevoree.org", Required = false, HelpText = "Kevoree remote registry.")]
        public string KevoreeRegistryUrl { get; set; }

        [ParserState]
        public IParserState LastParserState { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this,
              (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}
