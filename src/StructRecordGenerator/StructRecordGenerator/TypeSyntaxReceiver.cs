using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using System;
using System.Collections.Generic;

namespace StructRecordGenerators
{
    [Flags]
    public enum GeneratedTargetTypeKinds
    {
        Class = 1 << 0,
        Struct = 1 << 1,
    }

    /// <summary>
    /// A visitor that collects type syntax nodes marked with at least one attribute.
    /// </summary>
    /// <remarks>
    /// This class should be very efficient.
    /// </remarks>
    internal class TypeSyntaxReceiver : ISyntaxReceiver
    {
        private readonly GeneratedTargetTypeKinds _typeKinds;

        /// <summary>
        /// A list of type marked with any attributes visited during source generation analysis stage.
        /// </summary>
        public List<TypeDeclarationSyntax> Candidates { get; } = new List<TypeDeclarationSyntax>();

        /// <nodoc />
        public TypeSyntaxReceiver(GeneratedTargetTypeKinds typeKinds)
        {
            _typeKinds = typeKinds;
        }

        /// <summary>
        /// Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
        /// </summary>
        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (_typeKinds.HasFlag(GeneratedTargetTypeKinds.Struct) && syntaxNode is StructDeclarationSyntax structDeclarationSyntax && structDeclarationSyntax.AttributeLists.Count > 0)
            {
                Candidates.Add(structDeclarationSyntax);
            }

            if (_typeKinds.HasFlag(GeneratedTargetTypeKinds.Class) && syntaxNode is ClassDeclarationSyntax classDeclarationSyntax && classDeclarationSyntax.AttributeLists.Count > 0)
            {
                Candidates.Add(classDeclarationSyntax);
            }

            if (_typeKinds.HasFlag(GeneratedTargetTypeKinds.Class) && syntaxNode is RecordDeclarationSyntax recordDeclarationSyntax && recordDeclarationSyntax.AttributeLists.Count > 0)
            {
                Candidates.Add(recordDeclarationSyntax);
            }
        }
    }
}
