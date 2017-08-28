using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace CreateInterfaceImpType {
    public class InstanceCreater {
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

                il.EmitThis();
                il.Emit(OpCodes.Ldfld, tempField); //this.tempField

                var initArray = new Dictionary<int, EmitOper>();
                //初始化後立刻設定object[]元素
                for (int i = 0; i < parameters.Length + 1; i++) {
                    initArray.Add(i, new EmitOper() {
                        OpCode = OpCodes.Ldarg,
                        Arg = i
                    });
                }
                il.EmitNewArrayInit<object>(parameters.Length + 1, initArray);


                if (method.ReturnType == typeof(void)) {
                    //this.tempField(new object[parameters.Length + 1]{this, parameters...});
                    il.Emit(OpCodes.Call, typeof(Delegate).GetMethod("DynamicInvoke"));//Dynamic return void
                    il.Emit(OpCodes.Pop);//remove stack index-0
                    //return;
                    il.EmitReturn();
                } else {
                    //this.tempField(new object[parameters.Length + 1]{this, parameters...});
                    il.Emit(OpCodes.Callvirt, typeof(Delegate).GetMethod("DynamicInvoke"));

                    //return this.tempField(new object[parameters.Length + 1]{this, parameters...});
                    il.EmitReturn();
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
