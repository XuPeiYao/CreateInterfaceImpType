using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace CreateInterfaceImpType {
    public interface GoodideaUserApi {
        [RestApi(Url = "http://goodidea.nkfust.edu.tw/api/user/about", Method = Methods.GET)]
        string GetInfo([RestApiParam] string id);
    }
    class Program {
        static void Main(string[] args) {
            var r = ApiClient.Create<GoodideaUserApi>().GetInfo("u0124008@nkfust.edu.tw");

            Console.ReadKey();
        }
    }
}