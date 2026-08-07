using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;

namespace CustomWFUI.Styles
{
    // Reusable step/action icons, shipped as embedded resources in
    // CustomWFUI.dll so multiple projects can use them without keeping
    // their own copy of the image files.
    public static class UIIcons
    {
        private static readonly Lazy<Image> WebIcon = new Lazy<Image>(() => LoadIcon("IconWeb.png"));
        private static readonly Lazy<Image> FolderIcon = new Lazy<Image>(() => LoadIcon("IconFolder.png"));
        private static readonly Lazy<Image> DocumentIcon = new Lazy<Image>(() => LoadIcon("IconDocument.png"));
        private static readonly Lazy<Image> ApplicationIcon = new Lazy<Image>(() => LoadIcon("IconApplication.png"));

        public static Image Web => WebIcon.Value;
        public static Image Folder => FolderIcon.Value;
        public static Image Document => DocumentIcon.Value;
        public static Image Application => ApplicationIcon.Value;

        // Creates a scaled copy of an icon, e.g. for buttons that draw the
        // image at a fixed pixel size (Button.Image doesn't scale itself).
        public static Image Resize(Image source, int size)
        {
            if (source == null || size <= 0)
                return source;

            Bitmap resized = new Bitmap(size, size);

            using (Graphics graphics = Graphics.FromImage(resized))
            {
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.DrawImage(source, 0, 0, size, size);
            }

            return resized;
        }

        private static Image LoadIcon(string fileName)
        {
            return LoadEmbedded(Assembly.GetExecutingAssembly(), $"CustomWFUI.Icons.{fileName}");
        }

        /// <summary>
        /// Loads an image from an embedded resource in the given assembly. Intended
        /// for consuming apps to load their own logo/icon the same way CustomWFUI
        /// loads its bundled icons: baked into the assembly instead of a loose file
        /// next to the exe, so it can't go missing or get left behind by an update.
        /// </summary>
        public static Image LoadEmbedded(Assembly assembly, string resourceName)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));

            if (string.IsNullOrWhiteSpace(resourceName))
                throw new ArgumentNullException(nameof(resourceName));

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    string available = string.Join(", ", assembly.GetManifestResourceNames());
                    throw new InvalidOperationException(
                        $"Embedded resource '{resourceName}' was not found in assembly " +
                        $"'{assembly.GetName().Name}'. Available resources: {available}");
                }

                using (Image loaded = Image.FromStream(stream))
                {
                    // Copy it so the stream can be closed safely afterward.
                    return new Bitmap(loaded);
                }
            }
        }
    }
}
