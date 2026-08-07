using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;

namespace CustomWFUI.Styles
{
    // Wiederverwendbare Step-/Aktions-Icons, als eingebettete Ressourcen in
    // CustomWFUI.dll mitgeliefert, damit sie in mehreren Projekten ohne
    // eigene Kopie der Bilddateien genutzt werden können.
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

        // Erstellt eine skalierte Kopie eines Icons, z.B. für Buttons, die das
        // Bild in fester Pixelgröße zeichnen (Button.Image skaliert selbst nicht).
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
            Assembly assembly = Assembly.GetExecutingAssembly();
            string resourceName = $"CustomWFUI.Icons.{fileName}";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    throw new InvalidOperationException(
                        $"Eingebettete Ressource '{resourceName}' wurde nicht gefunden.");
                }

                using (Image loaded = Image.FromStream(stream))
                {
                    // Kopie anlegen, damit der Stream danach gefahrlos geschlossen werden kann.
                    return new Bitmap(loaded);
                }
            }
        }
    }
}
