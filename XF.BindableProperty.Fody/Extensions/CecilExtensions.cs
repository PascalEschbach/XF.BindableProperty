using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Mono.Cecil {

    public static class CecilExtensions {

        private static bool HasSameSignature( this MethodDefinition method, params TypeReference[] signature ) {
            if( method.HasGenericParameters )
                return false;

            if( method.Parameters.Count != signature?.Length )
                return false;

            return method.Parameters.Select( ( p, i ) => new { Param = p, Type = signature[i] } ).All( p => p.Param.ParameterType.FullName == p.Type.FullName );
        }
        public static bool HasSameSignature( this MethodDefinition method, TypeReference returnType, params TypeReference[] signature )
            => method.HasSameSignature( signature ) && method.ReturnType?.FullName == returnType?.FullName;

        public static IEnumerable<TypeDefinition> GetInheritanceChain( this TypeDefinition definition ) {
            for( var type = definition; type != null; type = type.BaseType?.Resolve() )
                yield return type;
        }
        public static IEnumerable<InterfaceImplementation> GetAllInterfaces( this TypeDefinition definition )
            => from type in definition.GetInheritanceChain()
               from implementedInterface in type.Interfaces
               select implementedInterface;

        public static bool Implements( this TypeDefinition typeDef, InterfaceImplementation implementedInterface )
            => typeDef.GetAllInterfaces().Where( i => i.InterfaceType.FullName == implementedInterface.InterfaceType.FullName ).Count() > 0;
        public static bool Implements( this TypeDefinition typeDef, TypeDefinition interfaceType )
            => typeDef.GetAllInterfaces().Where( i => i.InterfaceType.FullName == interfaceType.FullName ).Count() > 0;
        public static bool Inherits( this TypeDefinition typeDef, TypeDefinition inheritedType )
            => typeDef.GetInheritanceChain().Where( t => t.FullName == inheritedType.FullName ).Count() > 0;

        public static MethodDefinition GetOrAddStaticConstructor( this TypeDefinition type ) {
            var ctor = type.GetStaticConstructor();
            if( ctor is null ) {
                ctor = new MethodDefinition( 
                    ".cctor",
                    MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.Static, 
                    type.Module.TypeSystem.Void 
                );
                type.Methods.Add( ctor );
                type.IsBeforeFieldInit = false;
            }
            return ctor;
        }


        public static IEnumerable<CustomAttribute> GetAttributes( this ICustomAttributeProvider provider, string fullName )
            => provider.CustomAttributes.GetAttributes( fullName );
        public static IEnumerable<CustomAttribute> GetAttributes( this IEnumerable<CustomAttribute> attributes, string fullName )
            => attributes.Where( attribute => attribute.Constructor.DeclaringType.FullName == fullName );

        public static CustomAttribute GetAttribute( this IEnumerable<CustomAttribute> attributes, string fullName )
            => attributes.FirstOrDefault( attribute => attribute.Constructor.DeclaringType.FullName == fullName );
        public static CustomAttribute GetAttribute( this ICustomAttributeProvider provider, string fullName )
            => provider.CustomAttributes.GetAttribute( fullName );

        public static bool ContainsAttribute( this IEnumerable<CustomAttribute> attributes, string fullName )
            => attributes.Any( attribute => attribute.Constructor.DeclaringType.FullName == fullName );
        public static bool HasAttribute( this ICustomAttributeProvider provider, string fullName )
            => provider != null && provider.CustomAttributes.ContainsAttribute( fullName );


        public static T GetValue<T>( this CustomAttribute attribute, string propertyName, T defaultValue = default ) {
            var value = attribute.Properties.SingleOrDefault( p => p.Name == propertyName ).Argument.Value;
            return value is null ? defaultValue : (T)value;
        }
    }
}
