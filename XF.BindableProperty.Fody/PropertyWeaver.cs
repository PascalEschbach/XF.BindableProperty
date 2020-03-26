using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fody;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

public partial class ModuleWeaver {

	private void WeaveProperty( BindableProperty propertyInfo ) {

		var property = propertyInfo.Property;
        var propertyType = ModuleDefinition.ImportReference( property.PropertyType );

        //Remove attribute
        property.CustomAttributes.Remove( propertyInfo.Property.GetAttribute( Constants.BindableAttribute ) );

        //Remove backing field
        property.DeclaringType.Fields.Remove( propertyInfo.BackingField );

		//Find static constructor
		var staticConstructorFlags = MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.Static;
		var staticConstructor = property.DeclaringType.Methods.FirstOrDefault( x => x.Name == ".cctor" && x.Attributes.HasFlag( staticConstructorFlags ) );
		if( staticConstructor is null ) {
			staticConstructor = new MethodDefinition( ".cctor", MethodAttributes.Private | staticConstructorFlags, ModuleDefinition.TypeSystem.Void );
			property.DeclaringType.Methods.Add( staticConstructor );
		}
		property.DeclaringType.IsBeforeFieldInit = false;

		//Add static property field
		var propertyField = new FieldDefinition( property.Name + "Property", FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.InitOnly, WeavingTypes.BindablePropertyRef );
		propertyField.CustomAttributes.Add( new CustomAttribute( ModuleDefinition.ImportReference( SystemTypes.CompilerGeneratedAttributeConstructorDef ) ) );
		property.DeclaringType.Fields.Add( propertyField );

        //Weave static property field construction
        if( !staticConstructor.Body.Instructions.Any( x => x.OpCode == OpCodes.Stsfld && x.Operand is FieldDefinition && ( x.Operand as FieldDefinition ).Name == ( property.Name + "Property" ) ) ) {
            var il = staticConstructor.Body.GetILProcessor();
            if( staticConstructor.Body.Instructions.Count > 0 && staticConstructor.Body.Instructions.Last().OpCode == OpCodes.Ret )
                il.Remove( staticConstructor.Body.Instructions.Last() );

            //BindableProperty Create( string propertyName, Type returnType, Type declaringType, object defaultValue = null, BindingMode defaultBindingMode = BindingMode.OneWay, ValidateValueDelegate validateValue = null, BindingPropertyChangedDelegate propertyChanged = null, BindingPropertyChangingDelegate propertyChanging = null, CoerceValueDelegate coerceValue = null, CreateDefaultValueDelegate defaultValueCreator = null )
            //string propertyName
            il.Emit( OpCodes.Ldstr, propertyField.Name );
            //Type returnType
            il.Emit( OpCodes.Ldtoken, propertyType );
            il.Emit( OpCodes.Call, ModuleDefinition.ImportReference( SystemTypes.GetTypeFromHandleDef ) );
            //Type declaringType
            il.Emit( OpCodes.Ldtoken, ModuleDefinition.ImportReference( propertyInfo.OwningType ) );
            il.Emit( OpCodes.Call, ModuleDefinition.ImportReference( SystemTypes.GetTypeFromHandleDef ) );
            //object defaultValue = null
            HandleDefaultValue( propertyInfo, il );
            //BindingMode defaultBindingMode = BindingMode.OneWay
            il.Emit( OpCodes.Ldc_I4, (int)propertyInfo.BindingMode );

            //ValidateValueDelegate validateValue = null
            EmitDelegate( il, WeavingTypes.ValidateValueDelegateRef.Resolve(), propertyInfo.ValidateValueMethod );
            //BindingPropertyChangedDelegate propertyChanged = null
            EmitDelegate( il, WeavingTypes.BindingPropertyChangedDelegateRef.Resolve(), propertyInfo.PropertyChangedMethod );
            //BindingPropertyChangingDelegate propertyChanging = null
            EmitDelegate( il, WeavingTypes.BindingPropertyChangingDelegateRef.Resolve(), propertyInfo.PropertyChangingMethod );
            //CoerceValueDelegate coerceValue = null
            EmitDelegate( il, WeavingTypes.CoerceValueDelegateRef.Resolve(), propertyInfo.CoerceValueMethod );
            //CreateDefaultValueDelegate defaultValueCreator = null
            EmitDelegate( il, WeavingTypes.CreateDefaultValueDelegateRef.Resolve(), propertyInfo.DefaultValueCreatorMethod );

            //Property = BindableProperty.Create( ... )
            il.Emit( OpCodes.Call, WeavingTypes.CreateRef );
            il.Emit( OpCodes.Stsfld, propertyField );

            il.Emit( OpCodes.Ret );

            staticConstructor.Body.Optimize();
        }

        //Weave getter
        if( property.GetMethod != null ) {
            property.GetMethod.Body.Instructions.Clear();
            var il = property.GetMethod.Body.GetILProcessor();

            il.Emit( OpCodes.Ldarg, 0 );
            il.Emit( OpCodes.Ldsfld, propertyField );
            il.Emit( OpCodes.Call, WeavingTypes.GetValueRef );

            if( propertyType.IsValueType ) 
                il.Emit( OpCodes.Unbox_Any, propertyType );
            else 
                il.Emit( OpCodes.Castclass, propertyType );

            il.Emit( OpCodes.Ret );
        }

        //Weave setter
        if( property.SetMethod != null ) {
			property.SetMethod.Body.Instructions.Clear();
			var il = property.SetMethod.Body.GetILProcessor();

            il.Emit( OpCodes.Ldarg, 0 );
            il.Emit( OpCodes.Ldsfld, propertyField );
            il.Emit( OpCodes.Ldarg, 1 );
            if( propertyType.IsValueType )
                il.Emit( OpCodes.Box, propertyType );

            il.Emit( OpCodes.Call, WeavingTypes.SetValueRef );

            il.Emit( OpCodes.Ret );
        }
	}

