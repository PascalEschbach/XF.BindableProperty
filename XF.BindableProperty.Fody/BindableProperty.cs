using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fody;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using XF.BindableProperty.Fody;

public enum XFBindingMode {
    Default,
    TwoWay,
    OneWay,
    OneWayToSource,
    OneTime
}

public class BindableProperty {

    public PropertyDefinition Property { get; }
    public TypeReference PropertyType { get; }
    public FieldDefinition BackingField { get; }
    public TypeReference OwningType { get; }

    public XFBindingMode BindingMode { get; }

    public MethodDefinition ValidateValueMethod { get; }
    public MethodDefinition PropertyChangedMethod { get; }
    public MethodDefinition PropertyChangingMethod { get; }
    public MethodDefinition CoerceValueMethod { get; }
    public MethodDefinition DefaultValueCreatorMethod { get; }

    public IEnumerable<FieldInitializer> Initializers { get; }

    public bool IsAutoProperty => BackingField != null;
    public bool IsReadonly => Property.SetMethod is null || !Property.SetMethod.IsPublic;
    public bool HasInitializer => Initializers?.Any() ?? false;


    public BindableProperty( PropertyDefinition property ) {

        Property = property;
        PropertyType = property.Module.ImportReference( property.PropertyType );
        BackingField = property.DeclaringType.Fields.SingleOrDefault( f => f.Name == $"<{property.Name}>k__BackingField" );

        var attribute = property.GetAttribute( WeaverConstants.BindableAttribute );

        BindingMode = attribute.GetValue( WeaverConstants.BindingMode, XFBindingMode.OneWay );

        var typeSystem = property.Module.TypeSystem;
        ValidateValueMethod = ResolveMethod( attribute.GetValue<string>( WeaverConstants.OnValidateValue ), $"OnValidate{property.Name}Value",
            typeSystem.Boolean,
            WeaverTypes.BindableObject, typeSystem.Object );
        PropertyChangedMethod = ResolveMethod( attribute.GetValue<string>( WeaverConstants.OnPropertyChanged ), $"On{property.Name}Changed",
            typeSystem.Void,
            WeaverTypes.BindableObject, typeSystem.Object, typeSystem.Object );
        PropertyChangingMethod = ResolveMethod( attribute.GetValue<string>( WeaverConstants.OnPropertyChanging ), $"On{property.Name}Changing",
            typeSystem.Void,
            WeaverTypes.BindableObject, typeSystem.Object, typeSystem.Object );
        CoerceValueMethod = ResolveMethod( attribute.GetValue<string>( WeaverConstants.OnCoerceValue ), $"OnCoerce{property.Name}Value",
            typeSystem.Object,
            WeaverTypes.BindableObject, typeSystem.Object );
        DefaultValueCreatorMethod = ResolveMethod( attribute.GetValue<string>( WeaverConstants.OnCreateValue ), $"OnCreate{property.Name}Value",
            typeSystem.Object,
            WeaverTypes.BindableObject );

        OwningType = attribute.GetValue<TypeReference>( WeaverConstants.OwningType ) ?? property.DeclaringType;

        Initializers = FieldInitializer.Create( BackingField );
    }

    private MethodDefinition ResolveMethod( string name, string searchName, TypeReference returnType, params TypeReference[] signature ) {
        if( !string.IsNullOrEmpty( name ) )
            return Property.DeclaringType.Methods.SingleOrDefault( m => m.Name == name && m.IsStatic && m.HasSameSignature( returnType, signature ) ) ??
                throw new WeavingException( $"Couldnt find static method {name} within {Property.DeclaringType} matching the required signature!" );

        return Property.DeclaringType.Methods.SingleOrDefault( m => m.Name == searchName && m.IsStatic && m.HasSameSignature( returnType, signature ) );
    }


