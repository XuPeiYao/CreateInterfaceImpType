using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace CreateInterfaceImpType {
    public interface UserApi {
        [RestApi(Url = "xxxx", Method = Methods.GET)]
        string GetName([RestApiParam(Field = "userId")] string id);
    }
    class Program {
        static void Main(string[] args) {


            Console.ReadKey();
        }
    }
}