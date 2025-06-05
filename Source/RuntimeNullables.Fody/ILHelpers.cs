﻿using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace RuntimeNullables.Fody;

internal static class ILHelpers
{
    /// <summary>
    /// Converts the value on the stack to a value ref which can be used to test for null.
    /// </summary>
    public static void InsertGetValueRefAndThrowHelperCallIfValueRefIsNull(
        ref int index,
        Collection<Instruction> instructions,
        TypeReference valueType,
        MethodReference throwHelperMethod,
        string paramValue,
        Instruction continuationPoint,
        ReturnBlockInfo? returnBlockInfo = null)
    {
        var elementType = valueType.GetElementType();

        if (elementType.IsGenericParameter) {
            if (valueType.IsByReference) {
                instructions.Insert(index++, Instruction.Create(OpCodes.Ldobj, elementType));
                instructions.Insert(index++, Instruction.Create(OpCodes.Box, elementType));
            }
            else {
                instructions.Insert(index++, Instruction.Create(OpCodes.Ldind_Ref));
            }
        }
        else if (valueType.IsByReference) {
            instructions.Insert(index++, Instruction.Create(OpCodes.Ldind_Ref));
        }

        var branchIfNotNull = Instruction.Create(OpCodes.Brtrue_S, continuationPoint);
        returnBlockInfo?.Branches.Add(branchIfNotNull);
        instructions.Insert(index++, branchIfNotNull);

        instructions.Insert(index++, Instruction.Create(OpCodes.Ldstr, paramValue));
        instructions.Insert(index++, Instruction.Create(OpCodes.Call, throwHelperMethod));
    }
}