using Org.Kevoree.Annotation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Registration;
using Org.Kevoree.Core.Api;
using org.kevoree.factory;
using org.kevoree;
using Org.Kevoree.Log;
using System.Text.RegularExpressions;

namespace Org.Kevoree.ModelGenerator.Nuget
{
    public class Runner : MarshalByRefObject,IRunner
    {
        private DirectoryCatalog directoryCatalog;
        private IEnumerable<Org.Kevoree.Annotation.DeployUnit> exports;
        private CompositionContainer container;

        private Log.Log log = LogFactory.getLog(typeof(Runner).ToString(), Level.DEBUG);

        private readonly Type[] EXPECTED_TYPES = {
			typeof(Org.Kevoree.Annotation.ComponentType),
			typeof(Org.Kevoree.Annotation.ChannelType),
			typeof(Org.Kevoree.Annotation.NodeType),
			typeof(Org.Kevoree.Annotation.GroupType)
		};

        void IRunner.setPluginPath(string pluginPath)
        {
            this.pluginPath = pluginPath;
        }

        public void Init()
        {
            // Use RegistrationBuilder to set up our MEF parts.
            var regBuilder = new RegistrationBuilder();
            regBuilder.ForTypesDerivedFrom<Org.Kevoree.Annotation.DeployUnit>().Export<Org.Kevoree.Annotation.DeployUnit>();

            var catalog = new AggregateCatalog();
            catalog.Catalogs.Add(new AssemblyCatalog(typeof(Runner).Assembly, regBuilder));
            directoryCatalog = new DirectoryCatalog(pluginPath, regBuilder);
            catalog.Catalogs.Add(directoryCatalog);

            container = new CompositionContainer(catalog);
            container.ComposeExportedValue(container);

            // Get our exports available to the rest of Program.
            exports = container.GetExportedValues<Org.Kevoree.Annotation.DeployUnit>();
        }

        static List<Tuple<Type, object>> filterByTypes(object[] types, Type[] pars)
        {
            var gootypes = new List<Tuple<Type, object>>();
            foreach (object type in types)
            {
                foreach (var par in pars)
                {
                    if (type.GetType().Equals(par))
                    {
                        gootypes.Add(Tuple.Create(par, type));
                    }
                }
            }
            return gootypes;
        }

        static List<Tuple<Type, object>> GetTypeDefinitionSub(Type t, Type[] types)
        {
            return filterByTypes(t.GetCustomAttributes(true), types);
        }

        private Type GetTypeDefinition(Type filteredAssemblyTypes, Type[] expectedTypes)
        {
            return GetTypeDefinitionSub(filteredAssemblyTypes, expectedTypes)[0].Item1;
        }

        static bool FilterByAttribute(Type t, Type[] types)
        {
            return GetTypeDefinitionSub(t, types).Count > 0;
        }

       public List<Org.Kevoree.Annotation.DeployUnit> ListDeployUnits()
        {
            return exports.ToList();
        }

       internal void AnalyseAndPublish()
       {
           var result = Analyse();
           var saver = new org.kevoree.pmodeling.api.json.JSONModelSerializer();
           Console.WriteLine(saver.serialize(result));
       }

