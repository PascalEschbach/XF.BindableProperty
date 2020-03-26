using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Rocks;

public static class SystemTypes {

	private static ModuleWeaver __weaver;

	public static TypeDefinition VoidDef => __weaver.FindTypeDefinition( typeof( void ).Name );
	public static TypeDefinition BoolDef => __weaver.FindTypeDefinition( typeof( bool ).Name );
	public static TypeDefinition ByteDef => __weaver.FindTypeDefinition( typeof( byte ).Name );
	public static TypeDefinition SByteDef => __weaver.FindTypeDefinition( typeof( sbyte ).Name );
	public static TypeDefinition CharDef => __weaver.FindTypeDefinition( typeof( char ).Name );
	public static TypeDefinition ShortDef => __weaver.FindTypeDefinition( typeof( short ).Name );
	public static TypeDefinition UShortDef => __weaver.FindTypeDefinition( typeof( ushort ).Name );
	public static TypeDefinition IntDef => __weaver.FindTypeDefinition( typeof( int ).Name );
	public static TypeDefinition UIntDef => __weaver.FindTypeDefinition( typeof( uint ).Name );
	public static TypeDefinition LongDef => __weaver.FindTypeDefinition( typeof( long ).Name );
	public static TypeDefinition ULongDef => __weaver.FindTypeDefinition( typeof( ulong ).Name );
	public static TypeDefinition FloatDef => __weaver.FindTypeDefinition( typeof( float ).Name );
	public static TypeDefinition DoubleDef => __weaver.FindTypeDefinition( typeof( double ).Name );
	public static TypeDefinition IntPtrDef => __weaver.FindTypeDefinition( typeof( IntPtr ).Name );
	public static TypeDefinition UIntPtrDef => __weaver.FindTypeDefinition( typeof( UIntPtr ).Name );

	public static TypeDefinition TypeDef => __weaver.FindTypeDefinition( "Type" );

	public static TypeDefinition ObjectDef => __weaver.FindTypeDefinition( typeof( object ).Name );
	public static TypeDefinition StringDef => __weaver.FindTypeDefinition( typeof( string ).Name );
	public static TypeDefinition EnumDef => __weaver.FindTypeDefinition( typeof( Enum ).Name );

	public static TypeDefinition CompilerGeneratedAttributeDef => __weaver.FindTypeDefinition( typeof( CompilerGeneratedAttribute ).Name );
	public static MethodDefinition CompilerGeneratedAttributeConstructorDef => CompilerGeneratedAttributeDef.GetConstructors().Single();

	public static TypeDefinition RuntimeTypeHandleDef => __weaver.FindTypeDefinition( typeof( RuntimeTypeHandle ).Name );
	public static MethodDefinition GetTypeFromHandleDef => TypeDef.Methods.Single( m => m.Name == nameof(Type.GetTypeFromHandle) );


	public static void Initialize( ModuleWeaver weaver )
		=> __weaver = weaver;
}
