using System;
namespace System.Runtime.CompilerServices;

/// <summary>
/// Marker type the C# compiler looks for whenever it compiles an
/// <c>init</c>-only property — including the ones every positional
/// <c>record</c> in this project generates automatically. .NET's own copy of
/// this type ships only from .NET 5 / .NET Standard 2.1 onward, so on
/// .NET Framework 4.8 the compiler cannot find it unless something declares
/// it. This file is that declaration — it is never used directly by name,
/// just required to exist for <c>record</c> and <c>init</c> to compile here.
/// </summary>
internal static class IsExternalInit
{
}