       private ContainerRoot Analyse()
       {
           var assemblyTypes = this.ListDeployUnits();
           KevoreeFactory kevoreeFactory = new DefaultKevoreeFactory();
           ContainerRoot containerRoot = kevoreeFactory.createContainerRoot();
           kevoreeFactory.root(containerRoot);

           var filteredAssemblyTypes = assemblyTypes.Where((x) => FilterByAttribute(x.GetType(), EXPECTED_TYPES)).ToList();
           if (filteredAssemblyTypes.Count() == 0)
           {
               log.Error("None of the expected types have been found (Component, Channel, Node, Group)");
           }
           else if (filteredAssemblyTypes.Count() == 1)
           {
               // A type found (nominal)
               var typedefinedObject = filteredAssemblyTypes[0];
               log.Info(string.Format("Class {0} found", typedefinedObject.ToString()));

               /* création de la deployUnit */

               org.kevoree.DeployUnit du = kevoreeFactory.createDeployUnit();
               var platform = kevoreeFactory.createValue();
               platform.setName("plateform");
               platform.setValue("dotnet");
               du.addFilters(platform);

               // lire nuspec ?
               // ou alors toute la génération se base sur un packet déjà possé sur nuget en utilisant le code de chargement utilisé dans le core ?
               du.setName("TODO");
               du.setVersion("TODO");

               var typeDefinitionType = GetTypeDefinition(typedefinedObject.GetType(), EXPECTED_TYPES);

               /* chargement des informations génériques à toutes les type defition */
               var typeDef = initTypeDefAndPackage(typedefinedObject.GetType().FullName, du, containerRoot, kevoreeFactory, getModelTypeName(typeDefinitionType));
               typeDef.setAbstract(java.lang.Boolean.FALSE); // TODO : dans un premier temps on ne gere pas l'héritage
               typeDef.addDeployUnits(du);



               /* each of those types inherits from TypeDefinition.
                Only ChannelType and ComponentType are specialized.
                NodeType and GroupType inherits from TypeDefinition without any specificity */
               if (typeDefinitionType == typeof(Org.Kevoree.Annotation.ComponentType))
               {
                   log.Debug("Component type");
               }
               else if (typeDefinitionType == typeof(Org.Kevoree.Annotation.ChannelType))
               {
                   log.Debug("Channel type");
               }
               else if (typeDefinitionType == typeof(Org.Kevoree.Annotation.NodeType))
               {
                   // nothing to do
                   log.Debug("Node type");
               }
               else if (typeDefinitionType == typeof(Org.Kevoree.Annotation.GroupType))
               {
                   // nothing to do
                   log.Debug("Group type");
               }
           }
           else
           {
               // To many types found
               log.Error("Too many class with expected types have been found (Component, Channel, Node Group)");
           }

           return containerRoot;
       }

       private TypeDefinition initTypeDefAndPackage(String name, org.kevoree.DeployUnit du, ContainerRoot root, KevoreeFactory factory, String typeName)
       {
           String[] packages = Regex.Split(name, "\\.");
           if (packages.Length <= 1)
           {
               throw new java.lang.RuntimeException("Component '" + name + "' must be defined in a Java package");
           }
           org.kevoree.Package pack = null;
           for (int i = 0; i < packages.Length - 1; i++)
           {
               if (pack == null)
               {
                   pack = root.findPackagesByID(packages[i]);
                   if (pack == null)
                   {
                       pack = (org.kevoree.Package)factory.createPackage().withName(packages[i]);
                       root.addPackages(pack);
                   }
               }
               else
               {
                   Package packNew = pack.findPackagesByID(packages[i]);
                   if (packNew == null)
                   {
                       packNew = (org.kevoree.Package)factory.createPackage().withName(packages[i]);
                       pack.addPackages(packNew);
                   }
                   pack = packNew;
               }
           }
           String tdName = packages[packages.Length - 1];
           TypeDefinition foundTD = pack.findTypeDefinitionsByNameVersion(tdName, du.getVersion());
           if (foundTD != null)
           {
               return foundTD;
           }
           else
           {
               TypeDefinition td = (TypeDefinition)factory.create(typeName);
               td.setVersion(du.getVersion());
               td.setName(tdName);
               td.addDeployUnits(du);
               pack.addTypeDefinitions(td);
               pack.addDeployUnits(du);
               return td;
           }
       }

       private string getModelTypeName(Type dotnetType)
       {
           string ret;
           if (dotnetType == typeof(Org.Kevoree.Annotation.ComponentType))
           {
               return typeof(org.kevoree.ComponentType).ToString();
           }
           else if (dotnetType == typeof(Org.Kevoree.Annotation.ChannelType))
           {
               return typeof(org.kevoree.ChannelType).ToString();
           }
           else if (dotnetType == typeof(Org.Kevoree.Annotation.NodeType))
           {
               return typeof(org.kevoree.NodeType).ToString();
           }
           else if (dotnetType == typeof(Org.Kevoree.Annotation.GroupType))
           {
               return typeof(org.kevoree.GroupType).ToString();
           }
           else
           {
               ret = null;
           }
           return ret;
       }

       private string pluginPath;
    }
}
