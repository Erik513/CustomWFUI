using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ErikwnkWFUI.Styles
{
    /// <summary>The fonts used across ErikwnkWFUI's controls - fixed, not part of the theme system.</summary>
    public static class UIFonts
    {
        /// <summary>Bold, for headings/titles.</summary>
        public static readonly Font Title = new Font("Segoe UI", 10, FontStyle.Bold);
        /// <summary>Default body text.</summary>
        public static readonly Font Normal = new Font("Segoe UI", 9);
        /// <summary>Smaller, de-emphasized text.</summary>
        public static readonly Font Small = new Font("Segoe UI", 8);
        /// <summary>Fixed-width, for code/tabular text.</summary>
        public static readonly Font Monospace = new Font("Consolas", 9);
        /// <summary>Segoe UI Symbol glyphs, used for icon buttons.</summary>
        public static readonly Font Icon = new Font("Segoe UI Symbol", 13f);
        /// <summary>Color emoji glyphs.</summary>
        public static readonly Font Emoji = new Font("Segoe UI Emoji", 11);
    }
}
