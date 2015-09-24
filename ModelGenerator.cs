using System;
using System.Reflection;
using Org.Kevoree.Annotation;
using System.Collections.Generic;
using System.Linq;
using Org.Kevoree.Log;
using org.kevoree;
using org.kevoree.factory;


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

		public ContainerRoot Analyze (string path)
		{
			var assembly = Assembly.LoadFile (path);
			var assemblyTypes = assembly.GetTypes ();
			KevoreeFactory kevoreeFactory = new DefaultKevoreeFactory ();
			ContainerRoot containerRoot = kevoreeFactory.createContainerRoot ();
			kevoreeFactory.root (containerRoot);

			var filteredAssemblyTypes = assemblyTypes.Where ((x) => FilterByAttribute (x, EXPECTED_TYPES)).ToList ();
			if (filteredAssemblyTypes.Count () == 0) {
				log.Error ("None of the expected types have been found (Component, Channel, Node Group)");
			} else if (filteredAssemblyTypes.Count () == 1) {
				// A type found (nominal)
				log.Info (string.Format ("Class {0} found", filteredAssemblyTypes [0].ToString ()));
				var typeDefintion = GetTypeDefinition (filteredAssemblyTypes [0], EXPECTED_TYPES);
				if (typeDefintion == typeof(Org.Kevoree.Annotation.ComponentType)) {
					log.Debug ("Component type");
				} else if (typeDefintion == typeof(Org.Kevoree.Annotation.ChannelType)) {
					log.Debug("Channel type");
				} else if (typeDefintion == typeof(Org.Kevoree.Annotation.NodeType)) {
					log.Debug("Node type");
				} else if (typeDefintion == typeof(Org.Kevoree.Annotation.GroupType)) {
					log.Debug("Group type");
				}
			} else {
				// To many types found
				log.Error ("Too many class with expected types have been found (Component, Channel, Node Group)");
			}

			return containerRoot;
		}
	}

}