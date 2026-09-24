using System;
namespace BarangayDocumentSystem.Interfaces;

/// <summary>
/// Raised when storage fails (server down, wrong password, constraint
/// violation, ...). It deliberately does NOT derive from
/// InvalidOperationException, so the views can tell a database problem apart
/// from a business-rule violation such as "release an unpaid document".
///
/// It lives in Interfaces so the views can catch it without knowing which
/// database is behind the repository.
/// </summary>
public class RepositoryException : Exception
{
    public RepositoryException(string message) : base(message) { }
    public RepositoryException(string message, Exception inner) : base(message, inner) { }
}
