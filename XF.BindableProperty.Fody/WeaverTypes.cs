using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Fody;
using Mono.Cecil;
using Mono.Cecil.Rocks;

public static class WeaverTypes {

    public static TypeReference BindableObject { get; private set; }
    public static TypeReference BindableProperty { get; private set; }
    public static TypeReference BindablePropertyKey { get; private set; }

    public static MethodReference Create { get; private set; }
    public static MethodReference CreateAttached { get; private set; }
    public static MethodReference CreateReadonly { get; private set; }
    public static MethodReference CreateAttachedReadonly { get; private set; }
    public static MethodReference SetValue { get; private set; }
    public static MethodReference SetReadonlyValue { get; private set; }
    public static MethodReference GetValue { get; private set; }

    public static TypeReference BindingMode { get; private set; }
    public static TypeReference ValidateValueDelegate { get; private set; }
    public static TypeReference BindingPropertyChangedDelegate { get; private set; }
    public static TypeReference BindingPropertyChangingDelegate { get; private set; }
    public static TypeReference CoerceValueDelegate { get; private set; }
    public static TypeReference CreateDefaultValueDelegate { get; private set; }

    public static TypeReference Type { get; private set; }
    public static TypeReference Enum { get; private set; }

    public static TypeReference CompilerGeneratedAttribute { get; private set; }
    public static MethodReference CompilerGeneratedAttributeConstructor { get; private set; }

    public static TypeReference RuntimeTypeHandle { get; private set; }
    public static MethodReference GetTypeFromHandle { get; private set; }


    public static void Initialize( ModuleWeaver weaver ) {

        Type = weaver.Resolve( nameof( Type ) );
        Enum = weaver.Resolve( nameof( Enum ) );

        CompilerGeneratedAttribute = weaver.Resolve( nameof( CompilerGeneratedAttribute ) );
        CompilerGeneratedAttributeConstructor = weaver.ModuleDefinition.ImportReference( CompilerGeneratedAttribute.Resolve().GetConstructors().Single() );

        RuntimeTypeHandle = weaver.Resolve( nameof( RuntimeTypeHandle ) );
        GetTypeFromHandle = weaver.ModuleDefinition.ImportReference( Type.Resolve().Methods.Single( m => m.Name == nameof( System.Type.GetTypeFromHandle ) ) );

        BindingMode = weaver.Resolve( nameof( BindingMode ) );
        BindableObject = weaver.Resolve( nameof( BindableObject ) );
        BindableProperty = weaver.Resolve( nameof( BindableProperty ) );
        BindablePropertyKey = weaver.Resolve( nameof( BindablePropertyKey ) );

        SetValue = weaver.ModuleDefinition.ImportReference( BindableObject.Resolve().Methods.Single( m => m.Name == "SetValue" && m.IsPublic && m.Parameters.First().ParameterType.Name == "BindableProperty" ) );
        SetReadonlyValue = weaver.ModuleDefinition.ImportReference( BindableObject.Resolve().Methods.Single( m => m.Name == "SetValue" && m.IsPublic && m.Parameters.First().ParameterType.Name == "BindablePropertyKey" ) );
        GetValue = weaver.ModuleDefinition.ImportReference( BindableObject.Resolve().Methods.Single( m => m.Name == "GetValue" && m.IsPublic && m.Parameters.First().ParameterType.Name == "BindableProperty" ) );

        Create = weaver.ModuleDefinition.ImportReference( BindableProperty.Resolve().Methods.Single( m => m.Name == "Create" && m.IsPublic && !m.HasGenericParameters ) );
        CreateAttached = weaver.ModuleDefinition.ImportReference( BindableProperty.Resolve().Methods.Single( m => m.Name == "CreateAttached" && m.IsPublic && !m.HasGenericParameters ) );
        CreateReadonly = weaver.ModuleDefinition.ImportReference( BindableProperty.Resolve().Methods.Single( m => m.Name == "CreateReadOnly" && m.IsPublic && !m.HasGenericParameters ) );
        CreateAttachedReadonly = weaver.ModuleDefinition.ImportReference( BindableProperty.Resolve().Methods.Single( m => m.Name == "CreateAttachedReadOnly" && m.IsPublic && !m.HasGenericParameters ) );

        ValidateValueDelegate = weaver.ModuleDefinition.ImportReference( BindableProperty.Resolve().NestedTypes.Single( t => t.Name == "ValidateValueDelegate" && !t.HasGenericParameters ) );
        BindingPropertyChangedDelegate = weaver.ModuleDefinition.ImportReference( BindableProperty.Resolve().NestedTypes.Single( t => t.Name == "BindingPropertyChangedDelegate" && !t.HasGenericParameters ) );
        BindingPropertyChangingDelegate = weaver.ModuleDefinition.ImportReference( BindableProperty.Resolve().NestedTypes.Single( t => t.Name == "BindingPropertyChangingDelegate" && !t.HasGenericParameters ) );
        CoerceValueDelegate = weaver.ModuleDefinition.ImportReference( BindableProperty.Resolve().NestedTypes.Single( t => t.Name == "CoerceValueDelegate" && !t.HasGenericParameters ) );
        CreateDefaultValueDelegate = weaver.ModuleDefinition.ImportReference( BindableProperty.Resolve().NestedTypes.Single( t => t.Name == "CreateDefaultValueDelegate" && !t.HasGenericParameters ) );
    }
    private static TypeReference Resolve( this ModuleWeaver weaver, string typename ) 
        => weaver.ModuleDefinition.ImportReference( weaver.FindTypeDefinition( typename ) ?? throw new WeavingException( $"Couldnt find {typename}!" ) );
}
