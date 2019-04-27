using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DiplomCS
{
    public partial class Form1 : Form
    {
        /*struct Pixel : IEquatable<Pixel>
        {
            public byte Blue;
            public byte Green;
            public byte Red;
            public byte Alpha;

            public bool Equals(Pixel other)
            {
                return Red == other.Red && Green == other.Green && Blue == other.Blue && Alpha == other.Alpha;
            }
        }*/

        /*struct Picturess
        {
            string[,] pics;

            string Path { get; set; }
            string Name { get; set; }
            int Num { get; set; }
            int Count { get; set; }

            void Set_count()
            {
                pics = new string[2, Count];
            }

            void Set_path(int i)
            {

            }
        }*/

        private const int rect_size = 5, // размер изображения, посылаемого на обработку нейронной сети, по умолчанию: 5, т.е. изображение 5х5
                    c_solution = 3; // способы ускорения работы программы:
                                    // 1 - доработанные GetPixel и SetPixel
                                    // 2 - указатели
                                    // 3 - указатели + LockBits
                                    // 4 - указатели + LockBits - массивы
                                    // 5 - assembler
        private int input_count,
                    files_count;
        private string[,] files_array;

        private NeuralNetwork Nn = new NeuralNetwork();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            input_count = rect_size * rect_size * 3;

            if (rect_size != 5)
                Nn.Recreate_weights(rect_size);
        }
        // исправить
        private void Get_files_list()
        {
            FolderBrowserDialog OpenF = new FolderBrowserDialog();

            if (OpenF.ShowDialog() == DialogResult.OK)
            {
                List<string> paths_list = new List<string>();
                List<string> names_list = new List<string>();

                //Picturess.;

                //------------------------------------
                /*ListView listView = listView1;
                listView.View = View.Details;
                int k = 0;
                Color shaded = Color.FromArgb(240, 240, 240);

                foreach (Product product in products)
                {
                    ListViewItem item = new ListViewItem(product.Name);
                    item.SubItems.Add(product.Version);
                    item.SubItems.Add(product.Description);
                    item.SubItems.Add(product.Status);
                    if (k++ % 2 == 1)
                    {
                        item.BackColor = shaded;
                        item.UseItemStyleForSubItems = true;
                    }
                    listView.Items.Add(item);
                }*/
                //------------------------------------

                DirectoryInfo path = new DirectoryInfo(OpenF.SelectedPath);
                var list = path.GetFiles("*.png", SearchOption.TopDirectoryOnly);

                foreach (var fi in list.OrderBy(c => c.FullName)) // добавление новых файлов в лист
                    paths_list.Add(fi.FullName);

                if (paths_list.Count != 0)
                {
                    names_list = Get_names_list(paths_list);
                    files_count = names_list.Count / 2;
                    files_array = new string[4, files_count]; // = (int)Math.Ceiling(names_list.Count / 2d)       (string[,])ResizeArray(files_array, new int[] { 4, names_list.Count / 2 });

                    int j = 0;
                    for (int i = 0; i < paths_list.Count; i++)
                    {
                        if (!Is_ndvi(names_list[i]))
                        {
                            files_array[0, j] = paths_list[i];
                            files_array[1, j] = names_list[i];
                            comboBox1.Items.Add(files_array[1, j]);
                        }
                        else
                        {
                            Get_ndvi_range(names_list[i], j);
                            j++;
                        }
                        //int x = Int32.Parse(TextBoxD1.Text);
                    }
                }
            }
        }

        private List<string> Get_names_list(List<string> paths_list)
        {
            string str;
            List<string> names_list = new List<string>();

            names_list.Clear();
            comboBox1.Items.Clear();

            foreach (string path in paths_list.OrderBy(c => c))
            {
                str = Get_name(path); // paths_list[i]
                names_list.Add(str);
            }

            return names_list;
        }

        private string Get_name(string path)
        {
            string c = "", name = "";
            char ch = ' ';
            int startIndex = 0, length = 0;

            for (int i = 0; i < path.Length; i++)
            {
                ch = path[i];
                c = ch.ToString();

                if (c == "\\")
                {
                    startIndex = i + 1;
                    length = 0;
                }
                else
                    length++;

                if (c == ".")
                {
                    length--;
                    break;
                }
            }

            name = path.Substring(startIndex, length);
            return name;
        }

        private bool Is_ndvi(string name)
        {
            bool res = false;
            for (int i = 0; i < name.Length - 3; i++)
                if (name[i] == 'n' && name[i + 1] == 'd' && name[i + 2] == 'v' && name[i + 3] == 'i')
                {
                    res = true;
                    break;
                }
            return res;
        }

        private void Get_ndvi_range(string name, int i)
        {
            files_array[2, i] = name.Substring(name.Length - 5, 3); // нижний порог
            if (name[name.Length - 2] == '-')
                files_array[3, i] = name.Substring(name.Length - 2, 3); // верхний порог
            else
                files_array[3, i] = name.Substring(name.Length - 2, 2); // верхний порог
            //files_array[2, i] = name.Substring(name.Length - 4, 3); // нижний порог
        }

        private void Enable_dir_elements()
        {
            if (files_array != null)
            {
                label2.Visible = false;
                comboBox2.Visible = false;
                comboBox2.Enabled = false;

                Save_as.Enabled = false;
                Save_picture.Enabled = false;

                label1.Visible = true;
                label1.Text = "Выберите файл:";
                label1.Location = new Point(ClientSize.Width - 129, 25); // - comboBox1.Size.Width - 8

                comboBox1.Visible = true;
                comboBox1.Enabled = true;
                comboBox1.Location = new Point(ClientSize.Width - 129, label1.Location.Y + 18);
                comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
            }
        }

        private void Combobox_change_elements()
        {
            label2.Visible = false;
            comboBox2.Visible = false;
            comboBox2.Enabled = false;

            label1.Location = new Point(ClientSize.Width - 129, 25);
            //label1.Location = new Point(ClientSize.Width - label1.Size.Width - 8, 25);

            comboBox1.Location = new Point(ClientSize.Width - 129, label1.Location.Y + 18);

            label3.Location = new Point(ClientSize.Width - 129, comboBox1.Location.Y + 26);
        }

        private void Pictures_change_elements()
        {
            if (ClientSize.Height - 60 < label1.Location.X - 44)
            {
                pictureBox1.Size = new Size(ClientSize.Height / 2 - 60, ClientSize.Height / 2 - 60);
                pictureBox2.Size = new Size(ClientSize.Height / 2 - 60, ClientSize.Height / 2 - 60);
                pictureBox3.Size = new Size(ClientSize.Height / 2 - 60, ClientSize.Height / 2 - 60);
                pictureBox4.Size = new Size(ClientSize.Height / 2 - 60, ClientSize.Height / 2 - 60);
            }
            else
            {
                pictureBox1.Size = new Size(label1.Location.X / 2 - 44, label1.Location.X / 2 - 44);
                pictureBox2.Size = new Size(label1.Location.X / 2 - 44, label1.Location.X / 2 - 44);
                pictureBox3.Size = new Size(label1.Location.X / 2 - 44, label1.Location.X / 2 - 44);
                pictureBox4.Size = new Size(label1.Location.X / 2 - 44, label1.Location.X / 2 - 44);
            }

            pictureBox2.Location = new Point(pictureBox1.Size.Width + 32, 28);
            pictureBox3.Location = new Point(12, pictureBox1.Size.Height + 48);
            pictureBox4.Location = new Point(pictureBox1.Size.Width + 32, pictureBox1.Size.Height + 48);
        }

        private void Initiate_learning()
        {
            //for (int j = 0; j < 50; j++)
                for (int i = 0; i < files_count; i++)
                {
                    Bitmap pic_rgb = new Bitmap(files_array[0, i]);
                    Bitmap pic_ndvi = new Bitmap(files_array[0, i].Substring(0, files_array[0, i].Length - 4) + "ndvi" + files_array[2, i] + files_array[3, i] + ".png");

                    pictureBox1.Image = pic_rgb;
                    pictureBox2.Image = pic_ndvi;

                    if (pic_ndvi != null)
                        Rects_from_pic(pic_rgb, pic_ndvi);
                    else
                        Rects_from_pic(pic_rgb);
                }

            Pictures_change_elements();
        }
        // доработать
        #region 1 способ (доработанные GetPixel и SetPixel)
        private int[,,] MGetPixel(Bitmap bmp, int start_x, int start_y)
        {
            DirectBitmap dbm = new DirectBitmap(bmp);
            int[,,] res = new int[rect_size, rect_size, 3];

            int l = 0, d = 0;
            for (int j = start_y; j < rect_size + start_y; j++)
            {
                for (int i = start_x; i < rect_size + start_x; i++)
                {
                    res[l, d, 0] = dbm.GetPixel(j, i).R;
                    res[l, d, 1] = dbm.GetPixel(j, i).G;
                    res[l, d, 2] = dbm.GetPixel(j, i).B;
                    l++;
                }
                l = 0;
                d++;
            }
            dbm.Dispose();

            return res;
        }

        private Bitmap MSetPixel(Bitmap bmp, int[,,] arr, int start_x, int start_y)
        {
            Color clr;
            DirectBitmap dbm = new DirectBitmap(bmp);

            int l = 0, d = 0;
            for (int j = start_y; j < rect_size + start_y; j++)
            {
                for (int i = start_x; i < rect_size + start_x; i++)
                {
                    clr = Color.FromArgb(255, arr[l, d, 0], arr[l, d, 1], arr[l, d, 2]);
                    dbm.SetPixel(j, i, clr);
                    l++;
                }
                l = 0;
                d++;
            }
            dbm.Dispose();

            return bmp;
        }
        #endregion 1 способ (доработанные GetPixel и SetPixel)

        #region 2 способ (указатели)
        /*unsafe {
            var oneBits = one.LockBits(new Rectangle(0, 0, one.Width, one.Height), ImageLockMode.ReadOnly, one.PixelFormat);
            var twoBits = two.LockBits(new Rectangle(0, 0, two.Width, two.Height), ImageLockMode.ReadOnly, two.PixelFormat);
            var thrBits = thr.LockBits(new Rectangle(0, 0, thr.Width, thr.Height), ImageLockMode.WriteOnly, thr.PixelFormat);

            int padding = twoBits.Stride - (two.Width * sizeof(Pixel));

            int width = two.Width;
            int height = two.Height;

            Parallel.For(0, one.Width * one.Height, i => {
                Pixel* pxOne = (Pixel*)((byte*)oneBits.Scan0 + i * sizeof(Pixel));

                byte* ptr = (byte*)twoBits.Scan0;

                for (int j = 0; j < height; j++) {
                    for (int k = 0; k < width; k++) {
                        Pixel* pxTwo = (Pixel*)ptr;
                        if (pxOne->Equals(*pxTwo)) {
                            Pixel* pxThr = (Pixel*)((byte*)thrBits.Scan0 + i * sizeof(Pixel));
                            pxThr->Alpha = pxThr->Red = pxThr->Green = pxThr->Blue = 0xFF;
                        }
                        ptr += sizeof(Pixel);
                    }
                    ptr += padding;
                }
            });

            one.UnlockBits(oneBits);
            two.UnlockBits(twoBits);
            thr.UnlockBits(thrBits);
        }*/
        #endregion 2 способ (указатели)

        #region 3 способ (указатели + LockBits) используется
        public unsafe int[,,] LBGetPixel(Bitmap bmp, int start_x, int start_y)
        {
            int width = bmp.Width, height = bmp.Height;
            byte[,,] res = new byte[3, height, width];
            int[,,] res_int = new int[rect_size, rect_size, 3];

            BitmapData bd = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

            try
            {
                fixed (byte* _res = res)
                {
                    byte* _r = _res,
                          _g = _res + width * height,
                          _b = _res + 2 * width * height;

                    for (int h = 0; h < height; h++)
                    {
                        byte* curpos = ((byte*)bd.Scan0) + h * bd.Stride;
                        for (int w = 0; w < width; w++)
                        {
                            *_b = *curpos++;
                            ++_b;
                            *_g = *curpos++;
                            ++_g;
                            *_r = *curpos++;
                            ++_r;
                        }
                    }
                }

                int l = 0, d = 0;
                for (int h = start_y; h < rect_size + start_y; h++)
                {
                    for (int w = start_x; w < rect_size + start_x; w++)
                    {
                        res_int[l, d, 0] = res[0, h, w];
                        res_int[l, d, 1] = res[1, h, w];
                        res_int[l, d, 2] = res[2, h, w];
                        l++;
                    }
                    l = 0;
                    d++;
                }
            }
            finally
            {
                bmp.UnlockBits(bd);
            }

            return res_int;
        }
        // изменить
        public unsafe Bitmap LBSetPixel(Bitmap bmp, int[,,] arr, int start_x, int start_y)
        {
            int width = bmp.Width, height = bmp.Height;
            byte[,,] res = new byte[3, height, width];

            BitmapData bd = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            try
            {
                int d = 0,
                    end_x = start_x + rect_size,
                    end_y = start_y + rect_size;

                for (int h = start_y; h < end_y; h++)
                {
                    int l = 0;
                    for (int w = start_x; w < end_x; w++)
                    {
                        res[0, h, w] = (byte)arr[l, d, 0];
                        res[1, h, w] = (byte)arr[l, d, 1]; // w = 70
                        res[2, h, w] = (byte)arr[l, d, 2]; // w = 71
                        l++;
                    }
                    d++;
                }
                
                fixed (byte* _res = res)
                {
                    int start_pos = start_y * width + start_x,
                        r_layer = 0,
                        g_layer = width * height,
                        b_layer = 2 * width * height;

                    byte* _r = _res + r_layer,
                          _g = _res + g_layer,
                          _b = _res + b_layer; // + start_pos

                    d = 0;
                    for (int h = start_y; h < end_y; h++)
                    {
                        int l = 0;
                        byte* curpos = (byte*)bd.Scan0 + h * bd.Stride; // (width * 3 + 1)
                        for (int w = start_x; w < end_x; w++)
                        {
                            curpos[start_x * 3 + 0] = (byte)arr[l, d, 2]; // *_b++
                            curpos[start_x * 3 + 1] = (byte)arr[l, d, 1]; // *_g++
                            curpos[start_x * 3 + 2] = (byte)arr[l, d, 0]; // *_r++
                            curpos += 3;
                            l++;
                        }
                        d++;
                    }
                }
            }
            finally
            {
                bmp.UnlockBits(bd);
            }

            return bmp;
        }
        #endregion 3 способ (указатели + LockBits) используется

        #region 4 способ (указатели + LockBits - массивы)
        /*
         */
        #endregion 4 способ (указатели + LockBits - массивы)

        #region 5 способ (assembler)
        /*
         */
        #endregion 5 способ (assembler)

        private void Rects_from_pic(Bitmap pic_rgb, Bitmap pic_ndvi)
        {
            int wd = pic_rgb.Size.Width,
                hd = pic_rgb.Size.Height;

            Bitmap taken_rgb = new Bitmap(wd, hd);
            Bitmap taken_ndvi = new Bitmap(wd, hd);
            Bitmap taken_prec = new Bitmap(wd, hd);

            for (int j = 0; j < hd - rect_size; j++)
            {
                for (int i = 0; i < wd - rect_size; i++)
                {
                    int[] rgb_res = new int[input_count];
                    int[] ndvi_res = new int[input_count];
                    int[,,] s_argb = new int[rect_size, rect_size, 3];
                    int[,,] s_ndvi = new int[rect_size, rect_size, 3];

                    switch (c_solution)
                    {
                        case 1:
                            s_argb = MGetPixel(pic_rgb, i, j);
                            s_ndvi = MGetPixel(pic_ndvi, i, j);
                            break;
                        case 2:
                            break;
                        case 3:
                            s_argb = LBGetPixel(pic_rgb, i, j);
                            s_ndvi = LBGetPixel(pic_ndvi, i, j);
                            break;
                        case 4:
                            break;
                        case 5:
                            break;
                    }

                    Give_data(s_argb, s_ndvi);
                    ndvi_res = Nn.Analyse();
                    rgb_res = Fill_1dim_array(s_argb);

                    taken_ndvi = Paint_ndvi(taken_ndvi, ndvi_res, i, j);
                    taken_rgb = Cut_rgb_image(taken_rgb, rgb_res, ndvi_res, i, j);
                }
            }

            //taken_prec = Calculate_precision((Bitmap)pictureBox2.Image, taken_ndvi);
            pictureBox3.Image = taken_rgb;
            pictureBox4.Image = taken_ndvi;
            //pictureBox5.Image = taken_prec;
        }
        private void Rects_from_pic(Bitmap pic_rgb)
        {
            int wd = pic_rgb.Size.Width,
                hd = pic_rgb.Size.Height;

            Bitmap taken_argb = new Bitmap(wd, hd);
            Bitmap taken_ndvi = new Bitmap(wd, hd);

            for (int j = 0; j < hd - rect_size; j++)
            {
                for (int i = 0; i < wd - rect_size; i++)
                {
                    int[] rgb_res = new int[input_count];
                    int[] ndvi_res = new int[input_count];

                    switch (c_solution)
                    {
                        case 1:
                            Give_data(MGetPixel(pic_rgb, i, j));
                            break;
                        case 2:
                            break;
                        case 3:
                            Give_data(LBGetPixel(pic_rgb, i, j));
                            break;
                        case 4:
                            break;
                        case 5:
                            break;
                    }
                    ndvi_res = Nn.Analyse();

                    taken_ndvi = Paint_ndvi(taken_ndvi, ndvi_res, i, j);
                    taken_argb = Cut_rgb_image(taken_argb, rgb_res, ndvi_res, i, j);
                }
            }

            pictureBox3.Image = taken_argb;
            pictureBox4.Image = taken_ndvi;
        }

        private void Give_data(int[,,] rgb, int[,,] ndvi)
        {
            for (int j = 0; j < rect_size; j++)
                for (int i = 0; i < rect_size; i++)
                    if (ndvi[i, j, 2] > 80 || (ndvi[i, j, 0] == 0 && ndvi[i, j, 0] == 0 && ndvi[i, j, 0] == 0)) // ndvi[i, j, 0] < 150 &&
                    {
                        ndvi[i, j, 0] = 0;
                        ndvi[i, j, 1] = 0;
                        ndvi[i, j, 2] = 0;
                    }

            Nn.Take_rgb(Fill_1dim_array(rgb), rect_size);
            Nn.Take_ndvi(Fill_1dim_array(ndvi));
        }
        private void Give_data(int[,,] pic)
        {
            Nn.Take_rgb(Fill_1dim_array(pic), rect_size);
        }

        private Bitmap Paint_ndvi(Bitmap bmp, int[] arr, int start_x, int start_y)
        {
            Bitmap res = bmp;
            int k = 0,
                end_x = bmp.Width - rect_size,
                end_y = bmp.Height - rect_size;
            int[,,] rgb_arr = Fill_3dim_array(arr);

            for (int j = start_y; j < start_y + rect_size || j < end_y; j++)
            {
                for (int i = start_x; i < start_x + rect_size || i < end_x; i++)
                {
                    //bmp.SetPixel(i, j, Color.FromArgb(255, arr[k], arr[k + 1], arr[k + 2]));
                    bmp = LBSetPixel(bmp, rgb_arr, i, j);
                    //k = k + 3;
                }
            }

            return res;
        }

        private Bitmap Paint_rgb(Bitmap bmp, int[] rgb_array, int[] ndvi_array, int start_x, int start_y)
        {
            Bitmap res = bmp;

            return res;
        }

        private Bitmap Cut_rgb_image(Bitmap bmp, int[] rgb_array, int[] ndvi_array, int start_x, int start_y)
        {
            Bitmap res = bmp;
            int k = 0,
                end_x = bmp.Width - rect_size,
                end_y = bmp.Height - rect_size;
            int[,,] arr = Fill_3dim_array(rgb_array);

            for (int j = start_y; j < start_y + rect_size || j < end_y; j++)
            {
                for (int i = start_x; i < start_x + rect_size || i < end_x; i++)
                {
                    //if (ndvi_array[k] != 0 && ndvi_array[k + 1] != 0 && ndvi_array[k + 2] != 0)
                        bmp = LBSetPixel(bmp, arr, i, j);
                    //bmp.SetPixel(i, j, Color.FromArgb(0, 0, 0, 0));
                    //else
                    //bmp.SetPixel(i, j, Color.FromArgb(255, rgb_array[k], rgb_array[k + 1], rgb_array[k + 2]));
                    k = k + 3;
                }
            }

            return res;
        }

        private Bitmap Calculate_precision(Bitmap ndvi, Bitmap ans)
        {
            int wd = ndvi.Width,
                hd = ndvi.Height;
            Bitmap res = new Bitmap(wd, hd);

            //DirectBitmap res_dbm = new DirectBitmap(wd, hd);
            //DirectBitmap ndvi_dbm = new DirectBitmap(ndvi);
            //DirectBitmap ans_dbm = new DirectBitmap(ans);

            for (int j = 0; j < hd; j++)
                for(int i = 0; i < wd; i++)
                {
                    //ndvi.SetPixel(i, j, clr);
                }

            //res = res_dbm.ToBmp();

            return res;
        }

        private int[] Fill_1dim_array(int[,,] arr)
        {
            int k = 0;
            int[] res = new int[input_count];

            for (int j = 0; j < rect_size; j++)
            {
                for (int i = 0; i < rect_size; i++)
                {
                    res[k] = arr[i, j, 0];
                    ++k;
                    res[k] = arr[i, j, 1];
                    ++k;
                    res[k] = arr[i, j, 2];
                    ++k;
                }
            }

            return res;
        }

        private int[,,] Fill_3dim_array(int[] arr)
        {
            int k = 0;
            int[,,] res = new int[rect_size, rect_size, 3];

            for (int j = 0; j < rect_size; j++)
            {
                for (int i = 0; i < rect_size; i++)
                {
                    res[i, j, 0] = arr[k];
                    ++k;
                    res[i, j, 1] = arr[k];
                    ++k;
                    res[i, j, 2] = arr[k];
                    ++k;
                }
            }

            return res;
        }

        private static Array ResizeArray(Array arr, int[] newSizes)
        {
            // Стандартный метод с официального сайта //
            if (newSizes.Length != arr.Rank)
                throw new ArgumentException("arr must have the same number of dimensions " +
                                            "as there are elements in newSizes", "newSizes");

            var temp = Array.CreateInstance(arr.GetType().GetElementType(), newSizes);
            int length = arr.Length <= temp.Length ? arr.Length : temp.Length;
            Array.ConstrainedCopy(arr, 0, temp, 0, length);
            return temp;
        }

        //События--------------------------------------------------//

        private void Open_directory_Click(object sender, EventArgs e)
        {
            Get_files_list();
            //Initiate_learning();
            Enable_dir_elements();
        }
        // пусто
        private void Save_as_Click(object sender, EventArgs e)
        {

        }
        // пусто
        private void Save_picture_Click(object sender, EventArgs e)
        {

        }

        private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex > -1)
            {
                Bitmap pic_rgb = new Bitmap(files_array[0, comboBox1.SelectedIndex]);
                Bitmap pic_ndvi = new Bitmap(files_array[0, comboBox1.SelectedIndex].Substring(0, files_array[0, comboBox1.SelectedIndex].Length - 4) + "ndvi" + files_array[2, comboBox1.SelectedIndex] + files_array[3, comboBox1.SelectedIndex] + ".png");

                pictureBox1.Image = pic_rgb;
                pictureBox2.Image = pic_ndvi;

                if (pic_ndvi != null)
                    Rects_from_pic(pic_rgb, pic_ndvi);
                else
                    Rects_from_pic(pic_rgb);

                //comboBox2.SelectedIndex = 0;
                Combobox_change_elements();
                Pictures_change_elements();
            }
        }

        private void Open_rgb_Click(object sender, EventArgs e)
        {
            Get_files_list();

            if (files_array != null)
            {
                Random rnd = new Random();
                Bitmap pic_rgb = new Bitmap(files_array[0, rnd.Next(0, 200)]);

                int wd = pic_rgb.Width, hd = pic_rgb.Height;

                Color[,] array = new Color[wd, hd];
                Bitmap res = new Bitmap(wd, hd);

                for (int j = 0; j < hd + 1 - rect_size; j++)
                {
                    for (int i = 0; i < wd + 1 - rect_size; i++)
                    {
                        res = LBSetPixel(res, LBGetPixel(pic_rgb, i, j), i, j);
                    }
                }
                
                pictureBox1.Image = pic_rgb;
                pictureBox2.Image = res;
                Pictures_change_elements();
            }
        }

        // пусто
        private void ComboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            //if (comboBox2.SelectedIndex > -1)
            switch (comboBox2.SelectedIndex)
            {
                case 0:
                    // 0. исходное изображение
                    pictureBox1.Image = new Bitmap(files_array[0, comboBox1.SelectedIndex]);
                    Pictures_change_elements();
                    break;
                case 1:
                    // 1. густая растительность         ~ 0.70
                    break;
                case 2:
                    // 2. разражённая растительность    ~ 0.50
                    break;
                case 3:
                    // 3. открытая почва                ~ 0.25
                    break;
                case 4:
                    // 4. облака                        ~ 0.00
                    break;
                case 5:
                    // 5. снег и лёд                    ~-0.05
                    break;
                case 6:
                    // 6. вода                          ~-0.25
                    break;
                case 7:
                    // 7. искусственные материалы       ~-0.50
                    break;
            }
        }

        private void Form_ChangedSize(object sender, EventArgs e)
        {
            Combobox_change_elements();
            Pictures_change_elements();
        }
    }

