using System;
using System.Reflection;
using Org.Kevoree.Annotation;
using System.Collections.Generic;
using System.Linq;


namespace kevoreedotnetmodelgenerator2
{
	public class MyClass
	{
		public MyClass ()
		{
			/*let aaaa = Assembly.LoadFile path
				//let app = AppDomain.CurrentDomain.Load(path)
				let types:Type [] = aaaa.GetTypes ()
				Console.WriteLine types.Length
				let isAComponentType:Object -> bool = fun tp -> tp :? Org.Kevoree.Annotation.ComponentType
				let hasTypeComponentType:Type -> bool = fun x -> Array.exists isAComponentType (x.GetCustomAttributes true)
				let componentTypes:Type [] =  Array.filter hasTypeComponentType types
				//let _ = Array.map (fun x -> Console.WriteLine(x.Name.ToString()) ) componentTypes
				let printType:Type -> unit = fun typ -> Console.WriteLine typ
				let _ = Array.map printType componentTypes*/

		}

		static List<object> filterByTypes (object[] types, Type par)
		{
			var gootypes = new List<object> ();
			foreach (object type in types) {
				if (type.GetType().Equals (par)) {
					gootypes.Add (type);
				}
			}
			return gootypes;
		}

		static bool NewMethod (Type t)
		{
			var arguments = t.GetCustomAttributes (true);
			var gt = filterByTypes (arguments, typeof(ComponentType));
			return gt.Count > 0;
		}

		public void loadDll (string path)
		{
			var assembly = Assembly.LoadFile (path);
			var types = assembly.GetTypes ();
			var gt = types.Where (NewMethod);
			foreach (Type t in gt) {
				Console.WriteLine (t.Name);
			}
		}
	}

}