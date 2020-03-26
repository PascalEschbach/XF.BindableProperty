using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fody;
using Mono.Cecil;
using Mono.Cecil.Rocks;

public static class WeavingTypes {

	public static TypeDefinition BindableObjectDef { get; private set; }
	public static TypeReference BindableObjectRef{ get; private set; }

	public static TypeDefinition BindablePropertyDef{ get; private set; }
	public static TypeReference BindablePropertyRef{ get; private set; }

	public static MethodReference CreateRef{ get; private set; }
	public static MethodReference CreateAttachedRef{ get; private set; }
	public static MethodReference CreateReadonlyRef{ get; private set; }
	public static MethodReference CreateAttachedReadonlyRef{ get; private set; }
	public static MethodReference SetValueRef{ get; private set; }
	public static MethodReference GetValueRef{ get; private set; }

	public static TypeReference BindingModeRef{ get; private set; }
	public static TypeReference ValidateValueDelegateRef{ get; private set; }
	public static TypeReference BindingPropertyChangedDelegateRef{ get; private set; }
	public static TypeReference BindingPropertyChangingDelegateRef{ get; private set; }
	public static TypeReference CoerceValueDelegateRef{ get; private set; }
	public static TypeReference CreateDefaultValueDelegateRef{ get; private set; }


	public static void Initialize( ModuleWeaver weaver ) {

		BindingModeRef = weaver.ModuleDefinition.ImportReference( weaver.FindTypeDefinition( "BindingMode" ) ?? throw new WeavingException( "Couldnt find binding mode type!" ) );


		BindableObjectDef = weaver.FindTypeDefinition( "BindableObject" ) ?? throw new WeavingException( "Couldnt find bindable object type!" );
		BindableObjectRef = weaver.ModuleDefinition.ImportReference( BindableObjectDef );

		SetValueRef = weaver.ModuleDefinition.ImportReference( BindableObjectDef.Methods.Single( m => m.Name == "SetValue" && m.IsPublic && m.Parameters.First().ParameterType.Name == "BindableProperty" ) );
		GetValueRef = weaver.ModuleDefinition.ImportReference( BindableObjectDef.Methods.Single( m => m.Name == "GetValue" && m.IsPublic && m.Parameters.First().ParameterType.Name == "BindableProperty" ) );


		BindablePropertyDef = weaver.FindTypeDefinition( "BindableProperty" ) ?? throw new WeavingException( "Couldnt find bindable property type!" );
		BindablePropertyRef = weaver.ModuleDefinition.ImportReference( BindablePropertyDef );

		CreateRef = weaver.ModuleDefinition.ImportReference( BindablePropertyDef.Methods.Single( m => m.Name == "Create" && m.IsPublic && !m.HasGenericParameters ) );
		CreateAttachedRef = weaver.ModuleDefinition.ImportReference( BindablePropertyDef.Methods.Single( m => m.Name == "CreateAttached" && m.IsPublic && !m.HasGenericParameters ) );
		CreateReadonlyRef = weaver.ModuleDefinition.ImportReference( BindablePropertyDef.Methods.Single( m => m.Name == "CreateReadOnly" && m.IsPublic && !m.HasGenericParameters ) );
		CreateAttachedReadonlyRef = weaver.ModuleDefinition.ImportReference( BindablePropertyDef.Methods.Single( m => m.Name == "CreateAttachedReadOnly" && m.IsPublic && !m.HasGenericParameters ) );
		
		ValidateValueDelegateRef = weaver.ModuleDefinition.ImportReference( BindablePropertyDef.NestedTypes.Single( t => t.Name == "ValidateValueDelegate" && !t.HasGenericParameters ) );
		BindingPropertyChangedDelegateRef = weaver.ModuleDefinition.ImportReference( BindablePropertyDef.NestedTypes.Single( t => t.Name == "BindingPropertyChangedDelegate" && !t.HasGenericParameters ) );
		BindingPropertyChangingDelegateRef = weaver.ModuleDefinition.ImportReference( BindablePropertyDef.NestedTypes.Single( t => t.Name == "BindingPropertyChangingDelegate" && !t.HasGenericParameters ) );
		CoerceValueDelegateRef = weaver.ModuleDefinition.ImportReference( BindablePropertyDef.NestedTypes.Single( t => t.Name == "CoerceValueDelegate" && !t.HasGenericParameters ) );
		CreateDefaultValueDelegateRef = weaver.ModuleDefinition.ImportReference( BindablePropertyDef.NestedTypes.Single( t => t.Name == "CreateDefaultValueDelegate" && !t.HasGenericParameters ) );
	}
}
