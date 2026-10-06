// ---------------------------------------------------------------------------
//  OperationResult.cs - how my service classes say "done" or "no, because...".
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;

namespace BarangayDocumentSystem.Models
{
    /// <summary>
    /// The answer a service gives back.
    ///
    /// I stopped throwing exceptions for ordinary business refusals ("that
    /// resident already has a request open") because a screen should not need
    /// a try/catch around every button just to show a message. Something that
    /// a clerk can fix by reading the sentence is a refused result; something
    /// that means the program is wrong - the database is gone, a rule was
    /// broken by my own code - is still an exception.
    /// </summary>
    public class OperationResult
    {
        public bool Succeeded { get; protected set; }
        public string Message { get; protected set; }

        public OperationResult()
        {
            Message = string.Empty;
        }

        public static OperationResult Ok(string message)
        {
            OperationResult result = new OperationResult();
            result.Succeeded = true;
            result.Message = message ?? string.Empty;
            return result;
        }

        public static OperationResult Fail(string message)
        {
            OperationResult result = new OperationResult();
            result.Succeeded = false;
            result.Message = message ?? string.Empty;
            return result;
        }

        public override string ToString()
        {
            return (Succeeded ? "OK: " : "No: ") + Message;
        }
    }

    /// <summary>The same idea, carrying a value back - the new resident, the
    /// generated reference number, the report table.</summary>
    public class OperationResult<T> : OperationResult
    {
        public T Value { get; private set; }

        public static OperationResult<T> Ok(T value, string message)
        {
            OperationResult<T> result = new OperationResult<T>();
            result.Value = value;
            result.Succeeded = true;
            result.Message = message ?? string.Empty;
            return result;
        }

        public new static OperationResult<T> Fail(string message)
        {
            OperationResult<T> result = new OperationResult<T>();
            result.Succeeded = false;
            result.Message = message ?? string.Empty;
            return result;
        }
    }
}
