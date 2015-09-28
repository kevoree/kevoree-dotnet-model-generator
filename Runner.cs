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
using Org.Kevoree.Registry.Client;
using System.Reflection;

namespace Org.Kevoree.ModelGenerator
{
    public class Runner : MarshalByRefObject, IRunner
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

        private void Init()
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

        internal void AnalyseAndPublish(string packageName, string packageVersion, string kevoreeRegistryUrl)
        {
            this.Init();

            var result = Analyse(packageName, packageVersion);
            new RegistryClient(kevoreeRegistryUrl).publishContainerRoot(result).Wait();

        }

        private ContainerRoot Analyse(string packageName, string packageVersion)
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
                /*
                 * Initialisation a type definition (container root) with information common to all the components types.
                 */
                var typedefinedObject = filteredAssemblyTypes[0];
                TypeDefinition typeDefinitionType = GenericComponentDefinition(packageName, packageVersion, kevoreeFactory, containerRoot, typedefinedObject);



                /* each of those types inherits from TypeDefinition.
                 Only ChannelType and ComponentType are specialized.
                 NodeType and GroupType inherits from TypeDefinition without any specificity */
                if (typeDefinitionType.GetType() == typeof(org.kevoree.impl.ComponentTypeImpl))
                {
                    log.Debug("Component type");
                    CompleteComponentTypeDefinition(typedefinedObject, (org.kevoree.impl.ComponentTypeImpl)typeDefinitionType, kevoreeFactory, containerRoot);
                }
                else if (typeDefinitionType.GetType() == typeof(org.kevoree.impl.ChannelTypeImpl))
                {
                    log.Debug("Channel type");
                    CompleteChannelTypeDefinition(typedefinedObject, (org.kevoree.impl.ChannelTypeImpl)typeDefinitionType, kevoreeFactory, containerRoot);
                }
                else if (typeDefinitionType.GetType() == typeof(org.kevoree.impl.NodeTypeImpl))
                {
                    // nothing to do
                    log.Debug("Node type");
                    CompleteNodeTypeDefinition(typedefinedObject, (org.kevoree.impl.NodeTypeImpl)typeDefinitionType, kevoreeFactory, containerRoot);
                }
                else if (typeDefinitionType.GetType() == typeof(org.kevoree.impl.GroupTypeImpl))
                {
                    // nothing to do
                    log.Debug("Group type");
                    CompleteGroupTypeDefinition(typedefinedObject, (org.kevoree.impl.GroupTypeImpl)typeDefinitionType, kevoreeFactory, containerRoot);
                }
            }
            else
            {
                // To many types found
                log.Error("Too many class with expected types have been found (Component, Channel, Node Group)");
            }

            return containerRoot;
        }

        private void CompleteChannelTypeDefinition(Annotation.DeployUnit component, org.kevoree.impl.ChannelTypeImpl type, KevoreeFactory factory, ContainerRoot containerRoot)
        {
            CompleteComponentTypeDefinitionParams(component, type, factory);
        }

        private void CompleteNodeTypeDefinition(Annotation.DeployUnit component, org.kevoree.impl.NodeTypeImpl type, KevoreeFactory factory, ContainerRoot containerRoot)
        {
            CompleteComponentTypeDefinitionParams(component, type, factory);
        }

        private void CompleteGroupTypeDefinition(Annotation.DeployUnit component, org.kevoree.impl.GroupTypeImpl type, KevoreeFactory factory, ContainerRoot containerRoot)
        {
            CompleteComponentTypeDefinitionParams(component, type, factory);
        }

        private void CompleteComponentTypeDefinition(Annotation.DeployUnit component, org.kevoree.impl.ComponentTypeImpl type, KevoreeFactory factory, ContainerRoot containerRoot)
        {
            CompleteComponentTypeDefinitionOutputs(component, type, factory);
            CompleteComponentTypeDefinitionParams(component, type, factory);
            CompleteComponentTypeDefinitionInput(component, type, factory);
        }

        private void CompleteComponentTypeDefinitionInput(Annotation.DeployUnit component, org.kevoree.impl.ComponentTypeImpl type, KevoreeFactory factory)
        {
            var lstMethodsInfos = filterMethodsByAttribute(component, typeof(Input));
            foreach (MethodInfo methodInfo in lstMethodsInfos)
            {
                PortTypeRef providedPortRef = factory.createPortTypeRef();
                providedPortRef.setName(methodInfo.Name);
                
                var optional = ((Input)methodInfo.GetCustomAttribute(typeof(Input))).Optional ? java.lang.Boolean.TRUE : java.lang.Boolean.FALSE;
                providedPortRef.setOptional(optional);
                
                type.addProvided(providedPortRef);
            }
        }

        private static void CompleteComponentTypeDefinitionOutputs(Annotation.DeployUnit component, org.kevoree.impl.ComponentTypeImpl type, KevoreeFactory factory)
        {
            var lstOutputFields = filterFieldsByAttribute(component, typeof(Output));
            foreach (FieldInfo fieldInfo in lstOutputFields)
            {
                PortTypeRef requiredPortRef = CompleteComponentTypeDefinitionOutput(component, factory, fieldInfo);
                type.addRequired(requiredPortRef);
            }
        }

        private static PortTypeRef CompleteComponentTypeDefinitionOutput(Annotation.DeployUnit component, KevoreeFactory factory, FieldInfo fieldInfo)
        {
            if (fieldInfo.FieldType != typeof(Org.Kevoree.Core.Api.Port))
            {
                throw new TypeDefinitionException("Class=" + component + ", Field=" + fieldInfo + ", annotated with Outbut but type is not Port");
            }
            PortTypeRef requiredPortRef = factory.createPortTypeRef();
            requiredPortRef.setName(fieldInfo.Name);
            Boolean opt = ((Output)fieldInfo.GetCustomAttribute(typeof(Output))).Optional;
            requiredPortRef.setOptional(opt ? java.lang.Boolean.TRUE : java.lang.Boolean.FALSE);
            return requiredPortRef;
        }

        private static void CompleteComponentTypeDefinitionParams(Annotation.DeployUnit component, org.kevoree.TypeDefinition type, KevoreeFactory factory)
        {
            var lstOutputFields = filterFieldsByAttribute(component, typeof(Param));
            foreach (FieldInfo fieldInfo in lstOutputFields)
            {
                DictionaryAttribute dicAtt = CompleteComponentTypeDefinitionParamField(type, factory, fieldInfo);
                type.getDictionaryType().addAttributes(dicAtt);
            }
        }

        private static DictionaryAttribute CompleteComponentTypeDefinitionParamField(org.kevoree.TypeDefinition type, KevoreeFactory factory, FieldInfo fieldInfo)
        {
            var fieldType = fieldInfo.FieldType;
            var name = fieldInfo.Name;
            var paramAttribute = ((Param)fieldInfo.GetCustomAttribute(typeof(Param)));

            return CompleteComponentTypeDefinitionParam(type, factory, fieldType, name, paramAttribute);
        }

        private static DictionaryAttribute CompleteComponentTypeDefinitionParamMethid(org.kevoree.impl.ComponentTypeImpl type, KevoreeFactory factory, MethodInfo methodInfo)
        {
            var fieldType = methodInfo.ReturnType;
            var name = methodInfo.Name;
            var paramAttribute = ((Param)methodInfo.GetCustomAttribute(typeof(Param)));

            return CompleteComponentTypeDefinitionParam(type, factory, fieldType, name, paramAttribute);
        }

        /**
         * Defined a dictionnary attribute for a method or field annotated with a Param attribute
         */
        private static DictionaryAttribute CompleteComponentTypeDefinitionParam(org.kevoree.TypeDefinition typeDef, KevoreeFactory factory, Type type, string name, Param paramAttribute)
        {
            DataType dataType = GetDatatypeByType(type);
            DictionaryAttribute dicAtt = factory.createDictionaryAttribute();
            if (typeDef.getDictionaryType() == null)
            {
                typeDef.setDictionaryType(factory.createDictionaryType());
            }
            dicAtt.setName(name);
            dicAtt.setDatatype(dataType);
            var opt = paramAttribute.Optional;
            var fragmentDependent = paramAttribute.FragmentDependent;
            var defaultValue = paramAttribute.DefaultValue;
            dicAtt.setOptional(opt ? java.lang.Boolean.TRUE : java.lang.Boolean.FALSE);
            dicAtt.setFragmentDependant(fragmentDependent ? java.lang.Boolean.TRUE : java.lang.Boolean.FALSE);
            dicAtt.setDefaultValue(defaultValue);
            return dicAtt;
        }

        private static DataType GetDatatypeByType(Type fieldType)
        {
            DataType dataType = null;
            if (fieldType == typeof(string))
            {
                dataType = DataType.STRING;
            }
            else if (fieldType == typeof(float))
            {
                dataType = DataType.FLOAT;
            }
            else if (fieldType == typeof(int))
            {
                dataType = DataType.INT;
            }
            else if (fieldType == typeof(double))
            {
                dataType = DataType.DOUBLE;
            }
            else if (fieldType == typeof(bool))
            {
                dataType = DataType.BOOLEAN;
            }
            else if (fieldType == typeof(long))
            {
                dataType = DataType.LONG;
            }
            else if (fieldType == typeof(short))
            {
                dataType = DataType.SHORT;
            }
            else if (fieldType == typeof(char))
            {
                dataType = DataType.CHAR;
            }
            else if (fieldType == typeof(byte))
            {
                dataType = DataType.BYTE;
            }
            else
            {
                throw new Exception("Param annotation is only applicable on field/method of type String,Long,Double,Float,Integer, current " + fieldType);
            }
            return dataType;
        }

        private static IEnumerable<FieldInfo> filterFieldsByAttribute(Annotation.DeployUnit component, Type typeOutput)
        {
            return component.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Where(x => x.GetCustomAttribute(typeOutput) != null);
        }

        private IEnumerable<MethodInfo> filterMethodsByAttribute(Annotation.DeployUnit component, Type type)
        {
            return component.GetType().GetMethods().Where(x => x.GetCustomAttribute(type) != null);
        }

        private TypeDefinition GenericComponentDefinition(string packageName, string packageVersion, KevoreeFactory kevoreeFactory, ContainerRoot containerRoot, Annotation.DeployUnit typedefinedObject)
        {
            log.Info(string.Format("Class {0} found", typedefinedObject.ToString()));

            /* création de la deployUnit */

            org.kevoree.DeployUnit du = kevoreeFactory.createDeployUnit();
            var platform = kevoreeFactory.createValue();
            platform.setName("plateform");
            platform.setValue("dotnet");
            du.addFilters(platform);


            // on garde le même nom et le même numéro de version que ceux du nuget
            du.setName(packageName);
            du.setVersion(packageVersion);

            var typeDefinitionType = GetTypeDefinition(typedefinedObject.GetType(), EXPECTED_TYPES);

            /* chargement des informations génériques à toutes les type defition */
            var typeDef = initTypeDefAndPackage(typedefinedObject.GetType().FullName, du, containerRoot, kevoreeFactory, getModelTypeName(typeDefinitionType));
            typeDef.setAbstract(java.lang.Boolean.FALSE); // TODO : dans un premier temps on ne gere pas l'héritage
            typeDef.addDeployUnits(du);
            return typeDef;
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
