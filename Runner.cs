using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using java.util;
using org.kevoree;
using org.kevoree.factory;
using org.kevoree.impl;
using org.kevoree.pmodeling.api.json;
using Org.Kevoree.Annotation;
using Org.Kevoree.Core.Api;
using Org.Kevoree.Core.Api.IMarshalled;
using Org.Kevoree.Library.Annotation;
using Org.Kevoree.Log;
using Org.Kevoree.Log.Api;
using Org.Kevoree.Registry.Client;
using Boolean = java.lang.Boolean;
using ChannelType = Org.Kevoree.Annotation.ChannelType;
using ComponentType = Org.Kevoree.Annotation.ComponentType;
using DeployUnit = Org.Kevoree.Annotation.DeployUnit;
using GroupType = Org.Kevoree.Annotation.GroupType;
using NodeType = Org.Kevoree.Annotation.NodeType;
using Port = Org.Kevoree.Core.Api.Port;
using Type = System.Type;

namespace Org.Kevoree.ModelGenerator
{
    public class Runner : MarshalByRefObject, IRunner
    {
        // private DirectoryCatalog _directoryCatalog;

        [ImportMany(typeof(DeployUnit))]
        private HashSet<DeployUnit> _exports;
        private CompositionContainer _container;
        private readonly AnnotationHelper _annotationHelper = new AnnotationHelper();

        private ILogger log = new LoggerMaster(Level.Debug, "ModelGenerator");

        private string _packageName;
        private string _packageVersion;

        private readonly Type[] EXPECTED_TYPES = {
			typeof(ComponentType),
			typeof(ChannelType),
			typeof(NodeType),
			typeof(GroupType)
		};

        void IRunner.setPluginPath(string pluginPath)
        {
            this._pluginPath = pluginPath;
        }

        public void setPackageName(string packageName)
        {
            this._packageName = packageName;
        }

        public void setPackageVersion(string packageVersion)
        {
            this._packageVersion = packageVersion;
        }

        private void Init()
        {
            // Use RegistrationBuilder to set up our MEF parts.
            //var targetPath = Path.Combine(this.pluginPath, packageName + "." + packageVersion);
            var targetPath = _pluginPath;
            var plugDir = new FileInfo(targetPath).Directory;
            var catalogs = plugDir.GetDirectories("*", SearchOption.AllDirectories).Select(x => new DirectoryCatalog(x.FullName));
            var directoryAggregate = new AggregateCatalog(catalogs);
            _container = new CompositionContainer(directoryAggregate);
            _container.ComposeParts(this);

        }

        internal void AnalyseAndPublish(string typeDefName, string typeDefVersion, string typeDefPackage, string packageName, string packageVersion, string outputPath)
        {
            Init();

            var result = Analyse(typeDefName, typeDefVersion, typeDefPackage, packageName, packageVersion);

            if (result != null)
            {
                //debug(result);


				var kevoreeRegistryUrl = parseToUrl (outputPath);
				if (kevoreeRegistryUrl != null) {
					new RegistryClient (kevoreeRegistryUrl.ToString()).publishContainerRoot (result).Wait ();
				} else {
					// trying to handle the outPath as a file path and then handling error if it fails.
					try {
						var saver = new JSONModelSerializer();
						string json = saver.serialize(result);
						File.WriteAllText(outputPath, json);
					} catch(UnauthorizedAccessException) {
						Console.WriteLine ("Invalid output path");
					}
				}
            }
            else
            {
                log.Error("Malformed project");
            }

        }
			

		Uri parseToUrl (string uriName)
		{
			Uri uriResult;
			if (Uri.TryCreate (uriName, UriKind.Absolute, out uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps)) {
				return uriResult;
			}
		    return null;
		}

        /*private static void debug(ContainerRoot result)
        {
            var fact = new DefaultKevoreeFactory();
            var serializer = fact.createJSONSerializer();
            var modelStr = serializer.serialize(result);

            Console.WriteLine(modelStr);
        }*/