    public void Weave() {

        if( !IsAutoProperty )
            throw new WeavingException( $"Cannot weave property {Property.FullName} as its not an auto property!" );

        //Weave the static BindableProperty field
        var properties = WeavePropertyField();

        //Weave getter
        if( Property.GetMethod != null ) {
            Property.GetMethod.Body.Instructions.Clear();
            var il = Property.GetMethod.Body.GetILProcessor();

            il.Emit( OpCodes.Ldarg, 0 );
            il.Emit( OpCodes.Ldsfld, properties.field );
            il.Emit( OpCodes.Call, WeaverTypes.GetValue );

            if( PropertyType.IsValueType )
                il.Emit( OpCodes.Unbox_Any, PropertyType );
            else
                il.Emit( OpCodes.Castclass, PropertyType );

            il.Emit( OpCodes.Ret );
        }

        //Weave setter
        if( Property.SetMethod != null ) {
            Property.SetMethod.Body.Instructions.Clear();
            var il = Property.SetMethod.Body.GetILProcessor();

            il.Emit( OpCodes.Ldarg, 0 );
            il.Emit( OpCodes.Ldsfld, IsReadonly ? properties.key : properties.field );
            il.Emit( OpCodes.Ldarg, 1 );
            if( PropertyType.IsValueType )
                il.Emit( OpCodes.Box, PropertyType );

            il.Emit( OpCodes.Call, IsReadonly ? WeaverTypes.SetReadonlyValue : WeaverTypes.SetValue );

            il.Emit( OpCodes.Ret );
        }


        //Remove attribute & backing field
        Property.CustomAttributes.Remove( Property.GetAttribute( WeaverConstants.BindableAttribute ) );

        //Remove backing field and strip initializers from constructor
        Property.DeclaringType.Fields.Remove( BackingField );
        foreach( var initializer in Initializers )
            initializer.Strip();
    }
    private (FieldDefinition field, FieldDefinition key) WeavePropertyField() {

        //Add static property field
        var propertyField = new FieldDefinition( Property.Name + "Property", FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.InitOnly, WeaverTypes.BindableProperty );
        propertyField.CustomAttributes.Add( new CustomAttribute( WeaverTypes.CompilerGeneratedAttributeConstructor ) );
        Property.DeclaringType.Fields.Add( propertyField );

        FieldDefinition keyField = null;
        if( IsReadonly ) {
            keyField = new FieldDefinition( Property.Name + "PropertyKey", FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.InitOnly, WeaverTypes.BindablePropertyKey );
            keyField.CustomAttributes.Add( new CustomAttribute( WeaverTypes.CompilerGeneratedAttributeConstructor ) );
            Property.DeclaringType.Fields.Add( keyField );
        }

        //Weave static property field construction
        var cctor = Property.DeclaringType.GetOrAddStaticConstructor();

        //if( !cctor.Body.Instructions.Any( x => x.OpCode == OpCodes.Stsfld && x.Operand is FieldDefinition && ( x.Operand as FieldDefinition ).Name == ( Property.Name + "Property" ) ) ) {
        //    return;

        var il = cctor.Body.GetILProcessor();

        if( cctor.Body.Instructions.Count > 0 && cctor.Body.Instructions.Last().OpCode == OpCodes.Ret )
            il.Remove( cctor.Body.Instructions.Last() );

        //BindableProperty Create( string propertyName, Type returnType, Type declaringType, object defaultValue = null, BindingMode defaultBindingMode = BindingMode.OneWay, ValidateValueDelegate validateValue = null, BindingPropertyChangedDelegate propertyChanged = null, BindingPropertyChangingDelegate propertyChanging = null, CoerceValueDelegate coerceValue = null, CreateDefaultValueDelegate defaultValueCreator = null )
        //string propertyName
        il.Emit( OpCodes.Ldstr, Property.Name );
        //Type returnType
        il.Emit( OpCodes.Ldtoken, PropertyType );
        il.Emit( OpCodes.Call, WeaverTypes.GetTypeFromHandle );
        //Type declaringType
        il.Emit( OpCodes.Ldtoken, OwningType );
        il.Emit( OpCodes.Call, WeaverTypes.GetTypeFromHandle );
        //object defaultValue = null
        EmitDefaultValue( il );
        //BindingMode defaultBindingMode = BindingMode.OneWay
        il.Emit( OpCodes.Ldc_I4, (int)BindingMode );

        //ValidateValueDelegate validateValue = null
        EmitDelegate( il, WeaverTypes.ValidateValueDelegate.Resolve(), ValidateValueMethod );
        //BindingPropertyChangedDelegate propertyChanged = null
        EmitDelegate( il, WeaverTypes.BindingPropertyChangedDelegate.Resolve(), PropertyChangedMethod );
        //BindingPropertyChangingDelegate propertyChanging = null
        EmitDelegate( il, WeaverTypes.BindingPropertyChangingDelegate.Resolve(), PropertyChangingMethod );
        //CoerceValueDelegate coerceValue = null
        EmitDelegate( il, WeaverTypes.CoerceValueDelegate.Resolve(), CoerceValueMethod );
        //CreateDefaultValueDelegate defaultValueCreator = null
        EmitDelegate( il, WeaverTypes.CreateDefaultValueDelegate.Resolve(), DefaultValueCreatorMethod );

        //Property = BindableProperty.Create( ... )
        il.Emit( OpCodes.Call, IsReadonly ? WeaverTypes.CreateReadonly : WeaverTypes.Create );
        il.Emit( OpCodes.Stsfld, IsReadonly ? keyField : propertyField );

        //Assign property field from key if readonly
        if( IsReadonly ) {
            il.Emit( OpCodes.Ldsfld, keyField );
            il.Emit( OpCodes.Call, WeaverTypes.GetBindablePropertyFromKey );
            il.Emit( OpCodes.Stsfld, propertyField );
        }

        il.Emit( OpCodes.Ret );

        cctor.Body.Optimize();

        return ( propertyField, keyField );
    }
    private void EmitDelegate( ILProcessor il, TypeDefinition delegateType, MethodDefinition method ) {
        il.Emit( OpCodes.Ldnull );
        if( method is null )
            return;

        il.Emit( OpCodes.Ldftn, method );
        il.Emit( OpCodes.Newobj, Property.Module.ImportReference( delegateType.GetConstructors().Single() ) );
    }
    private void EmitDefaultValue( ILProcessor il ) {

        if( !HasInitializer ) {
            DefaultValueGenerator.EmitDefaultValue( il, PropertyType );
            return;
        }
        
        //Emit field initializer from constructor
        var initializer = Initializers.First();
        var instructions = initializer.Instructions.Skip( 1 ).Copy();

        if( initializer.HasVariables ) {
            il.Body.InitLocals = true;
            foreach( var variable in instructions.CopyVariables() )
                il.Body.Variables.Add( variable );
        }

        foreach( var instruction in instructions )
            il.Append( instruction );

        //Replace stfld with a nop (redirect all potential jumps aswell)
        il.ReplaceAndUpdateJumps( instructions.Last(), Instruction.Create( OpCodes.Nop ) );
    }
}