using org.kevoree;
using org.kevoree.factory;
using Org.Kevoree.Annotation;
using Org.Kevoree.Core.Api;
using Org.Kevoree.Core.Api.IMarshalled;
using Org.Kevoree.Library.Annotation;
using Org.Kevoree.Log;
using Org.Kevoree.Log.Api;
using Org.Kevoree.Registry.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
//using System.ComponentModel.Composition.Registration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Org.Kevoree.ModelGenerator
{
    public class Runner : MarshalByRefObject, IRunner
    {
        private DirectoryCatalog directoryCatalog;

        [ImportMany(typeof(Org.Kevoree.Annotation.DeployUnit))]
        private HashSet<Org.Kevoree.Annotation.DeployUnit> exports;
        private CompositionContainer _container;
        private readonly AnnotationHelper annotationHelper = new AnnotationHelper();

        private ILogger log = new LoggerMaster(Log.Api.Level.Debug, "ModelGenerator");

        private string packageName;
        private string packageVersion;

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

        public void setPackageName(string packageName)
        {
            this.packageName = packageName;
        }

        public void setPackageVersion(string packageVersion)
        {
            this.packageVersion = packageVersion;
        }

        private void Init()
        {
            // Use RegistrationBuilder to set up our MEF parts.
            //var targetPath = Path.Combine(this.pluginPath, packageName + "." + packageVersion);
            var targetPath = this.pluginPath;
            var plugDir = new FileInfo(targetPath).Directory;
            var catalogs = plugDir.GetDirectories("*", SearchOption.AllDirectories).Select(x => new DirectoryCatalog(x.FullName));
            var directoryAggregate = new AggregateCatalog(catalogs);
            _container = new CompositionContainer(directoryAggregate);
            _container.ComposeParts(this);

        }

        internal void AnalyseAndPublish(string typeDefName, string typeDefVersion, string typeDefPackage, string packageName, string packageVersion, string kevoreeRegistryUrl)
        {
            this.Init();

            var result = Analyse(typeDefName, typeDefVersion, typeDefPackage, packageName, packageVersion);

            if (result != null)
            {
                debug(result);

                new RegistryClient(kevoreeRegistryUrl).publishContainerRoot(result).Wait();
            }
            else
            {
                log.Error("Malformed project");
            }

        }

        private static void debug(ContainerRoot result)
        {
            var fact = new DefaultKevoreeFactory();
            var serializer = fact.createJSONSerializer();
            var modelStr = serializer.serialize(result);

            Console.WriteLine(modelStr);
        }

        private ContainerRoot Analyse(string typeDefName, string typeDefVersion, string typeDefPackage, string packageName, string packageVersion)
        {
            KevoreeFactory kevoreeFactory = new DefaultKevoreeFactory();
            ContainerRoot containerRoot = null;


            var filteredAssemblyTypes = exports.Where((x) => annotationHelper.FilterByAttribute(x.GetType(), EXPECTED_TYPES)).ToList();
            if (filteredAssemblyTypes.Count() == 0)
            {
                log.Error("None of the expected types have been found (Component, Channel, Node, Group)");
            }
            else if (filteredAssemblyTypes.Count() == 1)
            {
                containerRoot = kevoreeFactory.createContainerRoot();
                kevoreeFactory.root(containerRoot);
                // A type found (nominal)
                /*
                 * Initialisation a type definition (container root) with information common to all the components types.
                 */
                var typedefinedObject = filteredAssemblyTypes[0];
                TypeDefinition typeDefinitionType = GenericComponentDefinition(typeDefName, typeDefVersion, typeDefPackage, packageName, packageVersion, kevoreeFactory, containerRoot, typedefinedObject);



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

                if (typeDefinitionType.getDictionaryType() == null)
                {
                    typeDefinitionType.setDictionaryType(kevoreeFactory.createDictionaryType());
                }

                if (typeDefinitionType.getDictionaryType().getAttributes() == null)
                {
                    typeDefinitionType.getDictionaryType().setAttributes(new java.util.ArrayList());
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
            var lstMethodsInfos = this.annotationHelper.filterMethodsByAttribute(component.GetType(), typeof(Input));
            foreach (MethodInfo methodInfo in lstMethodsInfos)
            {
                PortTypeRef providedPortRef = factory.createPortTypeRef();
                providedPortRef.setName(methodInfo.Name);

                var optional = ((Input)methodInfo.GetCustomAttribute(typeof(Input))).Optional ? java.lang.Boolean.TRUE : java.lang.Boolean.FALSE;
                providedPortRef.setOptional(optional);

                type.addProvided(providedPortRef);
            }
        }

        private void CompleteComponentTypeDefinitionOutputs(Annotation.DeployUnit component, org.kevoree.impl.ComponentTypeImpl type, KevoreeFactory factory)
        {
            var lstOutputFields = annotationHelper.filterFieldsByAttribute(component.GetType(), typeof(Output));
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

        private void CompleteComponentTypeDefinitionParams(Annotation.DeployUnit component, org.kevoree.TypeDefinition type, KevoreeFactory factory)
        {
            var lstOutputFields = this.annotationHelper.filterFieldsByAttribute(component.GetType(), typeof(Param));
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



        private TypeDefinition GenericComponentDefinition(string typeDefName, string typeDefVersion, string typeDefPackage, string packageName, string packageVersion, KevoreeFactory kevoreeFactory, ContainerRoot containerRoot, Annotation.DeployUnit typedefinedObject)
        {

            /* création de la deployUnit */

            org.kevoree.DeployUnit du = DeployUnitInit(packageName, packageVersion, kevoreeFactory);

            /* chargement des informations génériques à toutes les type defition */
            var typeDef = initTypeDefAndPackage(typeDefName, typeDefVersion, typeDefPackage, du, containerRoot, kevoreeFactory, typedefinedObject);
            typeDef.setAbstract(java.lang.Boolean.FALSE);
            typeDef.addDeployUnits(du);
            return typeDef;
        }

        private static org.kevoree.DeployUnit DeployUnitInit(string packageName, string packageVersion, KevoreeFactory kevoreeFactory)
        {
            org.kevoree.DeployUnit du = kevoreeFactory.createDeployUnit();
            var platform = kevoreeFactory.createValue();
            platform.setName("platform");
            platform.setValue("dotnet");
            du.addFilters(platform);


            // on garde le même nom et le même numéro de version que ceux du nuget
            du.setName(packageName);
            du.setVersion(packageVersion);
            return du;
        }

        private TypeDefinition initTypeDefAndPackage(string typeDefName, string typeDefVersion, string typeDefPackage, org.kevoree.DeployUnit du, ContainerRoot root, KevoreeFactory factory, Annotation.DeployUnit typedefinedObject)
        {
            string[] packages = Regex.Split(typeDefPackage, "\\.");
            org.kevoree.Package pack = null;
            for (int i = 0; i < packages.Length; i++)
            {
                if (pack == null)
                {
                    pack = root.findPackagesByID(packages[i]);
                    if (pack == null)
                    {
                        pack = (org.kevoree.Package)factory.createPackage().withName(packages[i].ToLower());
                        root.addPackages(pack);
                    }
                }
                else
                {
                    Package packNew = pack.findPackagesByID(packages[i]);
                    if (packNew == null)
                    {
                        packNew = (org.kevoree.Package)factory.createPackage().withName(packages[i].ToLower());
                        pack.addPackages(packNew);
                    }
                    pack = packNew;
                }
            }
            string tdName = packages[packages.Length - 1];
            TypeDefinition foundTD = pack.findTypeDefinitionsByNameVersion(typeDefName, typeDefVersion);
            if (foundTD != null)
            {
                return foundTD;
            }
            else
            {
                var typeDefinitionType = annotationHelper.GetTypeDefinition(typedefinedObject.GetType(), EXPECTED_TYPES);
                TypeDefinition td = (TypeDefinition)factory.create(getModelTypeName(typeDefinitionType));
                td.setVersion(typeDefVersion);
                td.setName(typeDefName);
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

        public bool updateDictionary(IDictionaryAttributeMarshalled attribute, IValueMarshalled value)
        {
            throw new NotImplementedException();
        }
    }
}
