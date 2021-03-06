﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class PartialTypeDocumentationCommentTests : CSharpTestBase
    {
        private readonly CSharpCompilation compilation;
        private readonly NamedTypeSymbol fooClass;

        public PartialTypeDocumentationCommentTests()
        {
            var tree1 = Parse(
                @"
/// <summary>Summary on first file's Foo.</summary>
partial class Foo
{
    /// <summary>Summary on MethodWithNoImplementation.</summary>
    partial void MethodWithNoImplementation();

    /// <summary>Summary in file one which should be shadowed.</summary>
    partial void ImplementedMethodWithNoSummaryOnImpl();

    partial void ImplementedMethod();
}", options: TestOptions.RegularWithDocumentationComments);

            var tree2 = Parse(
                @"
/// <summary>Summary on second file's Foo.</summary>
partial class Foo
{
    /// <remarks>Foo.</remarks>
    partial void ImplementedMethodWithNoSummaryOnImpl() { }

    /// <summary>Implemented method.</summary>
    partial void ImplementedMethod() { }
}", options: TestOptions.RegularWithDocumentationComments);

            compilation = CreateCompilationWithMscorlib(new[] { tree1, tree2 });

            fooClass = compilation.GlobalNamespace.GetTypeMembers("Foo").Single();
        }

        [Fact]
        public void TestSummaryOfType()
        {
            Assert.Equal(
@"<member name=""T:Foo"">
    <summary>Summary on first file's Foo.</summary>
    <summary>Summary on second file's Foo.</summary>
</member>
", fooClass.GetDocumentationCommentXml());
        }

        [Fact]
        public void TestSummaryOfMethodWithNoImplementation()
        {
            var method = fooClass.GetMembers("MethodWithNoImplementation").Single();
            Assert.Equal(string.Empty, method.GetDocumentationCommentXml()); //Matches what would be written to an XML file.
        }

        [Fact]
        public void TestImplementedMethodWithNoSummaryOnImpl()
        {
            // This is an interesting behavior; as long as there is any XML at all on the implementation, it overrides
            // any XML on the latent declaration. Since we don't have a summary on this implementation, this should be
            // null!
            var method = fooClass.GetMembers("ImplementedMethodWithNoSummaryOnImpl").Single();
            Assert.Equal(
@"<member name=""M:Foo.ImplementedMethodWithNoSummaryOnImpl"">
    <remarks>Foo.</remarks>
</member>
", method.GetDocumentationCommentXml());
        }

        [Fact]
        public void TestImplementedMethod()
        {
            var method = fooClass.GetMembers("ImplementedMethod").Single();
            Assert.Equal(
@"<member name=""M:Foo.ImplementedMethod"">
    <summary>Implemented method.</summary>
</member>
", method.GetDocumentationCommentXml());
        }
    }
}
