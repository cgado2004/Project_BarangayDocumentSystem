using System.Drawing;
using System.Windows.Forms;
using BarangayDocumentSystem.App.Controls;
using BarangayDocumentSystem.App.Theme;
using BarangayDocumentSystem.Core.Data;
using BarangayDocumentSystem.Core.Entities;
using BarangayDocumentSystem.Core.Rules;

namespace BarangayDocumentSystem.App.Dialogs;

/// <summary>
/// The shared setup for every dialog I wrote.
///
/// I make them sizable, give them a MinimumSize and turn on AutoScroll, so a
/// dialog can never end up with its OK button off the bottom of a small
/// screen. That is the usual way WinForms dialogs break on somebody else's
/// machine, and it is invisible to me on mine.
/// </summary>
public class DialogBase : Form
{
    protected DialogBase(string title, int w, int h)
    {
        Text = title;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        BackColor = AppTheme.Canvas;
        Font = AppTheme.Body;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(w, h);
        MinimumSize = new Size(w - 40, h - 40);
        AutoScroll = true;
        Padding = new Padding(22);
    }

    protected static Label FieldLabel(string text) => new()
    {
        Text = text,
        Font = AppTheme.SmallBold,
        ForeColor = AppTheme.Muted,
        AutoSize = false,
        Height = 20
    };
}

/// <summary>My add-or-edit resident form. I use the same dialog for both, so
/// the fields and the validation can never drift apart between them.</summary>
public class ResidentDialog : DialogBase
{
    public ResidentDetails? Result { get; private set; }

    private readonly TextBox _first = new(), _middle = new(), _last = new(), _suffix = new();
    private readonly DateTimePicker _dob = new(), _residency = new();
    private readonly ComboBox _gender = new(), _civil = new(), _purok = new();
    private readonly TextBox _address = new(), _contact = new(), _occupation = new();
    private readonly CheckBox _voter = new() { Text = "Registered voter" };
    private readonly CheckedListBox _classes = new();

