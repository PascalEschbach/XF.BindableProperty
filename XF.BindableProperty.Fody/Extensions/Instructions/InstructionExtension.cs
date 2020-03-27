using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fody;
using Mono.Cecil;
using Mono.Cecil.Cil;

public static class InstructionExtension {

	public static Instruction GetHead( this Instruction il )
		=> il.Previous is null ? il : il.Previous.GetHead();
	public static Instruction GetTail( this Instruction il )
		=> il.Next is null ? il : il.Next.GetTail();

	public static IEnumerable<Instruction> GetAll( this Instruction il ) {
		il = il.GetHead();
		do {
			yield return il;
			il = il.Next;
		} while( il != null );
	}


	public static void InsertBefore( this Instruction il, Instruction instruction ) => Insert( il.Previous, il, instruction );
	public static void InsertAfter( this Instruction il, Instruction instruction ) => Insert( il, il.Next, instruction );

	private static void Insert( Instruction prev, Instruction next, Instruction il ) {
		il.Previous = prev;
		il.Next = next;

		if( prev != null )
			prev.Next = il;
		if( next != null )
			next.Previous = il;
	}

	public static void Remove( this Instruction il ) {

		FixJumps( il, null );

		if( il.Previous != null )
			il.Previous.Next = il.Next;
		if( il.Next != null )
			il.Next.Previous = il.Previous;

		il.Next = il.Previous = null;
	}
	public static void Replace( this Instruction il, Instruction instruction ) {

		FixJumps( il, instruction );

		if( il.Previous != null )
			il.Previous.Next = instruction;
		if( il.Next != null )
			il.Next.Previous = instruction;

		instruction.Next = il.Next;
		instruction.Previous = il.Previous;
		instruction.Offset = il.Offset;

		il.Next = il.Previous = null;
	}

	public static void MoveJumps( this Instruction il, Instruction to ) => FixJumps( il, to );
	private static void FixJumps( Instruction oldIL, Instruction newIL ) {
		var all = oldIL.GetAll();
		foreach( var jump in all.Where( il => il.Operand == oldIL ) )
			jump.Operand = newIL ?? throw new WeavingException( $"Cannot remove {oldIL.ToString()} as {jump.ToString()} still depends on it!" );

		foreach( var il in all.Where( il => il.Operand is Instruction[] )) {
			var list = il.Operand as Instruction[];
			for( int i = 0; i < list.Length; i++ )
				if( list[i] == oldIL )
					list[i] = newIL ?? throw new WeavingException( $"Cannot remove {oldIL.ToString()} as {il.ToString()} still depends on it!" );
		}
	}

	public static Instruction[] CopyRange( this Instruction il, Instruction end ) {
		List<Instruction> instructions = new List<Instruction>();
		var cursor = il;
		while( cursor != null ) {
			instructions.Add( cursor.Copy() );
			if( cursor == end )
				break;

			cursor = cursor.Next;
		}

		InstructionLinker.Link( instructions );

		return instructions.ToArray();
	}
	public static Instruction[] CopyAll( this Instruction il )
		=> il.GetHead().CopyRange( il.GetTail());
	public static Instruction Copy( this Instruction il, bool keepLink = false ) {

		Instruction copy = il.Operand switch {
			null => Instruction.Create( il.OpCode ),
			ParameterDefinition p => Instruction.Create( il.OpCode, p ),
			VariableDefinition v => Instruction.Create( il.OpCode, v ),
			Instruction[] i => Instruction.Create( il.OpCode, i ),
			Instruction i => Instruction.Create( il.OpCode, i ),

			float n => Instruction.Create( il.OpCode, n ),
			double n => Instruction.Create( il.OpCode, n ),
			byte n => Instruction.Create( il.OpCode, n ),
			sbyte n => Instruction.Create( il.OpCode, n ),
			int n => Instruction.Create( il.OpCode, n ),
			long n => Instruction.Create( il.OpCode, n ),

			string s => Instruction.Create( il.OpCode, s ),

			FieldReference r => Instruction.Create( il.OpCode, r ),
			MethodReference r => Instruction.Create( il.OpCode, r ),
			TypeReference r => Instruction.Create( il.OpCode, r ),
			CallSite r => Instruction.Create( il.OpCode, r ),

			_ => throw new NotSupportedException()
		};

		if( keepLink ) {
			copy.Next = il.Next;
			copy.Previous = il.Previous;
			copy.Offset = il.Offset;
		}

		return copy;
	}

	public static void Link( this Instruction il, Instruction prev, Instruction next ) {

		if( prev != null )
			prev.Next = il;
		if( next != null )
			next.Previous = il;

		il.Previous = prev;
		il.Next = next;
	}
	public static void Unlink( this Instruction il ) 
		=> il.Previous = il.Next = null;


	public static bool HasJumpSources( this Instruction il ) => il.FindJumpSources().Any();
	public static Instruction[] FindJumpSources( this Instruction il )
		=> il.GetAll()
			.Where( i => i.Operand == il || ( i.Operand is Instruction[] list && list.Any( target => target == il ) ) )
			.Distinct()
			.ToArray();
}

