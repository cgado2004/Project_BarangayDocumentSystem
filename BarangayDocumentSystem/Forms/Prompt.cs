using System.Windows.Forms;
using System.Drawing;
using System;
using System.Linq;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.UIHelpers;
namespace BarangayDocumentSystem.Forms;

/// <summary>
/// A small modal text-input dialog, built entirely in code.
///
/// WinForms has MessageBox for output but no built-in input box, so this
/// fills the gap. Everything is created and wired programmatically to show
/// that the designer is a convenience, not a requirement.
/// </summary>
public static class Prompt
{
    /// <summary>Returns the typed text, or an empty string if cancelled.</summary>
    public static string Show(IWin32Window owner, string title, string question)
    {
        using var form = new Form
        {
            Text = title,
            ClientSize = new Size(460, 190),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MaximizeBox = false,
            MinimizeBox = false
        };

        var label = new Label
        {
            Text = question,
            Location = new Point(18, 18),
            Size = new Size(424, 46)
        };

        var textBox = new TextBox
        {
            Location = new Point(18, 70),
            Size = new Size(424, 27),
            MaxLength = 200
        };

        var ok = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Location = new Point(226, 120),
            Size = new Size(104, 38)
        };

        var cancel = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Location = new Point(338, 120),
            Size = new Size(104, 38)
        };

        // Controls.Add is what actually puts them on the form — forgetting it
        // is a classic WinForms bug: the control exists but never appears.
        form.Controls.Add(label);
        form.Controls.Add(textBox);
        form.Controls.Add(ok);
        form.Controls.Add(cancel);

        form.AcceptButton = ok;    // Enter
        form.CancelButton = cancel; // Esc
        DialogLayout.MakeScrollable(form);

        return form.ShowDialog(owner) == DialogResult.OK
            ? textBox.Text.Trim()
            : string.Empty;
    }
}

/// <summary>
/// Asks where a resident is transferring to before they are archived.
/// Nothing is deleted — the resident moves to the Archive page.
/// </summary>
public sealed class TransferForm : Form
{
    private readonly TextBox _destination = new();
    private readonly ComboBox _reason = new();
    private readonly string _residentName;

    public string Destination { get; private set; } = string.Empty;
    public string Reason { get; private set; } = string.Empty;

    public TransferForm(Resident resident)
    {
        if (resident is null) throw new ArgumentNullException(nameof(resident));
        _residentName = resident.GetFullName();

        Text = "Transfer resident";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = AppTheme.Background;
        Font = AppTheme.BodyFont;
        ClientSize = new Size(480, 330);

        Controls.Add(new Label
        {
            Text = $"{resident.GetFullName()} will be moved to the Archive.\n" +
                   $"Their record and {resident.Requests.Count} document request(s) are kept — nothing is deleted.",
            Location = new Point(24, 20),
            Size = new Size(432, 48),
            ForeColor = AppTheme.TextPrimary
        });

        Controls.Add(Caption("Transferred to (barangay, city or address)", 80));
        _destination.Location = new Point(24, 100);
        _destination.Size = new Size(432, 27);
        _destination.MaxLength = 255;
        _destination.BorderStyle = BorderStyle.FixedSingle;
        Controls.Add(_destination);

        Controls.Add(Caption("Reason", 144));
        _reason.Location = new Point(24, 164);
        _reason.Size = new Size(432, 27);
        _reason.DropDownStyle = ComboBoxStyle.DropDown;
        _reason.MaxLength = 255;
        _reason.Items.AddRange(new object[]
        {
            "Moved to another barangay",
            "Moved to another city / municipality",
            "Employment",
            "Marriage",
            "Studies",
            "Joined family elsewhere"
        });
        Controls.Add(_reason);

        var ok = UiFactory.PrimaryButton("Transfer to archive", 170);
        ok.Location = new Point(286, 262);
        ok.Height = 36;
        ok.Click += (_, _) => Submit();

        var cancel = UiFactory.SecondaryButton("Cancel", 90);
        cancel.Location = new Point(186, 262);
        cancel.Height = 36;
        cancel.DialogResult = DialogResult.Cancel;

        Controls.Add(ok);
        Controls.Add(cancel);
        AcceptButton = ok;
        CancelButton = cancel;
        DialogLayout.MakeScrollable(this);
    }

