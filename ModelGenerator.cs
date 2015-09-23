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


namespace Org.Kevoree.ModelGenerator.Test
{
	public class ModelGenerator
	{

		private Log.Log log = LogFactory.getLog (typeof(ModelGenerator).ToString (), Level.DEBUG);

		public ModelGenerator ()
		{
		}

		static List<Tuple<Type, object>> filterByTypes (object[] types, Type[] pars)
		{
			var gootypes = new List<Tuple<Type, object>> ();
			foreach (object type in types) {
				foreach (var par in pars) {
					if (type.GetType ().Equals (par)) {
						gootypes.Add (Tuple.Create (par, type));
					}
				}
			}
			return gootypes;
		}

		static List<Tuple<Type, object>> GetTypeDefinitionSub (Type t, Type[] types)
		{
			return filterByTypes (t.GetCustomAttributes (true), types);
		}

		static bool FilterByAttribute (Type t, Type[] types)
		{
			return GetTypeDefinitionSub (t, types).Count > 0;
		}

		private Type GetTypeDefinition (Type filteredAssemblyTypes, Type[] expectedTypes)
		{
			return GetTypeDefinitionSub (filteredAssemblyTypes, expectedTypes) [0].Item1;
		}

		private readonly Type[] EXPECTED_TYPES = {
			typeof(Org.Kevoree.Annotation.ComponentType),
			typeof(Org.Kevoree.Annotation.ChannelType),
			typeof(Org.Kevoree.Annotation.NodeType),
			typeof(Org.Kevoree.Annotation.GroupType)
		};

		public ContainerRoot Analyze (string name, string version)
		{

            var componentloaded = ComponentLoader.loadComponent(name, version);
			//var assembly = Assembly.LoadFile ("aa");
			//var assemblyTypes = assembly.GetTypes ();
            var assemblyTypes = componentloaded.ListDeployUnits();
			KevoreeFactory kevoreeFactory = new DefaultKevoreeFactory ();
			ContainerRoot containerRoot = kevoreeFactory.createContainerRoot ();
			kevoreeFactory.root (containerRoot);

			var filteredAssemblyTypes = assemblyTypes.Where ((x) => FilterByAttribute (x.GetType(), EXPECTED_TYPES)).ToList ();
			if (filteredAssemblyTypes.Count () == 0) {
				log.Error ("None of the expected types have been found (Component, Channel, Node, Group)");
			} else if (filteredAssemblyTypes.Count () == 1) {
				// A type found (nominal)
                var typedefinedObject = filteredAssemblyTypes [0];
				log.Info (string.Format ("Class {0} found", typedefinedObject.ToString ()));

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
                var typeDef = initTypeDefAndPackage(typedefinedObject.GetType().FullName, du, containerRoot, kevoreeFactory, getTypeName(typeDefinitionType));
                typeDef.setAbstract(java.lang.Boolean.FALSE); // TODO : dans un premier temps on ne gere pas l'héritage
                typeDef.addDeployUnits(du);



                /* each of those types inherits from TypeDefinition.
                 Only ChannelType and ComponentType are specialized.
                 NodeType and GroupType inherits from TypeDefinition without any specificity */
				if (typeDefinitionType == typeof(Org.Kevoree.Annotation.ComponentType)) {
					log.Debug ("Component type");
				} else if (typeDefinitionType == typeof(Org.Kevoree.Annotation.ChannelType)) {
					log.Debug("Channel type");
				} else if (typeDefinitionType == typeof(Org.Kevoree.Annotation.NodeType)) {
                    // nothing to do
					log.Debug("Node type");
				} else if (typeDefinitionType == typeof(Org.Kevoree.Annotation.GroupType)) {
                    // nothing to do
					log.Debug("Group type");
				}
			} else {
				// To many types found
				log.Error ("Too many class with expected types have been found (Component, Channel, Node Group)");
			}

			return containerRoot;
		}

        private string getTypeName(Type dotnetType)
        {
            string ret;
            if (dotnetType == typeof(Org.Kevoree.Annotation.ComponentType))
            {
                return typeof(Org.Kevoree.Annotation.ComponentType).ToString();
            }
            else if (dotnetType == typeof(Org.Kevoree.Annotation.ChannelType))
            {
                return typeof(Org.Kevoree.Annotation.ChannelType).ToString();
            }
            else if (dotnetType == typeof(Org.Kevoree.Annotation.NodeType))
            {
                return typeof(Org.Kevoree.Annotation.NodeType).ToString();
            }
            else if (dotnetType == typeof(Org.Kevoree.Annotation.GroupType))
            {
                return typeof(Org.Kevoree.Annotation.GroupType).ToString();
            }
            else
            {
                ret = null;
            }
            return ret;
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
	}

}