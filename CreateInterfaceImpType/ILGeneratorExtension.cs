using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;

namespace CreateInterfaceImpType {
    public static class ILGeneratorExtension {
        public static void EmitNewArray(this ILGenerator il, Type type, int length) {
            il.Emit(OpCodes.Ldc_I4, length);
            il.Emit(OpCodes.Newarr, type);
        }

        public static void EmitNewArrayInit(this ILGenerator il, Type type, int length, Dictionary<int, EmitOper> mapping) {
            EmitNewArray(il, type, length);
            foreach (var kp in mapping) {
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Ldc_I4, kp.Key);//索引值
                il.Emit(kp.Value.OpCode, (dynamic)kp.Value.Arg);
                il.Emit(OpCodes.Stelem_Ref);//REF
            }
        }

        public static void EmitNewArray<T>(this ILGenerator il, int length) {
            EmitNewArray(il, typeof(T), length);
        }

        public static void EmitNewArrayInit<T>(this ILGenerator il, int length, Dictionary<int, EmitOper> mapping) {
            EmitNewArrayInit(il, typeof(T), length, mapping);
        }

        public static void EmitThis(this ILGenerator il) => il.Emit(OpCodes.Ldarg, 0);

        public static void EmitReturn(this ILGenerator il) => il.Emit(OpCodes.Ret);
    }
}
