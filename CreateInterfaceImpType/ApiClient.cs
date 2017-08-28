using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Web;

namespace CreateInterfaceImpType {

    public class ApiClient {

        public static T Create<T>() {
            return (T)Activator.CreateInstance(ImpInterface<T>());
        }

        public static Type ImpInterface(Type type) {
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

            foreach (var method in type.GetMethods()) {
                var parameters = method.GetParameters();

                //建構方法
                MethodBuilder tempMethodBuilder =
                    tempTypeBuilder.DefineMethod(method.Name,
                    MethodAttributes.Public | MethodAttributes.Virtual,
                    method.ReturnType, parameters.Select(x => x.ParameterType).ToArray());

                //使用IL產生器
                ILGenerator il = tempMethodBuilder.GetILGenerator();
                il.EmitThis();
                for (int i = 0; i < parameters.Length; i++) {
                    il.Emit(OpCodes.Ldarg, i + 1);
                    var tempField = tempTypeBuilder.DefineField(
                        $"_{method.Name}_{i}",
                        parameters[i].ParameterType,
                        FieldAttributes.Private);

                    il.Emit(OpCodes.Stfld, tempField);
                }

                il.EmitThis();
                il.Emit(OpCodes.Call, typeof(MethodBase).GetMethod("GetCurrentMethod", BindingFlags.Public | BindingFlags.Static));
                il.Emit(OpCodes.Call, typeof(ApiClient).GetMethod("IMP_AOP", BindingFlags.Public | BindingFlags.Static));
                il.EmitReturn();

                tempTypeBuilder.DefineMethodOverride(
                    tempMethodBuilder,
                    method
                );
            }

            return tempTypeBuilder.CreateType();
        }

        public static Type ImpInterface<T>() {
            return ImpInterface(typeof(T));
        }

        public static string IMP_AOP(object instance, MethodBase caller) {
            var interfaceType = caller.DeclaringType.GetInterfaces()[0];
            caller = interfaceType.GetMethod(caller.Name, BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, caller.GetParameters().Select(x => x.ParameterType).ToArray(), null);

            var fields = new Dictionary<string, string>();
            foreach (ParameterInfo p in caller.GetParameters()) {
                fields[p.GetCustomAttribute<RestApiParamAttribute>().Field ?? p.Name] = instance.GetType().GetField("_" + caller.Name + "_" + p.Position, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(instance).ToString();
            }

            return Download(
                caller.GetCustomAttribute<RestApiAttribute>().Url,
                caller.GetCustomAttribute<RestApiAttribute>().Method,
                fields);
        }

        public static string Download(string url, Methods method, Dictionary<string, string> fields) {
            if (method != Methods.GET) throw new NotSupportedException("本範例只支援GET");

            if (url.Contains("?")) {
                url += "&";
            } else {
                url += "?";
            }
            url += string.Join("&", fields.Select(x => x.Key + "=" + Uri.EscapeDataString(x.Value)));

            HttpClient client = new HttpClient();
            return client.GetStringAsync(url).GetAwaiter().GetResult();
        }

    }
}
