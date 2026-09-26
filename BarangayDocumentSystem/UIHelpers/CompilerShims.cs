// =====================================================================
//  PART:    UIHelpers - the IsExternalInit marker that lets records compile on Framework 4.8
//  ORIGIN:  Fdraft - Frent Dhieniel Raborar (verbatim)
//  EDITS:   Clint Wood Gado - header and comment wording only
//  VOICE:   every comment in this file is mine (Clint), in the first person
// =====================================================================
using System;
namespace System.Runtime.CompilerServices;

/// <summary>
/// The marker type the C# compiler looks for whenever it compiles an
/// <c>init</c>-only property - including the ones every positional
/// <c>record</c> in this project generates for me. .NET's own copy ships
/// only from .NET 5 / .NET Standard 2.1 onward, so on .NET Framework 4.8
/// the compiler cannot find it unless something declares it. Frent found
/// this fix and I kept his file exactly as it was: it is never used by
/// name, it just has to exist for <c>record</c> and <c>init</c> to compile.
/// </summary>
internal static class IsExternalInit
{
}
