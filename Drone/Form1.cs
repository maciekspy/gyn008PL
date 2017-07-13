using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace Drone
{
    public partial class Form1 : Form
    {
        private static string data_dir = @"data\";
        private static string base_url = "http://gynvael.coldwind.pl/misja008_drone_io/scans/";

        private int cx = -1, cy = - 1;

        private Bitmap map;

        private static float pixPerMeter = 5;
        private static float widthMeter = 1000;
        private static float heightMeter = 900;

        private string[,] urls;

        public Form1()
        {
            InitializeComponent();

            urls = new string[(int)widthMeter, (int)heightMeter];


            map = new Bitmap((int)Math.Floor(widthMeter * pixPerMeter), 
                (int)Math.Floor( heightMeter * pixPerMeter));


            using (Graphics graph = Graphics.FromImage(map))
            {
                Rectangle ImageSize = new Rectangle(0, 0, map.Width, map.Height);
                graph.FillRectangle(Brushes.White, ImageSize);
            }

            panel1.AutoScroll = true;
            pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
            pictureBox1.Image = map;


            string start = "68eb1a7625837e38d55c54dc99257a17.txt";
            scan(start);

        }

        private void set_point(int x, int y, Color c)
        {
            try
            {
                map.SetPixel(x, y, c);
                map.SetPixel(x + 1, y, c);
                map.SetPixel(x - 1, y, c);
                map.SetPixel(x, y + 1, c);
                map.SetPixel(x, y - 1, c);
            }
            catch (Exception ex) { }
        }

        private string get_link(string l)
        {
            if (l == "not possible") return null;
            return l;
        }

        private void parse_scan(string s)
        {
            string[] lines = s.Split('\n');
            if (lines.Length != 42)
                return;

            cx = int.Parse(lines[1].Split(' ')[0]);
            cy = int.Parse(lines[1].Split(' ')[1]);

            int x = (int)Math.Round(cx * pixPerMeter);
            int y = (int)Math.Round(cy * pixPerMeter);

            set_point(x, y, Color.Red);

            for (int i = 0; i < 36; i++)
            {
                float dist;
                if (float.TryParse(lines[2 + i].Replace('.', ','), out dist))
                {
                    int x2 = x + (int)Math.Round(dist * pixPerMeter * Math.Sin(i * Math.PI / 18.0));
                    int y2 = y - (int)Math.Round(dist * pixPerMeter * Math.Cos(i * Math.PI / 18.0));
                    set_point(x2, y2, Color.Black);
                }
            }

            //east
            //west
            //south
            //north
            try
            {
                urls[cx + 1, cy] = get_link(lines[38].Substring(11));
                urls[cx - 1, cy] = get_link(lines[39].Substring(11));
                urls[cx, cy - 1] = get_link(lines[40].Substring(12));
                urls[cx, cy + 1] = get_link(lines[41].Substring(12));
            }
            catch (Exception ex) { }

        }

        private bool scan(string file)
        {
            if (file == null) { 
                
                Debug.WriteLine("No possible");
                return false;
            }

            string s;
            if (File.Exists(data_dir + file))
            {
                s = File.ReadAllText(data_dir + file);
            } else {
                using (WebClient client = new WebClient())
                {
                    s = client.DownloadString(base_url + file);
                }
                Debug.WriteLine("Downloaded: " + file);
                File.WriteAllText(data_dir + file, s);
            }

            parse_scan(s);

            pictureBox1.Image = map;
            pictureBox1.Refresh();
            Debug.WriteLine("Loaded: " + file);
            return true;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            scan(urls[cx, cy - 1]);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            scan(urls[cx, cy + 1]);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            scan(urls[cx + 1, cy]);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            scan(urls[cx - 1, cy]);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            DirectoryInfo d = new DirectoryInfo(data_dir);

            foreach (var file in d.GetFiles("*.txt"))
            {
                Debug.WriteLine(file);
                string s = File.ReadAllText(data_dir + file);
                parse_scan(s);
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            MouseEventArgs me = (MouseEventArgs)e;

            int x = (int)Math.Round(me.Location.X / pixPerMeter);
            int y = (int)Math.Round(me.Location.Y / pixPerMeter);

            if (urls[x, y] != null)
            {
                scan(urls[x, y]);
            }
            else
            {
                bool ret = false;
                do
                {
                    int dx = x - cx;
                    int dy = cy - y;

                    if (dx == 0 && dy == 0) return;

                    if (Math.Abs(dx) > Math.Abs(dy))
                    {
                        ret = scan(urls[cx + Math.Sign(dx), cy]);
                    }
                    else
                    {
                        ret = scan(urls[cx, cy + Math.Sign(dy)]);
                    }

                } while (ret);
            }

        }

        private void button7_Click(object sender, EventArgs e)
        {
            map.Save("map.png", System.Drawing.Imaging.ImageFormat.Png);
        }

    }
}
