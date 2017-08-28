using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Web;

namespace CreateInterfaceImpType {
    public class ApiClient<T> {
        T interfaceInstance;

        public ApiClient() {
            /*interfaceInstance = (T)Activator.CreateInstance(InstanceCreater.CreateInterfaceImpType<T>(new {
                
            }));
            MethodInfo.GetCurrentMethod()*/
        }

        public string AOP() {
            StackTrace stackTrace = new StackTrace();

            var caller = stackTrace.GetFrame(1).GetMethod();//取得呼叫者

            var fields = new Dictionary<string, string>();
            foreach (var p in caller.GetParameters()) {
                fields[p.Name] = interfaceInstance.GetType().GetField("_" + caller.Name + "_" + p.Name, BindingFlags.NonPublic).GetValue(interfaceInstance) as string;
            }

            return Download(
                caller.GetCustomAttribute<RestApiAttribute>().Url,
                caller.GetCustomAttribute<RestApiAttribute>().Method,
                fields);
        }

        public string Download(string url, Methods method, Dictionary<string, string> fields) {
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
