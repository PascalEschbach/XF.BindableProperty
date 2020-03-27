using System;
using System.Collections.Generic;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace XF.BindableProperty.Fody {
    
    public static class DefaultValueGenerator {

        public static void EmitDefaultValue( ILProcessor il, TypeReference importedType ) {

            if( !importedType.IsValueType || importedType.Name.StartsWith( "Nullable`1" ) ) {
                il.Emit( OpCodes.Ldnull );

            } else {
                if( importedType.FullName == "System.Byte" ) {
                    il.Emit( OpCodes.Ldc_I4_0 );
                    il.Emit( OpCodes.Conv_U1 );
                } else if( importedType.FullName == "System.Int16" || importedType.FullName == "System.UInt16" ) {
                    il.Emit( OpCodes.Ldc_I4_0 );
                    il.Emit( OpCodes.Conv_U2 );
                } else if( importedType.FullName == "System.Int32" || importedType.FullName == "System.UInt32" ) {
                    il.Emit( OpCodes.Ldc_I4_0 );
                } else if( importedType.FullName == "System.Int64" || importedType.FullName == "System.UInt64" ) {
                    il.Emit( OpCodes.Ldc_I4_0 );
                    il.Emit( OpCodes.Conv_I8 );
                } else if( importedType.FullName == "System.Single" ) {
                    il.Emit( OpCodes.Ldc_R4, 0f );
                } else if( importedType.FullName == "System.Double" ) {
                    il.Emit( OpCodes.Ldc_R8, 0d );
                } else {
                    il.Body.InitLocals = true;
                    var localVar = new VariableDefinition( importedType );
                    il.Body.Variables.Add( localVar );
                    il.Emit( OpCodes.Ldloca_S, localVar );
                    il.Emit( OpCodes.Initobj, importedType );
                    il.Emit( OpCodes.Ldloc, localVar.Index );
                }

                il.Emit( OpCodes.Box, importedType );
            }
        }
    }
}