    public ResidentDialog(Resident? existing = null)
        : base(existing is null ? "Add resident" : "Edit resident", 720, 560)
    {
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.Transparent };

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 4,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 0, 8, 0)
        };
        for (int i = 0; i < 4; i++)
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

        foreach (var c in new Control[] { _first, _middle, _last, _suffix, _address, _contact, _occupation })
        {
            c.Dock = DockStyle.Fill;
            c.Font = AppTheme.Body;
        }
        foreach (var c in new Control[] { _gender, _civil, _purok, _dob, _residency })
        {
            c.Dock = DockStyle.Fill;
            c.Font = AppTheme.Body;
        }

        _gender.DropDownStyle = ComboBoxStyle.DropDownList;
        _civil.DropDownStyle = ComboBoxStyle.DropDownList;
        _purok.DropDownStyle = ComboBoxStyle.DropDownList;
        _gender.Items.AddRange(Enum.GetNames<Gender>());
        _civil.Items.AddRange(Enum.GetNames<CivilStatus>());
        _purok.Items.AddRange(BarangayProfile.Puroks);

        _dob.Format = DateTimePickerFormat.Short;
        _residency.Format = DateTimePickerFormat.Short;

        _classes.Items.AddRange(new object[]
            { "Senior Citizen", "PWD", "Indigent", "Student", "Solo Parent" });
        _classes.CheckOnClick = true;
        _classes.Font = AppTheme.Body;
        _classes.BorderStyle = BorderStyle.FixedSingle;
        _classes.Height = 104;
        _classes.Dock = DockStyle.Fill;

        int row = 0;
        void Add(string label, Control control, int col, int span = 1)
        {
            var host = new Panel { Dock = DockStyle.Fill, Height = 56, BackColor = Color.Transparent };
            var l = FieldLabel(label); l.Dock = DockStyle.Top;
            control.Dock = DockStyle.Top;
            host.Controls.Add(control);
            host.Controls.Add(l);
            grid.Controls.Add(host, col, row);
            if (span > 1) grid.SetColumnSpan(host, span);
        }

        Add("First name *", _first, 0);
        Add("Middle name", _middle, 1);
        Add("Last name *", _last, 2);
        Add("Suffix", _suffix, 3);
        row++;
        Add("Date of birth", _dob, 0);
        Add("Gender", _gender, 1);
        Add("Civil status", _civil, 2);
        Add("Date of residency", _residency, 3);
        row++;
        Add("Purok *", _purok, 0);
        Add("Address", _address, 1, 3);
        row++;
        Add("Contact number", _contact, 0);
        Add("Occupation", _occupation, 1);
        Add("Classifications", _classes, 2, 2);
        row++;

        _voter.Dock = DockStyle.Top;
        _voter.Height = 30;
        _voter.Font = AppTheme.Body;
        grid.Controls.Add(_voter, 0, row);
        grid.SetColumnSpan(_voter, 2);

        scroll.Controls.Add(grid);

        // ---- my save and cancel buttons ----
        var bar = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 12, 0, 0)
        };
        var ok = new PillButton { Text = "Save", Width = 120 };
        var cancel = new PillButton { Text = "Cancel", Width = 110, Look = PillButton.Style.Outline };
        bar.Controls.Add(ok);
        bar.Controls.Add(cancel);

        ok.Click += (_, _) => Save();
        cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        CancelButton = cancel;

        Controls.Add(scroll);
        Controls.Add(bar);

        if (existing is not null) LoadFrom(existing);
        else
        {
            _gender.SelectedIndex = 0;
            _civil.SelectedIndex = 0;
            _dob.Value = DateTime.Today.AddYears(-25);
        }
    }

    /// <summary>I copy an existing resident's details into my fields.
    /// I named it LoadFrom rather than Load because Form already has a Load
    /// event, and hiding it would have been a trap for whoever reads this
    /// next.</summary>
    private void LoadFrom(Resident r)
    {
        _first.Text = r.FirstName;
        _middle.Text = r.MiddleName;
        _last.Text = r.LastName;
        _suffix.Text = r.Suffix;
        _dob.Value = r.DateOfBirth == default ? DateTime.Today.AddYears(-25) : r.DateOfBirth;
        _residency.Value = r.DateOfResidency == default ? DateTime.Today : r.DateOfResidency;
        _gender.SelectedItem = r.Gender.ToString();
        _civil.SelectedItem = r.CivilStatus.ToString();
        _purok.SelectedItem = r.Purok;
        _address.Text = r.AddressLine;
        _contact.Text = r.ContactNumber;
        _occupation.Text = r.Occupation;
        _voter.Checked = r.IsRegisteredVoter;

        _classes.SetItemChecked(0, r.HasClassification(ResidentClassification.SeniorCitizen));
        _classes.SetItemChecked(1, r.HasClassification(ResidentClassification.PWD));
        _classes.SetItemChecked(2, r.HasClassification(ResidentClassification.Indigent));
        _classes.SetItemChecked(3, r.HasClassification(ResidentClassification.Student));
        _classes.SetItemChecked(4, r.HasClassification(ResidentClassification.SoloParent));
    }

    private void Save()
    {
        // I validate here so a bad record never reaches the repository at
        // all. It is much easier to refuse it now than to clean it up later.
        if (string.IsNullOrWhiteSpace(_first.Text) || string.IsNullOrWhiteSpace(_last.Text))
        {
            MessageBox.Show(this, "First name and last name are required.",
                "Missing information", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_purok.SelectedItem is null)
        {
            MessageBox.Show(this, "Please choose a purok.",
                "Missing information", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // A person cannot start living somewhere before they were born, so I
        // refuse this outright rather than storing an impossible date.
        if (_residency.Value.Date < _dob.Value.Date)
        {
            MessageBox.Show(this,
                "The date of residency cannot be earlier than the date of birth.",
                "Check the dates", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var c = ResidentClassification.None;
        if (_classes.GetItemChecked(0)) c |= ResidentClassification.SeniorCitizen;
        if (_classes.GetItemChecked(1)) c |= ResidentClassification.PWD;
        if (_classes.GetItemChecked(2)) c |= ResidentClassification.Indigent;
        if (_classes.GetItemChecked(3)) c |= ResidentClassification.Student;
        if (_classes.GetItemChecked(4)) c |= ResidentClassification.SoloParent;

        Result = new ResidentDetails(
            _first.Text.Trim(), _middle.Text.Trim(), _last.Text.Trim(), _suffix.Text.Trim(),
            _dob.Value.Date,
            Enum.Parse<Gender>(_gender.SelectedItem?.ToString() ?? "Male"),
            Enum.Parse<CivilStatus>(_civil.SelectedItem?.ToString() ?? "Single"),
            _purok.SelectedItem?.ToString() ?? string.Empty,
            _address.Text.Trim(), _contact.Text.Trim(), _occupation.Text.Trim(),
            _residency.Value.Date, _voter.Checked, c);

        DialogResult = DialogResult.OK;
        Close();
    }
}

/// <summary>
/// Filing a new request.
///
/// I show the fee live as the document type changes, so the clerk can tell the
/// resident the price before anything is committed - and so nobody is
/// surprised by a charge after the fact.
/// </summary>
public class RequestDialog : DialogBase
{
    public DocumentType SelectedType { get; private set; }
    public string Purpose { get; private set; } = string.Empty;
    public ClearanceScope Scope { get; private set; } = ClearanceScope.Local;

    private readonly ComboBox _type = new();
    private readonly TextBox _purpose = new();
    private readonly ComboBox _scope = new();
    private readonly Label _feeLine = new();
    private readonly Label _blockLine = new();
    private readonly PillButton _ok = new() { Text = "File request", Width = 140 };

    private readonly Resident _resident;
    private readonly FeeSchedule _fees;

    public RequestDialog(Resident resident, FeeSchedule fees)
        : base($"New request — {resident.GetFullName()}", 620, 430)
    {
        _resident = resident;
        _fees = fees;

        var body = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.Transparent };

        _type.DropDownStyle = ComboBoxStyle.DropDownList;
        _type.Font = AppTheme.Body;
        _type.Dock = DockStyle.Top;
        foreach (var t in Enum.GetValues<DocumentType>())
            _type.Items.Add(FeeSchedule.NameOf(t));
        _type.SelectedIndex = 0;

        _scope.DropDownStyle = ComboBoxStyle.DropDownList;
        _scope.Font = AppTheme.Body;
        _scope.Dock = DockStyle.Top;
        _scope.Items.AddRange(new object[] { "Local employment", "For work abroad" });
        _scope.SelectedIndex = 0;

        _purpose.Font = AppTheme.Body;
        _purpose.Dock = DockStyle.Top;

        _feeLine.Font = AppTheme.Heading;
        _feeLine.ForeColor = AppTheme.Primary;
        _feeLine.Dock = DockStyle.Top;
        _feeLine.Height = 34;

        _blockLine.Font = AppTheme.Body;
        _blockLine.ForeColor = AppTheme.Danger;
        _blockLine.Dock = DockStyle.Top;
        _blockLine.Height = 58;

        void Section(string label, Control c)
        {
            var host = new Panel { Dock = DockStyle.Top, Height = 58, BackColor = Color.Transparent };
            var l = FieldLabel(label); l.Dock = DockStyle.Top;
            c.Dock = DockStyle.Top;
            host.Controls.Add(c);
            host.Controls.Add(l);
            body.Controls.Add(host);
            host.BringToFront();
        }

        body.Controls.Add(_blockLine);
        body.Controls.Add(_feeLine);
        Section("Purpose *", _purpose);
        Section("Clearance is for", _scope);
        Section("Document type *", _type);

        _type.SelectedIndexChanged += (_, _) => Recalculate();
        _scope.SelectedIndexChanged += (_, _) => Recalculate();

        var bar = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 12, 0, 0)
        };
        var cancel = new PillButton { Text = "Cancel", Width = 110, Look = PillButton.Style.Outline };
        bar.Controls.Add(_ok);
        bar.Controls.Add(cancel);
        cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        CancelButton = cancel;
        _ok.Click += (_, _) => Confirm();

        Controls.Add(body);
        Controls.Add(bar);

        Recalculate();
    }

    private DocumentType CurrentType() =>
        Enum.GetValues<DocumentType>()[_type.SelectedIndex];

    /// <summary>
    /// I re-run the fee rules whenever the choice changes, and I disable the
    /// button outright when the law forbids the document altogether.
    /// </summary>
    private void Recalculate()
    {
        var type = CurrentType();
        var scope = _scope.SelectedIndex == 1 ? ClearanceScope.Abroad : ClearanceScope.Local;

        // The scope only means anything for a barangay clearance, so I grey
        // it out for everything else rather than letting it mislead.
        _scope.Enabled = type == DocumentType.BarangayClearance;

        var a = _fees.Assess(_resident, type, scope);

        if (a.IsBlocked)
        {
            _feeLine.Text = "Cannot be issued";
            _feeLine.ForeColor = AppTheme.Danger;
            _blockLine.Text = a.BlockReason;
            _ok.Enabled = false;
            return;
        }

        _blockLine.Text = string.Empty;
        _ok.Enabled = true;
        _feeLine.ForeColor = a.IsWaived ? AppTheme.Success : AppTheme.Primary;
        _feeLine.Text = a.IsWaived
            ? $"FREE  —  {a.Basis}"
            : $"{DisplayFormat.Peso(a.FinalFee)}  —  {a.Basis}";
    }

    private void Confirm()
    {
        if (string.IsNullOrWhiteSpace(_purpose.Text))
        {
            MessageBox.Show(this, "Please state the purpose of the request.",
                "Missing information", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        SelectedType = CurrentType();
        Purpose = _purpose.Text.Trim();
        Scope = _scope.SelectedIndex == 1 ? ClearanceScope.Abroad : ClearanceScope.Local;
        DialogResult = DialogResult.OK;
        Close();
    }
}

/// <summary>I record a payment against an official receipt number here. I
/// refuse to accept the payment without one, because money must never be
/// recorded with nothing to trace it back to.</summary>
public class PaymentDialog : DialogBase
{
    public string ReceiptNumber { get; private set; } = string.Empty;

    private readonly TextBox _receipt = new();

    public PaymentDialog(DocumentRequest request) : base("Record payment", 460, 300)
    {
        var body = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.Transparent };

        var amount = new Label
        {
            Text = DisplayFormat.Peso(request.Fee),
            Font = AppTheme.Display,
            ForeColor = AppTheme.Ink,
            Dock = DockStyle.Top,
            Height = 48
        };
        var basis = new Label
        {
            Text = request.FeeBasis,
            Font = AppTheme.Small,
            ForeColor = AppTheme.Muted,
            Dock = DockStyle.Top,
            Height = 34
        };

        _receipt.Font = AppTheme.Body;
        _receipt.Dock = DockStyle.Top;
        var l = FieldLabel("Official receipt number *");
        l.Dock = DockStyle.Top;

        body.Controls.Add(_receipt);
        body.Controls.Add(l);
        body.Controls.Add(basis);
        body.Controls.Add(amount);

        var bar = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom, Height = 60,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = Color.Transparent, Padding = new Padding(0, 12, 0, 0)
        };
        var ok = new PillButton { Text = "Record", Width = 120 };
        var cancel = new PillButton { Text = "Cancel", Width = 110, Look = PillButton.Style.Outline };
        bar.Controls.Add(ok);
        bar.Controls.Add(cancel);
        cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        CancelButton = cancel;

        ok.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_receipt.Text))
            {
                MessageBox.Show(this,
                    "An official receipt number is required. Money must never be "
                  + "recorded without one.",
                    "Missing information", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            ReceiptNumber = _receipt.Text.Trim();
            DialogResult = DialogResult.OK;
            Close();
        };

        Controls.Add(body);
        Controls.Add(bar);
    }
}