    private void EmitDelegate( ILProcessor il, TypeDefinition delegateType, MethodDefinition method ) {
        il.Emit( OpCodes.Ldnull );
        if( method is null )
            return;

        il.Emit( OpCodes.Ldftn, method );
        il.Emit( OpCodes.Newobj, ModuleDefinition.ImportReference( delegateType.GetConstructors().Single() ) );
    }

    private void HandleDefaultValue( BindableProperty property, ILProcessor il ) {

        var constructors = property.Property.DeclaringType.GetConstructors();
        if( !constructors.Any()) {
            EmitDefaultValue( property.Property, il );
            return;
        }

        Instruction[] initialization = null;
        
        foreach( var ctor in constructors ) {

            var assignments = ctor.Body.Instructions
                .Where( i => i.OpCode.Code == Code.Stfld && i.Operand is FieldReference field && field.Name == property.BackingField.Name )
                .ToArray();
                 
            if( assignments.Count() > 1 )
                throw new WeavingException( "Cannot assign field more than once!" );

            if( !assignments.Any() )
                continue;

            //Pop instructions until we encounter the thisptr
            //This should be save as field/property initializers mustnt access instance members
            var instructions = new List<Instruction>();
            var cursor = assignments.First();
            do {
                instructions.Insert( 0, cursor );
                cursor = cursor.Previous;
            } while( cursor != null && cursor.OpCode.Code != Code.Ldarg_0 && cursor.OpCode.Code != Code.Ldarg_1 );

            //If there is a conditional, the jump point will point to the stfld instruction which gets stripped
            //As such we create a nop and reroute all jumps to the stfld IL to the new nop
            var nop = Instruction.Create( OpCodes.Nop );
            instructions.Insert( instructions.Count - 1, nop );
            foreach( var instruction in instructions.Where( i => i.Operand is Instruction jumpPoint && jumpPoint == instructions.Last() ) )
                instruction.Operand = nop;

            //We remove all instructions from the constructor, including the thisptr (which isnt consumed by the previous while loop)
            ctor.Body.Instructions.Remove( instructions.First().Previous ); //Pop thisptr
            foreach( var instruction in instructions )
                ctor.Body.Instructions.Remove( instruction );

            //Remove trailing stfld as we only use the actual construction of the argument
            initialization = instructions.Take( instructions.Count - 1 ).ToArray();
        }

        if( initialization is null )
            EmitDefaultValue( property.Property, il );
        else {
            foreach( var instruction in initialization )
                il.Append( instruction );
        }
    }

    private void EmitDefaultValue( PropertyDefinition property, ILProcessor il ) {

        var propertyType = ModuleDefinition.ImportReference( property.PropertyType );

        if( !propertyType.IsValueType || propertyType.Name.StartsWith( "Nullable`1" ) ) {
            il.Emit( OpCodes.Ldnull );

        } else {
            if( propertyType.FullName == "System.Byte" ) {
                il.Emit( OpCodes.Ldc_I4_0 );
                il.Emit( OpCodes.Conv_U1 );
            } else if( propertyType.FullName == "System.Int16" || propertyType.FullName == "System.UInt16" ) {
                il.Emit( OpCodes.Ldc_I4_0 );
                il.Emit( OpCodes.Conv_U2 );
            } else if( propertyType.FullName == "System.Int32" || propertyType.FullName == "System.UInt32" ) {
                il.Emit( OpCodes.Ldc_I4_0 );
            } else if( propertyType.FullName == "System.Int64" || propertyType.FullName == "System.UInt64" ) {
                il.Emit( OpCodes.Ldc_I4_0 );
                il.Emit( OpCodes.Conv_I8 );
            } else if( propertyType.FullName == "System.Single" ) {
                il.Emit( OpCodes.Ldc_R4, 0f );
            } else if( propertyType.FullName == "System.Double" ) {
                il.Emit( OpCodes.Ldc_R8, 0d );
            } else {
                il.Body.InitLocals = true;
                var localVar = new VariableDefinition( propertyType );
                il.Body.Variables.Add( localVar );
                il.Emit( OpCodes.Ldloca_S, localVar );
                il.Emit( OpCodes.Initobj, propertyType );
                il.Emit( OpCodes.Ldloc_0 );
            }

            il.Emit( OpCodes.Box, propertyType );
        }
    }
}
