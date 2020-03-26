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

            var attribute = property.GetAttribute( Constants.BindableAttribute );

            BindingMode = attribute.GetValue( Constants.BindingMode, XFBindingMode.OneWay );

            ValidateValueMethod = ResolveMethod( attribute.GetValue<string>( Constants.OnValidateValue ), $"OnValidate{property.Name}Value",
                SystemTypes.BoolDef,
                WeavingTypes.BindableObjectDef, SystemTypes.ObjectDef );
            PropertyChangedMethod = ResolveMethod( attribute.GetValue<string>( Constants.OnPropertyChanged ), $"On{property.Name}Changed",
                SystemTypes.VoidDef,
                WeavingTypes.BindableObjectDef, SystemTypes.ObjectDef, SystemTypes.ObjectDef );
            PropertyChangingMethod = ResolveMethod( attribute.GetValue<string>( Constants.OnPropertyChanging ), $"On{property.Name}Changing",
                SystemTypes.VoidDef,
                WeavingTypes.BindableObjectDef, SystemTypes.ObjectDef, SystemTypes.ObjectDef );
            CoerceValueMethod = ResolveMethod( attribute.GetValue<string>( Constants.OnCoerceValue ), $"OnCoerce{property.Name}Value",
                SystemTypes.ObjectDef,
                WeavingTypes.BindableObjectDef, SystemTypes.ObjectDef );
            DefaultValueCreatorMethod = ResolveMethod( attribute.GetValue<string>( Constants.OnCreateValue ), $"OnCreate{property.Name}Value",
                SystemTypes.ObjectDef,
                WeavingTypes.BindableObjectDef );

            OwningType = attribute.GetValue<TypeReference>( Constants.OwningType )?.Resolve() ?? property.DeclaringType;
        }

        private MethodDefinition ResolveMethod( string name, string searchName, TypeDefinition returnType, params TypeDefinition[] signature ) {
            if( !string.IsNullOrEmpty( name ) )
                return Property.DeclaringType.Methods.SingleOrDefault( m => m.Name == name && m.IsStatic && m.HasSameSignature( returnType, signature ) ) ??
                    throw new WeavingException( $"Couldnt find static method {name} within {Property.DeclaringType} matching the required signature!" );

            return Property.DeclaringType.Methods.SingleOrDefault( m => m.Name == searchName && m.IsStatic && m.HasSameSignature( returnType, signature ) );
        }
    }
}
