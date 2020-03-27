using Fody;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class ILProcessorExtension {

	public static void Relink( this ILProcessor processor ) {
		var instructions = processor.Body.Instructions;
		for( int i = 0; i < instructions.Count; i++ ) {
			var instruction = instructions[i];
			if( i > 0 )
				instruction.Previous = instructions[i - 1];
			if( i < instructions.Count - 1 )
				instruction.Next = instructions[i + 1];
		}
	}

	public static void RemoveAndUpdateJumps( this ILProcessor processor, Instruction instruction ) {
		processor.Relink();
		var next = instruction.Next;
		if( next is null ) //This never happens unless you remove the ret
			processor.InsertAfter( instruction, Instruction.Create( OpCodes.Nop ) );

		foreach( var jump in processor.Body.Instructions.Where( il => il.Operand == instruction ) )
			jump.Operand = next;

		foreach( var jump in processor.Body.Instructions.Where( il => il.Operand is Instruction[] && ( il.Operand as Instruction[] ).Contains( instruction ) ) ) {
			var list = jump.Operand as Instruction[];
			for( int i = 0; i < list.Length; i++ )
				if( list[i] == instruction )
					list[i] = next;
		}

		processor.Remove( instruction );
	}

	public static void ReplaceAndUpdateJumps( this ILProcessor processor, Instruction oldIL, Instruction newIL ) {
		processor.Replace( oldIL, newIL );
		oldIL.Replace( newIL );
	}
}
