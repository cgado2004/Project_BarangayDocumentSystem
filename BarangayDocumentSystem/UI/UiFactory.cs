// ---------------------------------------------------------------------------
//  UiFactory.cs - the controls, made the same way everywhere.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using BarangayDocumentSystem.UI.Controls;

namespace BarangayDocumentSystem.UI
{
    /// <summary>
    /// Builds the controls the program is made of.
    ///
    /// If I created a button on each screen, each screen would have a slightly
    /// different button - one with nine pixels of padding, one with twelve, one
    /// bold. That is exactly what makes a program look untidy even when nothing
    /// is actually wrong with it, and it is what the review meant by spacing.
    /// So every button, grid, card and field label comes from here, and every
    /// gap comes from AppTheme.
    ///
    /// I build the interface in code rather than in the visual designer. Two
    /// reasons: the layout uses the 8-point grid instead of drag-and-drop
    /// positions, which is what cleaned the screens up, and a code-built layout
    /// cannot be quietly rearranged by somebody opening the designer on
    /// another computer.
    /// </summary>
    public static class UiFactory
    {
        // ==================================================================
        //  Buttons
        // ==================================================================

        public static Button PrimaryButton(string text)
        {
            return Button(text, AppTheme.Primary, Color.White, AppTheme.Primary);
        }

        public static Button SecondaryButton(string text)
        {
            return Button(text, Color.White, AppTheme.Primary, AppTheme.Border);
        }

        public static Button GhostButton(string text)
        {
            return Button(text, AppTheme.Canvas, AppTheme.Ink, AppTheme.Border);
        }

        public static Button DangerButton(string text)
        {
            return Button(text, Color.White, AppTheme.Danger, AppTheme.Danger);
        }

        private static Button Button(string text, Color back, Color fore, Color border)
        {
            Button button = new Button();
            button.Text = text;
            button.AutoSize = false;
            button.Height = AppTheme.ButtonHeight;
            button.Font = AppTheme.BodyBold;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = border;
            button.FlatAppearance.BorderSize = 1;
            button.BackColor = back;
            button.ForeColor = fore;
            button.Cursor = Cursors.Hand;
            button.Margin = new Padding(0, 0, AppTheme.Gap2, 0);
            button.Padding = new Padding(AppTheme.Gap3, 0, AppTheme.Gap3, 0);
            button.UseVisualStyleBackColor = false;

            // A measured width, so a row of buttons sits on the grid instead of
            // measuring itself differently on each screen.
            int width = TextRenderer.MeasureText(text, AppTheme.BodyBold).Width + (AppTheme.Gap5 * 2);
            button.Width = Math.Max(96, Math.Min(width, 260));

            return button;
        }

        /// <summary>Turns a list of controls into a wrapping row on the 8-point
        /// grid. Every button bar in the program is built with this, so no bar
        /// is a hand-placed pile.</summary>
        public static FlowLayoutPanel Row(params Control[] controls)
        {
            FlowLayoutPanel row = new FlowLayoutPanel();
            row.AutoSize = true;
            row.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            row.FlowDirection = FlowDirection.LeftToRight;
            row.WrapContents = false;
            row.Margin = new Padding(0, 0, 0, AppTheme.Gap2);

            foreach (Control control in controls) row.Controls.Add(control);
            return row;
        }

        // ==================================================================
        //  Fields
        // ==================================================================

        public static TextBox TextBox(string placeholder, int maximumLength)
        {
            TextBox box = new TextBox();
            box.Font = AppTheme.Body;
            box.BorderStyle = BorderStyle.FixedSingle;
            box.MaxLength = maximumLength;
            box.Height = AppTheme.FieldHeight;
            box.BackColor = AppTheme.Surface;
            box.ForeColor = AppTheme.Ink;

            if (!string.IsNullOrEmpty(placeholder)) Placeholder(box, placeholder);
            return box;
        }

        /// <summary>
        /// Grey hint text inside an empty box.
        ///
        /// This framework has no placeholder of its own, so I draw it myself.
        /// It is a hint, not a value: it disappears the moment the box has
        /// focus or a letter in it, so it can never be saved as if somebody
        /// had typed it.
        /// </summary>
        public static void Placeholder(TextBox box, string hint)
        {
            box.Tag = hint;

            EventHandler repaint = delegate (object sender, EventArgs e)
            {
                ((TextBox)sender).Invalidate();
            };

            box.Paint += delegate (object sender, PaintEventArgs e)
            {
                TextBox target = (TextBox)sender;
                if (target.Focused || target.TextLength > 0) return;

                string text = target.Tag as string;
                if (string.IsNullOrEmpty(text)) return;

                using (Brush brush = new SolidBrush(AppTheme.MutedSoft))
                    e.Graphics.DrawString(text, AppTheme.Small, brush, 2f, 5f);
            };

            box.TextChanged += repaint;
            box.GotFocus += repaint;
            box.LostFocus += repaint;
        }

