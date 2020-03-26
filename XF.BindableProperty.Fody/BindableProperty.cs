using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fody;
using Mono.Cecil;

public partial class ModuleWeaver {

	public enum XFBindingMode {
		Default,
		TwoWay,
		OneWay,
		OneWayToSource,
		OneTime
	}

	public class BindableProperty {

		public PropertyDefinition Property { get; } 
		public FieldDefinition BackingField { get; }		
		public TypeDefinition OwningType { get; }

		public XFBindingMode BindingMode { get; }

		public MethodDefinition ValidateValueMethod { get; }
		public MethodDefinition PropertyChangedMethod { get; }
		public MethodDefinition PropertyChangingMethod { get; }
		public MethodDefinition CoerceValueMethod { get; }
		public MethodDefinition DefaultValueCreatorMethod { get; }


		public bool IsAuto => BackingField != null;
		public bool IsReadonly => Property.SetMethod is null;


		public BindableProperty( PropertyDefinition property, ModuleWeaver weaver ) {

			Property = property;
			BackingField = property.DeclaringType.Fields.SingleOrDefault( f => f.Name == $"<{property.Name}>k__BackingField" );

			var attribute = property.GetAttribute( BINDABLE_ATTRIBUTE_NAME );
			BindingMode = (XFBindingMode)attribute.ConstructorArguments[0].Value;

			ValidateValueMethod = ResolveMethod( attribute.ConstructorArguments[1].Value as string, $"OnValidate{property.Name}Value", 
				SystemTypes.BoolDef, 
				WeavingTypes.BindableObjectDef, SystemTypes.ObjectDef );
			PropertyChangedMethod = ResolveMethod( attribute.ConstructorArguments[2].Value as string, $"On{property.Name}Changed", 
				SystemTypes.VoidDef, 
				WeavingTypes.BindableObjectDef, SystemTypes.ObjectDef, SystemTypes.ObjectDef );
			PropertyChangingMethod = ResolveMethod( attribute.ConstructorArguments[3].Value as string, $"On{property.Name}Changing",
				SystemTypes.VoidDef, 
				WeavingTypes.BindableObjectDef, SystemTypes.ObjectDef, SystemTypes.ObjectDef );
			CoerceValueMethod = ResolveMethod( attribute.ConstructorArguments[4].Value as string, $"OnCoerce{property.Name}",
				SystemTypes.ObjectDef, 
				WeavingTypes.BindableObjectDef, SystemTypes.ObjectDef );
			DefaultValueCreatorMethod = ResolveMethod( attribute.ConstructorArguments[5].Value as string, $"OnCreate{property.Name}Value",
				SystemTypes.ObjectDef,
				WeavingTypes.BindableObjectDef );

			OwningType = ( attribute.ConstructorArguments[6].Value as TypeReference )?.Resolve() ?? property.DeclaringType;
		}

		private MethodDefinition ResolveMethod( string name, string searchName, TypeDefinition returnType, params TypeDefinition[] signature ) {
			if( !string.IsNullOrEmpty( name ) )
				return Property.DeclaringType.Methods.SingleOrDefault( m => m.Name == name && m.IsStatic && m.HasSameSignature( returnType, signature ) ) ??
					throw new WeavingException( $"Couldnt find method {name} within {Property.DeclaringType}!" );

			return Property.DeclaringType.Methods.SingleOrDefault( m => m.Name == searchName && m.IsStatic && m.HasSameSignature( returnType, signature ) );
		}
	}
}
