// =====================================================================
//  PART:    Interfaces - the one exception storage is allowed to throw
//  ORIGIN:  Fdraft - Frent Dhieniel Raborar
//  EDITS:   Clint Wood Gado - header and comments only
//  VOICE:   every comment in this file is mine (Clint), in the first person
// =====================================================================
using System;

namespace BarangayDocumentSystem.Interfaces;

/// <summary>
/// Raised when storage fails: the server is down, the password is wrong, a
/// constraint rejected the row. Frent wrote it and I kept it exactly as he
/// designed it.
///
/// It deliberately does NOT derive from InvalidOperationException. My
/// business rules throw InvalidOperationException ("release an unpaid
/// document"), and a view answers that with a warning the clerk can act on.
/// A database failure is a different kind of problem - the clerk cannot fix
/// it from the dialog - so it needs its own type, and ViewBase.Persist
/// catches this one specifically.
///
/// It lives in Interfaces, not Database, so the views can catch it without
/// knowing which database is behind the repository.
/// </summary>
public class RepositoryException : Exception
{
    public RepositoryException(string message) : base(message) { }
    public RepositoryException(string message, Exception inner) : base(message, inner) { }
}