        private ContainerRoot Analyse(string typeDefName, string typeDefVersion, string typeDefPackage, string packageName, string packageVersion)
        {
            KevoreeFactory kevoreeFactory = new DefaultKevoreeFactory();
            ContainerRoot containerRoot = null;


            var filteredAssemblyTypes = new HashSet<DeployUnit>(_exports).Where(x => _annotationHelper.FilterByAttribute(x.GetType(), EXPECTED_TYPES)).ToList();
            if (!filteredAssemblyTypes.Any())
            {
                log.Error("None of the expected types have been found (Component, Channel, Node, Group)");
            }
            else if (filteredAssemblyTypes.Count > 0)
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
                if (typeDefinitionType is ComponentTypeImpl)
                {
                    log.Debug("Component type");
                    CompleteComponentTypeDefinition(typedefinedObject, (ComponentTypeImpl)typeDefinitionType, kevoreeFactory);
                }
                else if (typeDefinitionType is ChannelTypeImpl)
                {
                    log.Debug("Channel type");
                    CompleteChannelTypeDefinition(typedefinedObject, (ChannelTypeImpl)typeDefinitionType, kevoreeFactory);
                }
                else if (typeDefinitionType is NodeTypeImpl)
                {
                    // nothing to do
                    log.Debug("Node type");
                    CompleteNodeTypeDefinition(typedefinedObject, (NodeTypeImpl)typeDefinitionType, kevoreeFactory);
                }
                else if (typeDefinitionType is GroupTypeImpl)
                {
                    // nothing to do
                    log.Debug("Group type");
                    CompleteGroupTypeDefinition(typedefinedObject, (GroupTypeImpl)typeDefinitionType, kevoreeFactory);
                }

                if (typeDefinitionType.getDictionaryType() == null)
                {
                    typeDefinitionType.setDictionaryType(kevoreeFactory.createDictionaryType());
                }

                if (typeDefinitionType.getDictionaryType().getAttributes() == null)
                {
                    typeDefinitionType.getDictionaryType().setAttributes(new ArrayList());
                }
            }

            return containerRoot;
        }

        private void CompleteChannelTypeDefinition(DeployUnit component, ChannelTypeImpl type, KevoreeFactory factory)
        {
            CompleteComponentTypeDefinitionParams(component, type, factory);
        }

        private void CompleteNodeTypeDefinition(DeployUnit component, NodeTypeImpl type, KevoreeFactory factory)
        {
            CompleteComponentTypeDefinitionParams(component, type, factory);
        }

        private void CompleteGroupTypeDefinition(DeployUnit component, GroupTypeImpl type, KevoreeFactory factory)
        {
            CompleteComponentTypeDefinitionParams(component, type, factory);
        }

        private void CompleteComponentTypeDefinition(DeployUnit component, ComponentTypeImpl type, KevoreeFactory factory)
        {
            CompleteComponentTypeDefinitionOutputs(component, type, factory);
            CompleteComponentTypeDefinitionParams(component, type, factory);
            CompleteComponentTypeDefinitionInput(component, type, factory);
        }

        private void CompleteComponentTypeDefinitionInput(DeployUnit component, ComponentTypeImpl type, KevoreeFactory factory)
        {
            var lstMethodsInfos = _annotationHelper.filterMethodsByAttribute(component.GetType(), typeof(Input));
            foreach (MethodInfo methodInfo in lstMethodsInfos)
            {
                PortTypeRef providedPortRef = factory.createPortTypeRef();
                providedPortRef.setName(methodInfo.Name);

                var optional = ((Input)methodInfo.GetCustomAttribute(typeof(Input))).Optional ? Boolean.TRUE : Boolean.FALSE;
                providedPortRef.setOptional(optional);

                type.addProvided(providedPortRef);
            }
        }

        private void CompleteComponentTypeDefinitionOutputs(DeployUnit component, ComponentTypeImpl type, KevoreeFactory factory)
        {
            var lstOutputFields = _annotationHelper.filterFieldsByAttribute(component.GetType(), typeof(Output));
            foreach (FieldInfo fieldInfo in lstOutputFields)
            {
                PortTypeRef requiredPortRef = CompleteComponentTypeDefinitionOutput(component, factory, fieldInfo);
                type.addRequired(requiredPortRef);
            }
        }

        private static PortTypeRef CompleteComponentTypeDefinitionOutput(DeployUnit component, KevoreeFactory factory, FieldInfo fieldInfo)
        {
            if (fieldInfo.FieldType != typeof(Port))
            {
                throw new TypeDefinitionException("Class=" + component + ", Field=" + fieldInfo + ", annotated with Outbut but type is not Port");
            }
            PortTypeRef requiredPortRef = factory.createPortTypeRef();
            requiredPortRef.setName(fieldInfo.Name);
            System.Boolean opt = ((Output)fieldInfo.GetCustomAttribute(typeof(Output))).Optional;
            requiredPortRef.setOptional(opt ? Boolean.TRUE : Boolean.FALSE);
            return requiredPortRef;
        }

