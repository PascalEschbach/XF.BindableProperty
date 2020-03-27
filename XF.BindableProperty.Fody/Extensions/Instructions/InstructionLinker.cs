using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil.Cil;

public static class InstructionLinker {

	public static IEnumerable<Instruction> Copy( IEnumerable<Instruction> instructions ) {

		//Copy body
		var originalBody = instructions.ToList();
		var body = originalBody.Select( il => il.Copy() ).ToArray();

		Link( body );

		//Redirect jumps
		foreach( var instruction in body ) {

			if( instruction.Operand is Instruction jump ) 
				instruction.Operand = body[originalBody.IndexOf( jump )];

			if( instruction.Operand is Instruction[] jumpList )
				instruction.Operand = jumpList.Select( j => body[originalBody.IndexOf( j )] ).ToArray();
		}

		return body;
	}
	public static IEnumerable<Instruction> Link( IEnumerable<Instruction> instructions ) {
		var body = instructions.ToArray();
		for( int i = 0; i < body.Length; i++ ) {
			if( i > 0 )
				body[i].Previous = body[i - 1];
			if( i < body.Length - 1 )
				body[i].Next = body[i + 1];
		}

		return body.ToArray();
	}


	public static Instruction[] Add( this Instruction[] instructions, Instruction newIL ) {
		if( instructions is null || !instructions.Any() )
			return new [] { newIL };

		var copy = new List<Instruction>( instructions ) { newIL }.ToArray();

		copy[copy.Length - 2].Next = newIL;
		newIL.Previous = copy[copy.Length - 2];

		return copy;
	}
	public static Instruction[] Remove( this Instruction[] instructions, Instruction il ) {
		if( instructions is null || !instructions.Any() )
			return instructions;

		var list = new List<Instruction>( instructions );
		if( !list.Remove( il ))
			return list.ToArray();

		if( il.Previous != null )
			il.Previous.Next = il.Next;
		if( il.Next != null )
			il.Next.Previous = il.Previous;

		return list.ToArray();
	}
	public static void Set( this Instruction[] instructions, int index, Instruction il ) {
		var oldIL = instructions[index];

		instructions[index] = il;
		il.Next = oldIL.Next;
		il.Previous = oldIL.Previous;

		if( il.Next != null )
			il.Next.Previous = il;
		if( il.Previous != null )
			il.Previous.Next = il;
	}
	public static int IndexOf( this Instruction[] instructions, Instruction il )
		=> new List<Instruction>( instructions ).IndexOf( il );
}

