using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CustomWFUI.Styles;

namespace CustomWFUI.Forms
{
    public class InfoPopupForm : Form
    {
        private const int CornerRadius = 12;
        private const int MaxTextWidth = 260;
        private const int ScreenMargin = 10;
        private const int OwnerOffsetX = 8;
        private const int OwnerOffsetY = -10;

        private readonly Label _titleLabel;
        private readonly Label _textLabel;
        private readonly FlowLayoutPanel _layout;

        public InfoPopupForm(string title = "")
        {
            ConfigureForm();

            _layout = CreateLayoutPanel();
            _titleLabel = CreateTitleLabel(title);
            _textLabel = CreateTextLabel();

            if (!string.IsNullOrWhiteSpace(title))
                _layout.Controls.Add(_titleLabel);

            _layout.Controls.Add(_textLabel);
            Controls.Add(_layout);

            Load += OnFormLoad;
            SizeChanged += OnFormSizeChanged;
        }

        public void ShowInfo(string text, Control owner)
        {
            if (owner == null || owner.IsDisposed)
                return;

            _textLabel.Text = string.IsNullOrWhiteSpace(text)
                ? "Keine"
                : text;

            PerformLayout();

            Location = GetPopupLocation(owner);

            if (!Visible)
                Show(owner.FindForm());

            BringToFront();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && Region != null)
            {
                Region.Dispose();
                Region = null;
            }

            base.Dispose(disposing);
        }

        private void ConfigureForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;

            BackColor = UIColors.PrimaryDark;
            Padding = new Padding(12);

            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
        }

        private FlowLayoutPanel CreateLayoutPanel()
        {
            return new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(8)
            };
        }

        private Label CreateTitleLabel(string title)
        {
            return new Label
            {
                AutoSize = true,
                Text = title,
                Font = UIFonts.Title,
                ForeColor = UIColors.TextPrimary,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 6)
            };
        }

        private Label CreateTextLabel()
        {
            return new Label
            {
                AutoSize = true,
                MaximumSize = new Size(MaxTextWidth, 0),
                Font = UIFonts.Normal,
                ForeColor = UIColors.TextPrimaryDim,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };
        }

        private Point GetPopupLocation(Control owner)
        {
            Point location = owner.PointToScreen(
                new Point(owner.Width + OwnerOffsetX, -Height + owner.Height + OwnerOffsetY));

            Rectangle screen = Screen.FromControl(owner).WorkingArea;

            if (location.Y < screen.Top + ScreenMargin)
                location.Y = screen.Top + ScreenMargin;

            if (location.X + Width > screen.Right)
                location.X = screen.Right - Width - ScreenMargin;

            if (location.X < screen.Left + ScreenMargin)
                location.X = screen.Left + ScreenMargin;

            if (location.Y + Height > screen.Bottom)
                location.Y = screen.Bottom - Height - ScreenMargin;

            return location;
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            ApplyRoundedRegion();
        }

        private void OnFormSizeChanged(object sender, EventArgs e)
        {
            ApplyRoundedRegion();
        }

        private void ApplyRoundedRegion()
        {
            if (ClientRectangle.Width <= 0 || ClientRectangle.Height <= 0)
                return;

            Region oldRegion = Region;
            GraphicsPath path = CreateRoundedRectangle(ClientRectangle, CornerRadius);

            try
            {
                Region = new Region(path);
            }
            finally
            {
                path.Dispose();

                if (oldRegion != null)
                    oldRegion.Dispose();
            }
        }

        private static GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
        {
            int diameter = radius * 2;
            GraphicsPath path = new GraphicsPath();

            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);

            path.CloseFigure();

            return path;
        }
    }
}