    private static Label Caption(string text, int top) => new()
    {
        Text = text,
        Font = AppTheme.SmallBoldFont,
        ForeColor = AppTheme.TextSecondary,
        Location = new Point(24, top),
        Size = new Size(432, 18)
    };

    private void Submit()
    {
        if (string.IsNullOrWhiteSpace(_destination.Text))
        {
            Dialog.Warn("Please enter where the resident is transferring to.");
            _destination.Focus();
            return;
        }

        if (!Dialog.Confirm(
                $"Transfer {_residentName} to {_destination.Text.Trim()} and move them to the Archive?",
                "Confirm transfer"))
            return;

        Destination = _destination.Text.Trim();
        Reason = _reason.Text.Trim();
        DialogResult = DialogResult.OK;
        Close();
    }
}

/// <summary>
/// "Who is this document for?" — lists ACTIVE residents (archived ones are not
/// offered) so the Requests page can start a new document.
/// </summary>
public sealed class ResidentPickerForm : Form
{
    private readonly IBarangayRepository _repository;
    private readonly TextBox _search = new();
    private readonly DataGridView _grid = new();
    private readonly Label _empty = new();

    /// <summary>The chosen resident; set only when the result is OK.</summary>
    public Resident? SelectedResident { get; private set; }

    public ResidentPickerForm(IBarangayRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));

        Text = "New Request — choose a resident";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = AppTheme.Background;
        Font = AppTheme.BodyFont;
        ClientSize = new Size(760, 500);

        _search.Location = new Point(20, 18);
        _search.Size = new Size(720, 27);
        _search.BorderStyle = BorderStyle.FixedSingle;
        CueBanner.Set(_search, "Search by name, purok, or contact number…");
        _search.TextChanged += (_, _) => LoadResidents();
        Controls.Add(_search);

        UiFactory.StyleGrid(_grid);
        _grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) Choose(); };

        var host = UiFactory.CardHost(_grid);
        host.Location = new Point(20, 58);
        host.Size = new Size(720, 372);
        host.Dock = DockStyle.None;
        Controls.Add(host);

        _empty.Dock = DockStyle.Fill;
        _empty.TextAlign = ContentAlignment.MiddleCenter;
        _empty.ForeColor = AppTheme.TextMuted;
        _empty.BackColor = AppTheme.Surface;
        _empty.Visible = false;
        host.Controls.Add(_empty);
        _empty.BringToFront();

        var choose = UiFactory.PrimaryButton("Continue", 120);
        choose.Location = new Point(620, 448);
        choose.Height = 36;
        choose.Click += (_, _) => Choose();

        var cancel = UiFactory.SecondaryButton("Cancel", 90);
        cancel.Location = new Point(522, 448);
        cancel.Height = 36;
        cancel.DialogResult = DialogResult.Cancel;

        Controls.Add(choose);
        Controls.Add(cancel);
        AcceptButton = choose;
        CancelButton = cancel;

        DialogLayout.MakeScrollable(this);
        LoadResidents();
    }

    private void LoadResidents()
    {
        _grid.DataSource = _repository.SearchResidents(_search.Text.Trim())
            .Select(r => new
            {
                ID = r.ResidentId,
                Name = r.GetSortableName(),
                Age = r.GetAge(),
                Purok = r.Purok,
                Contact = r.ContactNumber,
                Classification = r.GetClassificationText()
            })
            .ToList();

        if (_grid.Columns["ID"] is { } idColumn)
            idColumn.Visible = false;

        _empty.Text = string.IsNullOrWhiteSpace(_search.Text)
            ? "No residents registered yet — add one on the Residents page first"
            : $"No residents match “{_search.Text.Trim()}”";
        _empty.Visible = _grid.Rows.Count == 0;
    }

    private void Choose()
    {
        if (_grid.CurrentRow?.Cells["ID"].Value is not int id)
        {
            Dialog.SelectFirst("resident");
            return;
        }

        SelectedResident = _repository.Residents.FirstOrDefault(r => r.ResidentId == id);
        if (SelectedResident is null) return;

        DialogResult = DialogResult.OK;
        Close();
    }
}
