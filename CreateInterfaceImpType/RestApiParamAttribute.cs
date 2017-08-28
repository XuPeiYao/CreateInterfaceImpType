using System;
using System.Collections.Generic;
using System.Text;

namespace CreateInterfaceImpType {
    [AttributeUsage(AttributeTargets.Parameter, Inherited = true)]
    public class RestApiParamAttribute : Attribute {
        public string Field { get; set; }
    }
}
