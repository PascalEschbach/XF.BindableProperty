using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fody;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

public class FieldInitializer {

    public FieldDefinition Field { get; }
    public MethodDefinition Constructor { get; }
    public IEnumerable<Instruction> Instructions { get; }
    public IEnumerable<VariableDefinition> Variables { get; }

    public bool HasVariables => Variables.Any();


    private FieldInitializer( FieldDefinition field, MethodDefinition ctor, IEnumerable<Instruction> instructions ) {
        Field = field;
        Constructor = ctor;
        Instructions = instructions;

        Variables = instructions
            .Where( i => i.Operand is VariableDefinition )
            .Select( i => i.Operand as VariableDefinition )
            .Distinct()
            .ToArray();
    }
    public static FieldInitializer Create( FieldDefinition field, MethodDefinition ctor ) {

        if( field.DeclaringType != ctor.DeclaringType )
            throw new WeavingException( "Field and constructor have differing declaring types!" );

        var assignments = ctor.Body.Instructions
            .Where( i => i.OpCode.Code == Code.Stfld && i.Operand is FieldReference f && f.Name == field.Name )
            .ToArray();

        if( assignments.Count() > 1 )
            throw new WeavingException( "Cannot assign field more than once!" );

        if( !assignments.Any() )
            return null;

        //Pop instructions until we encounter the thisptr
        //This should be save as field/property initializers mustnt access instance members
        var instructions = new Stack<Instruction>( new[] { assignments.First() } );
        do {
            instructions.Push( instructions.Peek().Previous );
        } while( instructions.Peek().OpCode.Code != Code.Ldarg && instructions.Peek().OpCode.Code != Code.Ldarg_0 );

        return new FieldInitializer( field, ctor, instructions.ToArray() );
    }
    public static IEnumerable<FieldInitializer> Create( FieldDefinition field )
        => field.DeclaringType.GetConstructors().Select( ctor => Create( field, ctor ) ).ToArray();


    public void Strip() {

        //Remove instructions
        var il = Constructor.Body.GetILProcessor();
        foreach( var instruction in Instructions )
            il.Remove( instruction );

        //Remove only those variables which arent reused in the remaining instructions (just in case)
        foreach( var variable in Variables.Where( v => !il.Body.Instructions.Any( i => i.Operand == v ) ) )
            il.Body.Variables.Remove( variable );
    }
}