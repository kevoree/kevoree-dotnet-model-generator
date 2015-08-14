namespace Org.Kevoree.ModelGenerator

open System
open System.Reflection
open Org.Kevoree.Annotation

module ModelGenerator =
    type Conversion = string -> string 
    let generator:Conversion = fun _ -> ""

    let loadDll: string -> unit = fun path ->
        let aaaa = Assembly.LoadFile path
        //let app = AppDomain.CurrentDomain.Load(path)
        let types:Type [] = aaaa.GetTypes ()
        Console.WriteLine types.Length
        let isAComponentType:Object -> bool = fun tp -> true
        let hasTypeComponentType:Type -> bool = fun x -> Array.exists isAComponentType (x.GetCustomAttributes true)
        let componentTypes:Type [] =  Array.filter hasTypeComponentType types
        //let _ = Array.map (fun x -> Console.WriteLine(x.Name.ToString()) ) componentTypes
        let printType:Type -> unit = fun typ -> Console.WriteLine typ
        let _ = Array.map printType componentTypes
        ()
   