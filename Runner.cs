using Org.Kevoree.Annotation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Registration;
using Org.Kevoree.Core.Api;

namespace Org.Kevoree.ModelGenerator.Nuget
{
    public class Runner : MarshalByRefObject
    {
        private CompositionContainer container;
        private DirectoryCatalog directoryCatalog;
        private IEnumerable<DeployUnit> exports;
        private AppDomain domain;

        public void DoWorkInShadowCopiedDomain(string pluginPath)
        {
            // Use RegistrationBuilder to set up our MEF parts.
            var regBuilder = new RegistrationBuilder();
            regBuilder.ForTypesDerivedFrom<DeployUnit>().Export<DeployUnit>();

            var catalog = new AggregateCatalog();
            catalog.Catalogs.Add(new AssemblyCatalog(typeof(Runner).Assembly, regBuilder));
            directoryCatalog = new DirectoryCatalog(@"C:\Users\mleduc\Desktop\NUGET\org-kevoree-yield-version\7.0.0\Plugins", regBuilder);
            catalog.Catalogs.Add(directoryCatalog);

            container = new CompositionContainer(catalog);
            container.ComposeExportedValue(container);

            // Get our exports available to the rest of Program.
            exports = container.GetExportedValues<DeployUnit>();
            Console.WriteLine("{0} exports in AppDomain {1}", exports.Count(), AppDomain.CurrentDomain.FriendlyName);
        }

        public void Recompose()
        {
            // Gimme 3 steps...
            directoryCatalog.Refresh();
            container.ComposeParts(directoryCatalog.Parts);
            exports = container.GetExportedValues<DeployUnit>();
        }

        public void DoSomething()
        {
            // Tell our MEF parts to do something.
            // TODO ici scan

            var deployUnit = exports.ToList()[0];
            foreach (var a in deployUnit.GetType().GetMethods())
            {
                Console.WriteLine(a.ToString());
                System.Attribute[] attrs = System.Attribute.GetCustomAttributes(a);

                foreach (var b in attrs)
                {
                    Console.WriteLine(">> " + b.ToString());
                }

            }
        }

        public void setDomain(AppDomain domain)
        {
            this.domain = domain;
        }

        public AppDomain getDomain()
        {
            return this.domain;
        }

       

       public List<DeployUnit> ListDeployUnits()
        {
            return exports.ToList();
        }
    }
}