        private void CompleteComponentTypeDefinitionParams(DeployUnit component, TypeDefinition type, KevoreeFactory factory)
        {
            var lstOutputFields = _annotationHelper.filterFieldsByAttribute(component.GetType(), typeof(Param));
            foreach (FieldInfo fieldInfo in lstOutputFields)
            {
                DictionaryAttribute dicAtt = CompleteComponentTypeDefinitionParamField(type, factory, fieldInfo);
                type.getDictionaryType().addAttributes(dicAtt);
            }
        }

        private static DictionaryAttribute CompleteComponentTypeDefinitionParamField(TypeDefinition type, KevoreeFactory factory, FieldInfo fieldInfo)
        {
            var fieldType = fieldInfo.FieldType;
            var name = fieldInfo.Name;
            var paramAttribute = ((Param)fieldInfo.GetCustomAttribute(typeof(Param)));

            return CompleteComponentTypeDefinitionParam(type, factory, fieldType, name, paramAttribute);
        }

        /**
         * Defined a dictionnary attribute for a method or field annotated with a Param attribute
         */
        private static DictionaryAttribute CompleteComponentTypeDefinitionParam(TypeDefinition typeDef, KevoreeFactory factory, Type type, string name, Param paramAttribute)
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
            dicAtt.setOptional(opt ? Boolean.TRUE : Boolean.FALSE);
            dicAtt.setFragmentDependant(fragmentDependent ? Boolean.TRUE : Boolean.FALSE);
            dicAtt.setDefaultValue(defaultValue);
            return dicAtt;
        }

        private static DataType GetDatatypeByType(Type fieldType)
        {
            DataType dataType;
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



        private TypeDefinition GenericComponentDefinition(string typeDefName, string typeDefVersion, string typeDefPackage, string packageName, string packageVersion, KevoreeFactory kevoreeFactory, ContainerRoot containerRoot, DeployUnit typedefinedObject)
        {

            /* création de la deployUnit */

            org.kevoree.DeployUnit du = DeployUnitInit(packageName, packageVersion, kevoreeFactory);

            /* chargement des informations génériques à toutes les type defition */
            var typeDef = InitTypeDefAndPackage(typeDefName, typeDefVersion, typeDefPackage, du, containerRoot, kevoreeFactory, typedefinedObject);
            typeDef.setAbstract(Boolean.FALSE);
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

        private TypeDefinition InitTypeDefAndPackage(string typeDefName, string typeDefVersion, string typeDefPackage, org.kevoree.DeployUnit du, ContainerRoot root, KevoreeFactory factory, DeployUnit typedefinedObject)
        {
            var packages = Regex.Split(typeDefPackage, "\\.");
            Package pack = null;
            foreach (string t in packages)
            {
                if (pack == null)
                {
                    pack = root.findPackagesByID(t);
                    if (pack == null)
                    {
                        pack = (Package)factory.createPackage().withName(t.ToLower());
                        root.addPackages(pack);
                    }
                }
                else
                {
                    var packNew = pack.findPackagesByID(t);
                    if (packNew == null)
                    {
                        packNew = (Package)factory.createPackage().withName(t.ToLower());
                        pack.addPackages(packNew);
                    }
                    pack = packNew;
                }
            }
            var foundTD = pack.findTypeDefinitionsByNameVersion(typeDefName, typeDefVersion);
            if (foundTD != null)
            {
                return foundTD;
            }
            var typeDefinitionType = _annotationHelper.GetTypeDefinition(typedefinedObject.GetType(), EXPECTED_TYPES);
            var td = (TypeDefinition)factory.create(getModelTypeName(typeDefinitionType));
            td.setVersion(typeDefVersion);
            td.setName(typeDefName);
            td.addDeployUnits(du);
            pack.addTypeDefinitions(td);
            pack.addDeployUnits(du);
            return td;
        }

        private string getModelTypeName(Type dotnetType)
        {
            if (dotnetType == typeof(ComponentType))
            {
                return typeof(org.kevoree.ComponentType).ToString();
            }
            if (dotnetType == typeof(ChannelType))
            {
                return typeof(org.kevoree.ChannelType).ToString();
            }
            if (dotnetType == typeof(NodeType))
            {
                return typeof(org.kevoree.NodeType).ToString();
            }
            return dotnetType == typeof(GroupType) ? typeof(org.kevoree.GroupType).ToString() : null;
        }

        private string _pluginPath;

        public bool updateDictionary(IDictionaryAttributeMarshalled attribute, IValueMarshalled value)
        {
            throw new NotImplementedException();
        }
    }
}