/*==========================*\
||      Нейронная сеть      ||
\*==========================*/

    public class NeuralNetwork
    {
        private const bool use_sigmoid = true;

        private bool has_answer = false;

        private int input_count,
                    hidden_count,
                    iteration = 0;

        private int[] taken_ans,
                      output_data;

        private double learning_rate = 0.5,
                       precision;

        private double[] input_layer,
                         first_hidden_layer,
                       m_first_hidden_layer,
                         //second_hidden_layer;
                         //third_hidden_layer;
                         //fourth_hidden_layer;
                         //fivth_hidden_layer;
                         output_layer,
                       m_output_layer,
                         error,
                         weights_1_delta,
                         weights_o_delta;

        private double[,] weights_1,
                          weights_2;

        public void Take_rgb(int[] pic, int input_size)
        {
            input_count = input_size * input_size * 3;
            hidden_count = input_size * 100;
            input_layer = new double[input_count];

            for (int i = 0; i < input_count; i++)
                input_layer[i] = (double)pic[i] / 255;
        }

        public void Take_ndvi(int[] ans)
        {
            taken_ans = new int[input_count];

            for (int i = 0; i < input_count; i++)
                taken_ans[i] = ans[i];

            has_answer = true;
        }

        public void Recreate_weights(int input_size)
        {
            double num;
            Random rnd = new Random();

            input_count = input_size * input_size * 3;
            hidden_count = input_size * 100;

            weights_1 = new double[hidden_count, input_count];
            weights_2 = new double[hidden_count, input_count];

            for (int j = 0; j < hidden_count; j++)
            {
                for (int i = 0; i < input_count; i++)
                {
                    num = rnd.Next(10, 90);
                    num = num / 100;

                    weights_1[j, i] = num;
                    weights_2[j, i] = num;
                }
            }

            Serializer.Save("weights01.bin", weights_1);
            Serializer.Save("weights02.bin", weights_2);
        }

        public int[] Analyse()
        {
            weights_1 = new double[hidden_count, input_count];
            weights_2 = new double[hidden_count, input_count];

            weights_1 = Serializer.Load_w(input_count, hidden_count, "weights01.bin");
            weights_2 = Serializer.Load_w(input_count, hidden_count, "weights02.bin");

            Fill_hidden_layer(); // заполняется средний слой
            Activation_function(); // средний мод. слой заполняется сигмоидом среднего слоя
            Fill_output_layer(); // заполняется выходной слой
            Activation_function(); // выходной мод. слой заполняется сигмоидом выходного слоя
            Output_convertation();

            if (has_answer)
            {
                Error_catching();
                Get_outputw_delta();
                Change_output_weights();

                Error_catching();
                Get_w1_delta();
                Change_weights_1();

                Serializer.Save("weights01.bin", weights_1);
                Serializer.Save("weights02.bin", weights_2);
            }

            return output_data;
        }

        public double Return_precision()
        {
            return precision;
        }

        //Анализ данных---------------//

        private void Fill_hidden_layer()
        {
            first_hidden_layer = new double[hidden_count];

            for (int j = 0; j < hidden_count; j++)
                for (int i = 0; i < input_count; i++)
                    first_hidden_layer[j] = first_hidden_layer[j] + input_layer[i] * weights_1[j, i];
        }

        private void Fill_output_layer()
        {
            output_layer = new double[input_count];

            for (int j = 0; j < hidden_count; j++)
                for (int i = 0; i < input_count; i++)
                    output_layer[i] = output_layer[i] + m_first_hidden_layer[j] * weights_2[j, i];
        }

        private void Output_convertation()
        {
            output_data = new int[input_count];

            for (int i = 0; i < input_count; i++)
                output_data[i] = (int)(m_output_layer[i] * 255);
        }

        //Обучение-----------------//

        private void Error_catching()
        {
            if (use_sigmoid)
            {
                switch (iteration)
                {
                    case 0:
                        error = new double[input_count];

                        for (int i = 0; i < input_count; i++)
                        {
                            error[i] = m_output_layer[i] - ((double)taken_ans[i] / 255); // Sigmoid((double)taken_ans[i] / 255)
                            //error[i] = Sigmoid(m_output_data[i] - taken_ans[i]);
                            precision = precision + error[i];
                        }
                        iteration++;
                        break;
                    case 1:
                        error = new double[hidden_count];

                        for (int j = 0; j < hidden_count; j++)
                            for (int i = 0; i < input_count; i++)
                                error[j] = weights_2[j, i] * weights_o_delta[i];

                        iteration = 0;
                        break;
                }
            }
            else
            {
                switch (iteration)
                {
                    case 0:
                        error = new double[input_count];

                        for (int i = 0; i < input_count; i++)
                        {
                            error[i] = m_output_layer[i] - (double)taken_ans[i] / 255;
                            //error[i] = Sigmoid(s_output_data[i] - taken_ans[i]);
                            precision = precision + error[i];
                        }
                        iteration++;
                        break;
                    case 1:
                        error = new double[hidden_count];

                        for (int j = 0; j < hidden_count; j++)
                            for (int i = 0; i < input_count; i++)
                                error[j] = weights_2[j, i] * weights_o_delta[i];

                        iteration = 0;
                        break;
                }
            }
        }

        private void Get_w1_delta()
        {
            weights_1_delta = new double[hidden_count];

            for (int i = 0; i < hidden_count; i++)
                weights_1_delta[i] = error[i] * m_first_hidden_layer[i] * (1 - m_first_hidden_layer[i]);
        }

        private void Get_outputw_delta()
        {
            weights_o_delta = new double[input_count];

            for (int i = 0; i < input_count; i++)
                weights_o_delta[i] = error[i] * m_output_layer[i] * (1 - m_output_layer[i]);
        }

        private void Change_weights_1()
        {
            for (int j = 0; j < hidden_count; j++)
                for (int i = 0; i < input_count; i++)
                    weights_1[j, i] = weights_1[j, i] - input_layer[i] * weights_1_delta[j] * learning_rate;
        }

        private void Change_output_weights()
        {
            for (int j = 0; j < hidden_count; j++)
                for (int i = 0; i < input_count; i++)
                    weights_2[j, i] = weights_2[j, i] - m_first_hidden_layer[j] * weights_o_delta[i] * learning_rate;
        }

        //Многозадачные функции-------------------------//

        private void Activation_function()
        {
            if (use_sigmoid)
            {
                switch (iteration)
                {
                    case 0:
                        m_first_hidden_layer = new double[hidden_count];

                        for (int i = 0; i < hidden_count; i++)
                        {
                            m_first_hidden_layer[i] = Sigmoid(first_hidden_layer[i] / 100);
                            m_first_hidden_layer[i] = 45 * (m_first_hidden_layer[i] - 0.53);
                        }
                        iteration++;
                        break;
                    case 1:
                        m_output_layer = new double[input_count];

                        for (int i = 0; i < input_count; i++)
                            m_output_layer[i] = Sigmoid(output_layer[i] / 10);
                        iteration = 0;
                        break;
                }
            }
            else
            {
                switch (iteration)
                {
                    case 0:
                        m_first_hidden_layer = new double[hidden_count];

                        for (int i = 0; i < hidden_count; i++)
                            m_first_hidden_layer[i] = first_hidden_layer[i];
                        iteration++;
                        break;
                    case 1:
                        m_output_layer = new double[input_count];

                        for (int i = 0; i < input_count; i++)
                            m_output_layer[i] = output_layer[i];
                        iteration = 0;
                        break;
                }
            }
        }

        private double Sigmoid(double x)
        {
            return 1 / (1 + Math.Pow(Math.E, -x));
        }

        // сделать построение своей ndvi сетки
        // сделать сравнение двух сеток ndvi

        // типы местности:
        // 1. густая растительность
        // 2. разряжённая растительность
        // 3. открытая почва
        // 4. облака
        // 5. вода
        // 6. искусственные материалы

        // Geoprocessing > File > Satellite Imagery > Import Landsat Scene
        //               > MTL > -, -, reflectance
        // Geoprocessing > Visualization > Grid > RGB Composite
        //               > 30 > 4, 3, 2
        // Geoprocessing > Grid > Calculus > Grid Calculation
    }

    public static class Serializer
    {
        public static void Save(string filePath, object objToSerialize)
        {
            try
            {
                using (Stream stream = File.Open(filePath, FileMode.Create))
                {
                    BinaryFormatter bin = new BinaryFormatter();
                    bin.Serialize(stream, objToSerialize);
                }
            }
            catch (IOException)
            {

            }
        }

        public static T Load<T>(string filePath) where T : new()
        {
            T rez = new T();

            try
            {
                using (Stream stream = File.Open(filePath, FileMode.Open))
                {
                    BinaryFormatter bin = new BinaryFormatter();
                    rez = (T)bin.Deserialize(stream);
                }
            }
            catch (IOException)
            {

            }

            return rez;
        }

        public static double[,] Load_w(int wd, int hd, string filePath)
        {
            double[,] res = new double[hd, wd];

            using (FileStream fs = new FileStream(filePath, FileMode.OpenOrCreate))
            {
                BinaryFormatter dat = new BinaryFormatter();
                res = (double[,])dat.Deserialize(fs);
            }

            return res;
        }
    }

    public unsafe class UnsafeBitmap
    {
        Bitmap bmp;
        int width;
        BitmapData bitmapData = null;
        byte* pBase = null;

        public UnsafeBitmap(Bitmap bmp)
        {
            this.bmp = new Bitmap(bmp);
        }

        public UnsafeBitmap(int width, int height)
        {
            this.bmp = new Bitmap(width, height);
        }

        public void Dispose()
        {
            bmp.Dispose();
        }

        public Bitmap Bitmap
        {
            get
            {
                return bmp;
            }
        }

        private Point PixelSize
        {
            get
            {
                GraphicsUnit unit = GraphicsUnit.Pixel;
                RectangleF bounds = bmp.GetBounds(ref unit);

                return new Point((int)bounds.Width, (int)bounds.Height);
            }
        }

        public void LockBitmap()
        {
            GraphicsUnit unit = GraphicsUnit.Pixel;
            RectangleF boundsF = bmp.GetBounds(ref unit);
            Rectangle bounds = new Rectangle((int)boundsF.X, (int)boundsF.Y, (int)boundsF.Width, (int)boundsF.Height);

            //width = (int)boundsF.Width * sizeof(PixelData);
        }
    }

    public class DirectBitmap : IDisposable
    {
        public Bitmap Bitmap { get; private set; }
        public Int32[] Bits { get; private set; }
        public bool Disposed { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        protected GCHandle BitsHandle { get; private set; }

        public DirectBitmap(int width, int height)
        {
            Width = width;
            Height = height;
            Bits = new int[width * height];
            BitsHandle = GCHandle.Alloc(Bits, GCHandleType.Pinned);
            Bitmap = new Bitmap(width, height, width * 4, PixelFormat.Format32bppPArgb, BitsHandle.AddrOfPinnedObject());
        }
        public DirectBitmap(Bitmap bmp)
        {

        }

        public void SetPixel(int x, int y, Color colour)
        {
            int index = x + (y * Width);
            int col = colour.ToArgb();

            Bits[index] = col;
        }

        public Color GetPixel(int x, int y)
        {
            int index = x + (y * Width);
            int col = Bits[index];
            Color result = Color.FromArgb(col);

            return result;
        }

        public Bitmap ToBmp()
        {
            return Bitmap;
        }

        public void Dispose()
        {
            if (Disposed)
                return;
            Disposed = true;
            Bitmap.Dispose();
            BitsHandle.Free();
        }
    }
}