        public static ComboBox DropDown(IEnumerable<string> items, bool readOnly)
        {
            ComboBox box = new ComboBox();
            box.DropDownStyle = readOnly ? ComboBoxStyle.DropDownList : ComboBoxStyle.DropDown;
            box.Font = AppTheme.Body;
            box.FlatStyle = FlatStyle.Flat;
            box.Height = AppTheme.FieldHeight;
            box.BackColor = AppTheme.Surface;

            if (items != null)
                foreach (string item in items) box.Items.Add(item);

            return box;
        }

        public static ComboBox DropDown<T>(bool readOnly)
        {
            List<string> names = new List<string>();
            foreach (T value in (T[])Enum.GetValues(typeof(T))) names.Add(Models.EnumText.Spaced(value.ToString()));

            return DropDown(names, readOnly);
        }

        public static DateTimePicker DatePicker(DateTime value)
        {
            DateTimePicker picker = new DateTimePicker();
            picker.Format = DateTimePickerFormat.Custom;
            picker.CustomFormat = "dd MMMM yyyy";
            picker.Font = AppTheme.Body;
            picker.Value = value;
            picker.Height = AppTheme.FieldHeight;
            return picker;
        }

        public static CheckBox CheckBox(string text, bool isChecked)
        {
            CheckBox box = new CheckBox();
            box.Text = text;
            box.Checked = isChecked;
            box.Font = AppTheme.Body;
            box.ForeColor = AppTheme.Ink;
            box.AutoSize = true;
            box.Margin = new Padding(0, AppTheme.Gap1, AppTheme.Gap3, AppTheme.Gap1);
            return box;
        }

        public static RadioButton Radio(string text, bool isChecked)
        {
            RadioButton button = new RadioButton();
            button.Text = text;
            button.Checked = isChecked;
            button.Font = AppTheme.Body;
            button.ForeColor = AppTheme.Ink;
            button.AutoSize = true;
            button.Margin = new Padding(0, AppTheme.Gap1, AppTheme.Gap3, AppTheme.Gap1);
            return button;
        }

        /// <summary>A labelled field: the caption above, the box below, both on
        /// the grid. Every input in the program is built with this, which is why
        /// the columns line up down a form.</summary>
        public static Panel Field(string caption, Control control, int width)
        {
            Panel holder = new Panel();
            holder.Width = width;
            holder.Height = 20 + AppTheme.FieldHeight + AppTheme.Gap1;
            holder.Margin = new Padding(0, 0, AppTheme.Gap3, AppTheme.Gap3);

            Label label = FieldLabel(caption);
            label.Location = new Point(0, 0);
            label.Width = width;

            control.Location = new Point(0, 20);
            control.Width = width;

            holder.Controls.Add(label);
            holder.Controls.Add(control);
            return holder;
        }

        public static Label FieldLabel(string caption)
        {
            Label label = new Label();
            label.Text = caption;
            label.Font = AppTheme.SmallBold;
            label.ForeColor = AppTheme.Muted;
            label.AutoSize = false;
            label.Height = 18;
            label.BackColor = Color.Transparent;
            return label;
        }

        // ==================================================================
        //  Text on the page
        // ==================================================================

        public static Label PageHeading(string text)
        {
            Label label = new Label();
            label.Text = text;
            label.Font = AppTheme.Heading;
            label.ForeColor = AppTheme.Ink;
            label.AutoSize = true;
            label.BackColor = Color.Transparent;
            return label;
        }

        public static Label Caption(string text)
        {
            Label label = new Label();
            label.Text = text;
            label.Font = AppTheme.Small;
            label.ForeColor = AppTheme.Muted;
            label.AutoSize = false;
            label.BackColor = Color.Transparent;
            return label;
        }

        public static Label Body(string text)
        {
            Label label = new Label();
            label.Text = text;
            label.Font = AppTheme.Body;
            label.ForeColor = AppTheme.Ink;
            label.AutoSize = true;
            label.BackColor = Color.Transparent;
            return label;
        }

        public static Label Hint(string text)
        {
            Label label = new Label();
            label.Text = text;
            label.Font = AppTheme.Small;
            label.ForeColor = AppTheme.Muted;
            label.AutoSize = false;
            label.BackColor = Color.Transparent;
            return label;
        }

        // ==================================================================
        //  Grids
        // ==================================================================

