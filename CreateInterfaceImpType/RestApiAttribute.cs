﻿using System;
using System.Collections.Generic;
using System.Text;

namespace CreateInterfaceImpType {
    [AttributeUsage(AttributeTargets.Method)]
    public class RestApiAttribute : Attribute {
        public string Url { get; set; }
        public Methods Method { get; set; }
    }
}