/// <summary>A single free-text question. I use it to collect the rejection
/// reason, which the resident is entitled to be told.</summary>
public class PromptDialog : DialogBase
{
    public string Value { get; private set; } = string.Empty;

    private readonly TextBox _input = new();

    public PromptDialog(string title, string question) : base(title, 480, 260)
    {
        var body = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.Transparent };

        var q = new Label
        {
            Text = question,
            Font = AppTheme.Body,
            ForeColor = AppTheme.Ink,
            Dock = DockStyle.Top,
            Height = 52
        };

        _input.Font = AppTheme.Body;
        _input.Dock = DockStyle.Top;
        _input.Multiline = true;
        _input.Height = 72;

        body.Controls.Add(_input);
        body.Controls.Add(q);

        var bar = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom, Height = 60,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = Color.Transparent, Padding = new Padding(0, 12, 0, 0)
        };
        var ok = new PillButton { Text = "Confirm", Width = 120, Accent = AppTheme.Danger };
        var cancel = new PillButton { Text = "Cancel", Width = 110, Look = PillButton.Style.Outline };
        bar.Controls.Add(ok);
        bar.Controls.Add(cancel);
        cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        CancelButton = cancel;

        ok.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_input.Text))
            {
                MessageBox.Show(this, "A reason is required.",
                    "Missing information", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            Value = _input.Text.Trim();
            DialogResult = DialogResult.OK;
            Close();
        };

        Controls.Add(body);
        Controls.Add(bar);
    }
}

/// <summary>I show the finished certificate here, exactly as it will print,
/// so mistakes are caught on screen rather than on paper.</summary>
public class DocumentPreviewDialog : DialogBase
{
    public DocumentPreviewDialog(DocumentRequest request)
        : base($"Preview — {request.GetReferenceNumber()}", 700, 640)
    {
        var renderer = new DocumentRenderer(BarangayProfile.MagugpoPoblacion);

        var text = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            // I use a monospaced font because I centre the letterhead by
            // counting characters. In a proportional font it would come out
            // ragged and the certificate would look wrong.
            Font = AppTheme.MonoBody,
            Dock = DockStyle.Fill,
            ScrollBars = ScrollBars.Both,
            WordWrap = false,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Text = renderer.Render(request).Replace("\n", Environment.NewLine)
        };

        var bar = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom, Height = 60,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = Color.Transparent, Padding = new Padding(0, 12, 0, 0)
        };
        var close = new PillButton { Text = "Close", Width = 110 };
        close.Click += (_, _) => Close();
        bar.Controls.Add(close);
        CancelButton = close;

        Controls.Add(text);
        Controls.Add(bar);
    }
}