        /// <summary>
        /// The table, styled once.
        ///
        /// A default Windows table looks like a spreadsheet from 2003: heavy
        /// lines, grey headings, a blue selection. This one has clean spacing, a
        /// flat heading, no cell borders and full-row selection, which is what
        /// makes a list readable at a glance. Every list in the program is built
        /// by this method, so every list looks the same.
        /// </summary>
        public static DataGridView Grid()
        {
            DataGridView grid = new DataGridView();

            grid.BackgroundColor = AppTheme.Surface;
            grid.BorderStyle = BorderStyle.None;
            grid.GridColor = AppTheme.Border;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            grid.Font = AppTheme.Body;
            grid.RowHeadersVisible = false;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.ReadOnly = true;
            grid.MultiSelect = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.AutoGenerateColumns = true;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.EnableHeadersVisualStyles = false;
            grid.RowTemplate.Height = AppTheme.RowHeight;
            grid.ColumnHeadersHeight = AppTheme.HeaderHeight;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.ScrollBars = ScrollBars.Both;

            grid.ColumnHeadersDefaultCellStyle.BackColor = AppTheme.Canvas;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = AppTheme.Muted;
            grid.ColumnHeadersDefaultCellStyle.Font = AppTheme.SmallBold;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = AppTheme.Canvas;
            grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(AppTheme.Gap2, 0, AppTheme.Gap2, 0);

            grid.DefaultCellStyle.SelectionBackColor = AppTheme.Selection;
            grid.DefaultCellStyle.SelectionForeColor = AppTheme.Ink;
            grid.DefaultCellStyle.ForeColor = AppTheme.Ink;
            grid.DefaultCellStyle.Padding = new Padding(AppTheme.Gap2, 0, AppTheme.Gap2, 0);
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(0xFA, 0xFB, 0xFD);

            return grid;
        }

        /// <summary>Colours the status column the way AppTheme decides, so the
        /// list can be read without reading every word.</summary>
        public static void ColourStatusColumn(DataGridView grid, string columnName)
        {
            grid.CellFormatting += delegate (object sender, DataGridViewCellFormattingEventArgs e)
            {
                if (e.RowIndex < 0 || e.Value == null) return;
                if (grid.Columns[e.ColumnIndex].Name != columnName) return;

                string text = Convert.ToString(e.Value);
                e.CellStyle.ForeColor = AppTheme.ColourFor(text);
                e.CellStyle.SelectionForeColor = AppTheme.ColourFor(text);
                e.CellStyle.Font = AppTheme.SmallBold;
            };
        }

        // ==================================================================
        //  Page furniture
        // ==================================================================

        /// <summary>A page: the soft grey canvas every screen sits on, with the
        /// margins on the grid and scrolling when the window is small.</summary>
        public static Panel Page()
        {
            Panel page = new Panel();
            page.Dock = DockStyle.Fill;
            page.AutoScroll = true;
            page.BackColor = AppTheme.Canvas;
            page.Padding = new Padding(AppTheme.PageMargin, AppTheme.PageMargin,
                                       AppTheme.PageMargin, AppTheme.PageMargin);
            return page;
        }

        public static ToolStripStatusLabel StatusLabel(string text)
        {
            ToolStripStatusLabel label = new ToolStripStatusLabel(text);
            label.Font = AppTheme.Small;
            label.ForeColor = AppTheme.Muted;
            return label;
        }

        // ==================================================================
        //  Button rows
        // ==================================================================

        /// <summary>
        /// A row of buttons that wraps when the window is narrow. I use this
        /// instead of placing buttons one by one, so every screen keeps the
        /// same spacing between its buttons and nobody has to nudge a control
        /// by a pixel in the designer.
        /// </summary>
        public static FlowLayoutPanel ButtonRow(params Control[] controls)
        {
            FlowLayoutPanel row = new FlowLayoutPanel();
            row.AutoSize = true;
            row.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            row.FlowDirection = FlowDirection.LeftToRight;
            row.WrapContents = true;
            row.Margin = new Padding(0, AppTheme.Gap2, 0, 0);
            row.BackColor = Color.Transparent;

            if (controls != null)
                foreach (Control control in controls)
                {
                    if (control == null) continue;
                    control.Margin = new Padding(0, 0, AppTheme.Gap2, AppTheme.Gap2);
                    control.Height = AppTheme.ButtonHeight;
                    row.Controls.Add(control);
                }

            return row;
        }

        /// <summary>Trims a set of buttons that has already been placed on a
        /// form: same height, same gaps, in the order I hand them over. The
        /// buttons keep their own parent, so this works inside a footer panel
        /// that a designer laid out.</summary>
        public static void AlignButtons(params Control[] controls)
        {
            if (controls == null) return;

            foreach (Control control in controls)
            {
                if (control == null) continue;
                control.Height = AppTheme.ButtonHeight;
                control.Margin = new Padding(0, 0, AppTheme.Gap2, 0);
            }
        }
    }
}
