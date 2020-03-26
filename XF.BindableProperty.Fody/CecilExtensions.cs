using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Mono.Cecil {

	public static class CecilExtensions {

		private static bool HasSameSignature( this MethodDefinition method, params TypeDefinition[] signature ) {
			if( method.HasGenericParameters )
				return false;

			if( method.Parameters.Count != signature?.Length )
				return false;

			return method.Parameters.Select( ( p, i ) => (p, signature[i]) ).All( p => p.Item1.ParameterType.FullName == p.Item2.FullName );
		}
		public static bool HasSameSignature( this MethodDefinition method, TypeDefinition returnType, params TypeDefinition[] signature )
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


		public static string GetName( this PropertyDefinition propertyDefinition ) 
			=> $"{propertyDefinition.DeclaringType.FullName}.{propertyDefinition.Name}";

		public static bool IsCallToMethod( this Instruction instruction, string methodName, out int propertyNameIndex ) {
			propertyNameIndex = 1;
			if( !instruction.OpCode.IsCall() ) {
				return false;
			}
			if( !( instruction.Operand is MethodReference methodReference ) ) {
				return false;
			}
			if( methodReference.Name != methodName ) {
				return false;
			}
			var parameterDefinition = methodReference.Parameters.FirstOrDefault( x => x.Name == "propertyName" );
			if( parameterDefinition != null ) {
				propertyNameIndex = methodReference.Parameters.Count - parameterDefinition.Index;
			}

			return true;
		}

		public static bool IsCall( this OpCode opCode ) =>  opCode.Code == Code.Call || opCode.Code == Code.Callvirt;

		public static FieldReference GetGeneric( this FieldDefinition definition ) {
			if( definition.DeclaringType.HasGenericParameters ) {
				var declaringType = new GenericInstanceType( definition.DeclaringType );
				foreach( var parameter in definition.DeclaringType.GenericParameters ) {
					declaringType.GenericArguments.Add( parameter );
				}
				return new FieldReference( definition.Name, definition.FieldType, declaringType );
			}

			return definition;
		}

		public static MethodReference GetGeneric( this MethodReference reference ) {
			if( reference.DeclaringType.HasGenericParameters ) {
				var declaringType = new GenericInstanceType( reference.DeclaringType );
				foreach( var parameter in reference.DeclaringType.GenericParameters ) {
					declaringType.GenericArguments.Add( parameter );
				}
				var methodReference = new MethodReference( reference.Name, reference.MethodReturnType.ReturnType, declaringType );
				foreach( var parameterDefinition in reference.Parameters ) {
					methodReference.Parameters.Add( parameterDefinition );
				}
				methodReference.HasThis = reference.HasThis;
				return methodReference;
			}

			return reference;
		}

		public static MethodReference MakeGeneric( this MethodReference method, params TypeReference[] args ) {
			if( args.Length == 0 )
				return method;

			if( method.GenericParameters.Count != args.Length )
				throw new ArgumentException( "Invalid number of generic type arguments supplied" );

			var genericTypeRef = new GenericInstanceMethod( method );
			foreach( var arg in args )
				genericTypeRef.GenericArguments.Add( arg );

			return genericTypeRef;
		}
		public static IEnumerable<CustomAttribute> GetAllCustomAttributes( this TypeDefinition typeDefinition ) {
			foreach( var attribute in typeDefinition.CustomAttributes )
				yield return attribute;

			if( typeDefinition.BaseType != null ) {
				var def = typeDefinition.BaseType as TypeDefinition;
				if( def == null )
					def = typeDefinition.BaseType.Resolve();

				foreach( var attr in def.GetAllCustomAttributes() )
					yield return attr;
			}

		}

		public static IEnumerable<CustomAttribute> GetAttributes( this IEnumerable<CustomAttribute> attributes, string attributeName ) 
			=> attributes.Where( attribute => attribute.Constructor.DeclaringType.FullName == attributeName );

		public static CustomAttribute GetAttribute( this IEnumerable<CustomAttribute> attributes, string attributeName ) 
			=> attributes.FirstOrDefault( attribute => attribute.Constructor.DeclaringType.FullName == attributeName );

		public static bool ContainsAttribute( this IEnumerable<CustomAttribute> attributes, string attributeName ) 
			=> attributes.Any( attribute => attribute.Constructor.DeclaringType.FullName == attributeName );

		public static bool HasAttribute( this ICustomAttributeProvider provider, string attrName )
			=> provider != null && provider.CustomAttributes.ContainsAttribute( attrName );
		public static CustomAttribute GetAttribute( this ICustomAttributeProvider provider, string attrName )
			=> provider.CustomAttributes.GetAttribute( attrName );
		public static IEnumerable<CustomAttribute> GetAttributes( this ICustomAttributeProvider provider, string attrName )
			=> provider.CustomAttributes.GetAttributes( attrName );

		public static bool HasAttribute( this ICustomAttributeProvider provider, TypeDefinition attrDef )
			=> provider != null && provider.CustomAttributes.FirstOrDefault( ( c ) => c.AttributeType.Resolve() == attrDef.Resolve() ) != null;
		public static CustomAttribute GetAttribute( this ICustomAttributeProvider provider, TypeDefinition attrDef )
			=> provider != null ? provider.CustomAttributes.FirstOrDefault( ( c ) => c.AttributeType.Resolve() == attrDef.Resolve() ) : null;


		public static T GetValue<T>( this CustomAttribute attribute, string propertyName, T defaultValue = default ) {
			var value = attribute.Properties.SingleOrDefault( p => p.Name == propertyName ).Argument.Value;
			return value is null ? defaultValue : (T)value;
		}

			public static bool HasReturnType( this MethodReference method )
			=> method.ReturnType.Resolve().Name != "Void";

	}
}
