using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace CreateInterfaceImpType {
    public interface IExample {
        string WriteLine(string str);
    }
    class Program {
        static void Main(string[] args) {
            var type = CreateInterfaceImpType<IExample>(new {
                WriteLine = (Func<IExample, string, string>)((IExample THIS, string str) => {
                    Console.WriteLine(THIS.GetType().Name + ", " + str);
                    return "";
                }),
                abc = 13
            });

            var instance = (IExample)Activator.CreateInstance(type);

            var gg = instance.WriteLine("XuPeiYao");

            Console.ReadKey();
        }

        public static Type CreateInterfaceImpType<T>(object impObj) {
            return CreateInterfaceImpType(typeof(T), impObj);
        }

        public static Type CreateInterfaceImpType(Type type, object impObj) {
            if (!type.IsInterface) throw new ArgumentException("必須是interface");

            //建構組件
            AssemblyBuilder tempAssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName() {
                Name = "TempAssembly"
            }, AssemblyBuilderAccess.RunAndCollect);

            //建構模組
            ModuleBuilder tempModuleBuilder = tempAssemblyBuilder.DefineDynamicModule("TempModule");

            //建構實作介面類型
            TypeBuilder tempTypeBuilder = tempModuleBuilder.DefineType(
                $"Anon_{Guid.NewGuid().ToString().Replace("-", "_")}",
                TypeAttributes.Class,
                typeof(object), new Type[] { type });

            //實作介面
            tempTypeBuilder.AddInterfaceImplementation(type);

            Dictionary<string, string> fieldList = new Dictionary<string, string>();

            foreach (var method in type.GetMethods()) {
                //產生委派欄位用以儲存引動方法內容
                var tempField = tempTypeBuilder.DefineField(
                    "_methodDelegate_" + method.Name,
                    typeof(Delegate),
                    FieldAttributes.Private | FieldAttributes.Static);

                fieldList.Add(method.Name, tempField.Name);

                var parameters = method.GetParameters().Select(x => x.ParameterType).ToArray();

                //建構方法
                MethodBuilder tempMethodBuilder =
                    tempTypeBuilder.DefineMethod(method.Name,
                    MethodAttributes.Public | MethodAttributes.Virtual,
                    method.ReturnType, parameters);

                //使用IL產生器
                ILGenerator il = tempMethodBuilder.GetILGenerator();

                il.Emit(OpCodes.Ldarg_0); //this
                il.Emit(OpCodes.Ldfld, tempField); //this.tempField

                //#region new object[parameters.Length + 1]{this, parameters...};
                il.Emit(OpCodes.Ldc_I4, parameters.Length + 1);
                il.Emit(OpCodes.Newarr, typeof(object));

                //設定object[]元素
                for (int i = 0; i < parameters.Length + 1; i++) {
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Ldc_I4, i);//索引值
                    il.Emit(OpCodes.Ldarg, i);//方法參數 i==0...為this
                    il.Emit(OpCodes.Stelem_Ref);//REF
                }
                //#endregion

                if (method.ReturnType == typeof(void)) {
                    //this.tempField(new object[parameters.Length + 1]{this, parameters...});
                    il.Emit(OpCodes.Call, typeof(Delegate).GetMethod("DynamicInvoke"));
                    il.Emit(OpCodes.Pop);//remove stack index-0
                    //return;
                    il.Emit(OpCodes.Ret);
                } else {
                    //this.tempField(new object[parameters.Length + 1]{this, parameters...});
                    il.Emit(OpCodes.Callvirt, typeof(Delegate).GetMethod("DynamicInvoke"));

                    //return this.tempField(new object[parameters.Length + 1]{this, parameters...});
                    il.Emit(OpCodes.Ret);
                }

                tempTypeBuilder.DefineMethodOverride(
                    tempMethodBuilder,
                    method
                );
            }

            Type result = tempTypeBuilder.CreateType();

            foreach (var field in fieldList.Keys) {
                result.GetField(fieldList[field], BindingFlags.NonPublic | BindingFlags.Static)
                    .SetValue(null, impObj.GetType().GetProperty(field).GetValue(impObj));
            }




            return result;
        }
    }
}