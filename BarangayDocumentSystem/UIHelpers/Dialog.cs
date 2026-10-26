using System.Windows.Forms;
using System;
namespace BarangayDocumentSystem.UIHelpers;



















public static class Dialog
{

    public static void Warn(string message, string caption = "Invalid input") =>
        MessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Warning);


    public static void Info(string message, string caption = "Information") =>
        MessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Information);


    public static void Error(string message, string caption = "Error") =>
        MessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);




    public static void SelectFirst(string what) =>
        MessageBox.Show($"Please select a {what} first.", "No selection",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);


    public static bool Confirm(string message, string caption = "Please confirm") =>
        MessageBox.Show(message, caption, MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question) == DialogResult.Yes;


    public static bool ConfirmDestructive(string message, string caption = "Confirm") =>
        MessageBox.Show(message, caption, MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2)
            == DialogResult.Yes;
}
