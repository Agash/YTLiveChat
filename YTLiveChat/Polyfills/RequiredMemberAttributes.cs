#if NETSTANDARD2_0 || NETSTANDARD2_1
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.CompilerServices
{
    /// <summary>Specifies that a type has required members or that a member is required.</summary>
    [AttributeUsage(
        AttributeTargets.Class
            | AttributeTargets.Struct
            | AttributeTargets.Field
            | AttributeTargets.Property,
        AllowMultiple = false,
        Inherited = false
    )]
    internal sealed class RequiredMemberAttribute : Attribute { }

    /// <summary>Indicates that the C# compiler should consider a method to be the one it would synthesize for a given compiler feature.</summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    internal sealed class CompilerFeatureRequiredAttribute : Attribute
    {
        /// <summary>Initializes a new instance of the <see cref="CompilerFeatureRequiredAttribute"/> class.</summary>
        /// <param name="featureName">The name of the required compiler feature.</param>
        public CompilerFeatureRequiredAttribute(string featureName) => FeatureName = featureName;

        /// <summary>Gets the name of the compiler feature.</summary>
        public string FeatureName { get; }

        /// <summary>Gets or sets a value indicating whether the compiler feature is optional.</summary>
        /// <value><see langword="true"/> if the compiler feature is optional; otherwise, <see langword="false"/>.</value>
        public bool IsOptional { get; init; }

        /// <summary>The <see cref="FeatureName"/> used for the ref structs language feature.</summary>
        public const string RefStructs = nameof(RefStructs);

        /// <summary>The <see cref="FeatureName"/> used for the required members language feature.</summary>
        public const string RequiredMembers = nameof(RequiredMembers);
    }
}

namespace System.Diagnostics.CodeAnalysis
{
    /// <summary>
    /// Specifies that this constructor sets all required members for the current type, and callers
    /// do not need to set any required members themselves.
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
    internal sealed class SetsRequiredMembersAttribute : Attribute { }
}

// Polyfill for IsExternalInit, often needed for 'init' accessors with older TFMs
// if not already provided by the SDK version being used for .NET Standard.
namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Reserved to be used by the compiler for tracking metadata.
    /// This attribute should not be used by developers in source code.
    /// </summary>
    [ComponentModel.EditorBrowsable(ComponentModel.EditorBrowsableState.Never)]
    internal static class IsExternalInit { }
}
#endif
