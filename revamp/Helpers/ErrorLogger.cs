using System;
using System.Diagnostics;
using System.IO;

namespace BarangayDocumentSystem.Helpers
{
    public static class ErrorLogger
    {
        public static void Write(Exception error)
        {
            try
            {
                string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "BarangayDocumentSystem");
                Directory.CreateDirectory(folder);
                File.AppendAllText(Path.Combine(folder, "errors.log"),
                    DateTime.Now.ToString("u") + Environment.NewLine + error + Environment.NewLine + Environment.NewLine);
            }
            catch (Exception loggingError)
            {
                Debug.WriteLine(loggingError);
                Debug.WriteLine(error);
            }
        }
    }
}
