using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FileAttributes
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                this.textBox1.Text = dialog.SelectedPath;

                this.FetchFiles();
            }
        }

        private void FetchFiles()
        {
            var path = this.textBox1.Text;
            var files = Directory.GetFiles(path);
            var sb = new StringBuilder();
            foreach (var imagePath in files)
            {
                var line = $"{new FileInfo(imagePath).Name} - ";
                using (Image image = Image.FromFile(imagePath))
                {
                    double? latitude = null;
                    double? longitude = null;
                    double? altitude = null;

                    foreach (PropertyItem propertyItem in image.PropertyItems)
                    {
                        if (propertyItem.Id == 0x0001) // GPSLatitude
                        {
                            latitude = DecodeGps(image.GetPropertyItem(0x0002), propertyItem.Value[0] == 'S');
                            line += $"{latitude}; ";
                        }
                        if (propertyItem.Id == 0x0003) // GPSLongitudeRef
                        {
                            longitude = DecodeGps(image.GetPropertyItem(0x0004), propertyItem.Value[0] == 'W');
                            line += $"{longitude}; ";
                        }
                        if (propertyItem.Id == 0x0006) // GPSAltitude
                        {
                            altitude = DecodeAltitude(propertyItem);
                            line += $"{altitude}";
                        }
                    }
                }
                sb.AppendLine(line);
            }
            textBox2.Text = sb.ToString();
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Text file|*.txt";
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
                File.WriteAllText(saveFileDialog.FileName , sb.ToString());
        }

        private static double DecodeGps(PropertyItem propertyItem, bool isNegative)
        {
            // GPS data is stored as a rational number (numerator/denominator)
            uint[] rational = new uint[propertyItem.Len / 4];
            Buffer.BlockCopy(propertyItem.Value, 0, rational, 0, propertyItem.Len);

            double degrees = rational[0] / (double)rational[1];
            double minutes = rational[2] / (double)rational[3];
            double seconds = rational[4] / (double)rational[5];

            double decimalDegrees = degrees + (minutes / 60.0) + (seconds / 3600.0);

            if (isNegative)
            {
                decimalDegrees = -decimalDegrees;
            }

            return Math.Round(decimalDegrees, 5);
        }

        private static double DecodeAltitude(PropertyItem propertyItem)
        {
            // Altitude is stored as a single rational number
            uint[] rational = new uint[propertyItem.Len / 4];
            Buffer.BlockCopy(propertyItem.Value, 0, rational, 0, propertyItem.Len);

            double altitude = rational[0] / (double)rational[1];

            // Check for the reference (0 = above sea level, 1 = below sea level)
            if (propertyItem.Type == 5 && propertyItem.Len == 4 && propertyItem.Value[0] == 1)
            {
                altitude = -altitude;
            }

            return altitude;
        }
    }
}
