﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DiplomCS
{
    public partial class Form1 : Form
    {
        private const int rect_size = 1, // размер изображения, посылаемого на обработку нейронной сети, по умолчанию: 5, т.е. изображение 5х5
                    layers_count = 1,
                    h_count = 1000,
                    c_solution = 3;
        private const double l_rate = 0.065; // скорость обучения нейронной сети, по умолчанию: 0.01

        private int i_count,
                    files_count;
        private double[,] w1, w2, w3, wo;
        private string saved_file_name = "";
        private string[,] files_array;

        private NeuralNetwork Nn = new NeuralNetwork();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            i_count = rect_size * rect_size * 3;
            //h_count = 1000;

            if (File.Exists("OldW.txt"))
            {
                int oldw = int.Parse(File.ReadAllText("OldW.txt"));
                if (oldw != rect_size)
                    Recreate_weights();
            }
            else
                File.WriteAllText("OldW.txt", rect_size.ToString());

            Nn.Init(rect_size, h_count);
        }

        #region Preprocessing
        private void Recreate_weights()
        {
            w1 = new double[h_count, i_count];
            w2 = new double[h_count, h_count];
            w3 = new double[h_count, h_count];
            wo = new double[h_count, i_count];

            for (int j = 0; j < h_count; j++)
            {
                for (int i = 0; i < i_count; i++)
                {
                    Random rnd = new Random();
                    double num = rnd.Next(40, 59);
                    //num = num / 100;
                    num = 0.0;

                    w1[j, i] = num;
                    w2[j, i] = num;
                    w3[j, i] = num;
                    wo[j, i] = num;
                }
            }

            Serializer.Save("weights01.bin", w1);
            Serializer.Save("weights02.bin", w2);
            Serializer.Save("weights03.bin", w3);
            Serializer.Save("weights_o.bin", wo);
        }
        // исправить
        private void Get_files_list()
        {
            FolderBrowserDialog OpenF = new FolderBrowserDialog();

            if (OpenF.ShowDialog() == DialogResult.OK)
            {
                List<string> paths_list = new List<string>();
                List<string> names_list = new List<string>();

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
            //progressBar1.Location = new Point(ClientSize.Width - 129, comboBox1.Location.Y + 26);

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

            pictureBox5.Size = new Size(1, 1);
            pictureBox2.Location = new Point(pictureBox1.Size.Width + 32, 28);
            pictureBox3.Location = new Point(12, pictureBox1.Size.Height + 48);
            pictureBox4.Location = new Point(pictureBox1.Size.Width + 32, pictureBox1.Size.Height + 48);
            pictureBox5.Location = new Point(pictureBox1.Size.Width * 2 + 52, pictureBox4.Location.Y);
        }

        private void Initiate_learning()
        {
            //int k = 0;
            //for (int j = 0; j < 50; j++)
            for (int i = 0; i < files_count; i++)
            {
                string ndvi_suf = "ndvi" + files_array[2, i] + files_array[3, i] + ".png",
                       ndvi_path = files_array[0, i].Substring(0, files_array[0, i].Length - 4) + ndvi_suf;

                Bitmap pic_rgb = new Bitmap(files_array[0, i]);
                Bitmap pic_ndvi = new Bitmap(ndvi_path);

                pictureBox1.Image = pic_rgb;
                pictureBox2.Image = pic_ndvi;

                Rects_from_pic(pic_rgb, pic_ndvi);

                if (i == 0 || i == 1 || i == 2 || i == 5 || i == 10 || i == 20 || i == 50 || i == 100 || i == 200 || i == 500 || i == 1000 || i == 1500 || i == 2000 || i == 2500 || i == 3000 || i == 3500 || i == 4000 || i == 4400 || i == 5000 || i == 10000 || i == 20000 || i == 50000)
                {
                    pictureBox3.Image.Save("C:\\Users\\user\\Desktop\\Diplom\\results\\" + rect_size + "x" + rect_size + " pxl, " + layers_count + "x" + h_count + " layer, " + l_rate + " rate, " + i + " it, " + files_array[1, i] + "(delta).png", ImageFormat.Png);
                    pictureBox4.Image.Save("C:\\Users\\user\\Desktop\\Diplom\\results\\" + rect_size + "x" + rect_size + " pxl, " + layers_count + "x" + h_count + " layer, " + l_rate + " rate, " + i + " it, " + files_array[1, i] + ".png", ImageFormat.Png);
                    //pictureBox4.Image.Save("C:\\Users\\user\\Desktop\\results\\" + layers_count + " h_layer\\l_r " + l_rate + "\\" + rect_size + "x" + rect_size + " pxl\\" + h_count + "  h_neurons\\" + i + " it, " + files_array[1, i] + ".png", ImageFormat.Png);

                    //Serializer.Save("C:\\Users\\user\\Desktop\\results\\weights\\" + rect_size + "x" + rect_size + " pxl, " + layers_count + " layer, " + l_rate + " rate, " + i + " w1" + ".bin", w1);
                    //Serializer.Save("C:\\Users\\user\\Desktop\\results\\weights\\" + rect_size + "x" + rect_size + " pxl, " + layers_count + " layer, " + l_rate + " rate, " + i + " w2" + ".bin", w2);
                }

                File.WriteAllText("C:\\Users\\user\\Desktop\\Fael.txt", "Последний (" + i + ") открытый файл был: " + files_array[1, i] + ".png");

                if (files_array[1, i] == "14829") // 01127 //if (i == 4490)
                {
                    //pictureBox3.Image.Save("C:\\Users\\user\\Desktop\\results\\" + layers_count + " h_layer\\l_r " + l_rate + "\\" + rect_size + "x" + rect_size + " pxl\\" + h_count + "  h_neurons\\" + i + " it, " + files_array[1, i] + ".png", ImageFormat.Png);
                    //pictureBox4.Image.Save("C:\\Users\\user\\Desktop\\results\\" + layers_count + " h_layer\\l_r " + l_rate + "\\" + rect_size + "x" + rect_size + " pxl\\" + h_count + "  h_neurons\\" + i + " it, " + files_array[1, i] + ".png", ImageFormat.Png);
                    pictureBox3.Image.Save("C:\\Users\\user\\Desktop\\Diplom\\results\\" + rect_size + "x" + rect_size + " pxl, " + layers_count + "x" + h_count + " layer, " + l_rate + " rate, " + i + " it, " + files_array[1, i] + "(delta).png", ImageFormat.Png);
                    pictureBox4.Image.Save("C:\\Users\\user\\Desktop\\Diplom\\results\\" + rect_size + "x" + rect_size + " pxl, " + layers_count + "x" + h_count + " layer, " + l_rate + " rate, " + i + " it, " + files_array[1, i] + ".png", ImageFormat.Png);
                    Process.GetCurrentProcess().Kill();
                }
            }

            Pictures_change_elements();
        }
        #endregion Preprocessing

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

                for (int h = start_y, d = 0; h < rect_size + start_y; h++, d++)
                {
                    for (int w = start_x, l = 0; w < rect_size + start_x; w++, l++)
                    {
                        res_int[l, d, 0] = res[0, h, w];
                        res_int[l, d, 1] = res[1, h, w];
                        res_int[l, d, 2] = res[2, h, w];
                    }
                }
            }
            finally
            {
                bmp.UnlockBits(bd);
            }

            return res_int;
        }
        
        public unsafe Bitmap LBSetPixel(Bitmap bmp, int[,,] arr, int start_x, int start_y)
        {
            int width = bmp.Width, height = bmp.Height;

            BitmapData bd = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            try
            {
                int end_x = start_x + rect_size,
                    end_y = start_y + rect_size;

                for (int h = start_y, d = 0; h < end_y; h++, d++)
                {
                    byte* curpos = (byte*)bd.Scan0 + h * bd.Stride;
                    for (int w = start_x, l = 0; w < end_x; w++, l++, curpos += 3)
                    {
                        curpos[start_x * 3 + 2] = (byte)arr[l, d, 0]; // r
                        curpos[start_x * 3 + 1] = (byte)arr[l, d, 1]; // g
                        curpos[start_x * 3 + 0] = (byte)arr[l, d, 2]; // b
                    }
                }
            }
            finally
            {
                bmp.UnlockBits(bd);
            }

            return bmp;
        }

        private void Rects_from_pic(Bitmap pic_rgb, Bitmap pic_ndvi)
        {
            int wd = pic_rgb.Size.Width,
                hd = pic_rgb.Size.Height;

            int[] rgb_res = new int[i_count],
                  ndvi_res = new int[i_count];
            int[,,] s_argb = new int[rect_size, rect_size, 3],
                    s_ndvi = new int[rect_size, rect_size, 3];

            Bitmap taken_rgb = new Bitmap(wd, hd);
            Bitmap taken_ndvi = new Bitmap(wd, hd);
            Bitmap taken_prec = new Bitmap(wd, hd);

            w1 = Serializer.Load_w(i_count, h_count, "weights01.bin");
            if (layers_count >= 2)
                w2 = Serializer.Load_w(i_count, h_count, "weights02.bin");
            else if (layers_count == 3)
                w3 = Serializer.Load_w(i_count, h_count, "weights03.bin");
            wo = Serializer.Load_w(i_count, h_count, "weights_o.bin");

            for (int j = 0; j < hd - rect_size + 1; j++)
                for (int i = 0; i < wd - rect_size + 1; i++)
                {
                    if (pic_ndvi != null)
                    {
                        s_argb = LBGetPixel(pic_rgb, i, j);
                        s_ndvi = LBGetPixel(pic_ndvi, i, j);
                    }

                    if (layers_count == 1)
                        Nn.Get_weights(w1, wo);
                    else if (layers_count == 2)
                        Nn.Get_weights(w1, w2, wo);
                    else
                        Nn.Get_weights(w1, w2, w3, wo);

                    Nn.Take_rgb(Fill_1dim_array(s_argb));
                    if (pic_ndvi != null)
                        Nn.Take_ndvi(Fill_1dim_array(s_ndvi), l_rate);

                    ndvi_res = Nn.Analysis();

                    if (pic_ndvi != null)
                    {
                        w1 = Nn.Return_weights(1);
                        if (layers_count >= 2)
                            w2 = Nn.Return_weights(2);
                        else if (layers_count == 3)
                            w3 = Nn.Return_weights(3);
                        wo = Nn.Return_weights(0);
                    }

                    rgb_res = Fill_1dim_array(s_argb);
                    taken_ndvi = Paint_ndvi(taken_ndvi, ndvi_res, i, j);
                    taken_rgb = Cut_rgb_image(taken_rgb, rgb_res, ndvi_res, i, j);
                }

            Serializer.Save("weights01.bin", w1);
            if (layers_count >= 2)
                Serializer.Save("weights02.bin", w2);
            else if (layers_count == 3)
                Serializer.Save("weights03.bin", w3);
            Serializer.Save("weights_o.bin", wo);

            pictureBox3.Image = taken_rgb;
            pictureBox4.Image = taken_ndvi;

            if (pic_ndvi != null)
            {
                taken_prec = Calculate_precision(pic_ndvi, taken_ndvi);
                pictureBox3.Image = taken_prec;
            }
        }
        //SetPixel
        private Bitmap Paint_ndvi(Bitmap bmp, int[] arr, int start_x, int start_y)
        {
            Bitmap res = bmp;
            int end_x = bmp.Width - rect_size + 1,
                end_y = bmp.Height - rect_size + 1;
            int[,,] rgb_arr = Fill_3dim_array(arr);

            /*for (int j = start_y; j < start_y + rect_size; j++)
                if (j == end_y)
                    break;
                else
                    for (int i = start_x; i < start_x + rect_size || i < end_x; i++)
                        if (i == end_x)
                            break;
                        else*/
                            bmp = LBSetPixel(bmp, rgb_arr, start_x, start_y);

            return res;
        }
        //SetPixel
        private Bitmap Cut_rgb_image(Bitmap bmp, int[] rgb_array, int[] ndvi_array, int start_x, int start_y)
        {
            Bitmap res = bmp;
            //int end_x = bmp.Width - rect_size + 1,
                //end_y = bmp.Height - rect_size + 1; // 50 - 1 + 1 / т.е. диапазон: 0 - 49 // диапазон: 0 - 49
            int[,,] arr = Fill_3dim_array(rgb_array);
            int[,,] n_a = new int[rect_size, rect_size, 3];

            /*for (int j = start_y, k = 0; j < start_y + rect_size; j++)
                if (j == end_y)
                    break;
                else
                    for (int i = start_x; i < start_x + rect_size; i++)
                        if (i == end_x)
                            break;
                        else
                        {
                            if (ndvi_array[k] == 0 && ndvi_array[k + 1] == 0 && ndvi_array[k + 2] == 0)
                                bmp = LBSetPixel(bmp, n_a, i, j);
                            else if (ndvi_array[k] == ndvi_array[k + 2])
                                bmp = LBSetPixel(bmp, n_a, i, j);
                            else if (ndvi_array[k + 2] >= 50)
                                bmp = LBSetPixel(bmp, n_a, i, j);
                            else
                                bmp = LBSetPixel(bmp, arr, i, j);
                        }*/

            if (ndvi_array[0] == 0 && ndvi_array[1] == 0 && ndvi_array[2] == 0)
                bmp = LBSetPixel(bmp, n_a, start_x, start_y);
            else if (ndvi_array[0] == ndvi_array[2])
                bmp = LBSetPixel(bmp, n_a, start_x, start_y);
            else if (ndvi_array[2] >= 50)
                bmp = LBSetPixel(bmp, n_a, start_x, start_y);
            else
                bmp = LBSetPixel(bmp, arr, start_x, start_y);

            return res;
        }
        //SetPixel
        private Bitmap Calculate_precision(Bitmap ndvi, Bitmap answer)
        {
            int wd = answer.Width,
                hd = answer.Height;
            Bitmap res = new Bitmap(wd, hd);

            for (int j = 0; j < hd; j++)
                for(int i = 0; i < wd; i++)
                {
                    int[,,] n = LBGetPixel(ndvi, i, j);
                    int[,,] a = LBGetPixel(answer, i, j);
                    int[,,] r = new int[rect_size, rect_size, 3];

                    for(int l = 0; l < rect_size; l++) // пока что расчитано на квадраты размерами 1х1
                        for(int k = 0; k < rect_size; k++)
                        {
                            r[k, l, 0] = a[k, l, 0] - n[k, l, 0];
                            r[k, l, 1] = a[k, l, 1] - n[k, l, 1];
                            r[k, l, 2] = a[k, l, 2] - n[k, l, 2];

                            if (r[k, l, 0] < 0)
                                r[k, l, 0] *= -1;
                            if (r[k, l, 1] < 0)
                                r[k, l, 1] *= -1;
                            if (r[k, l, 2] < 0)
                                r[k, l, 2] *= -1;
                            
                            if (r[k, l, 0] != 0 && r[k, l, 1] != 0 && r[k, l, 2] != 0)
                                LBSetPixel(res, r, i, j);
                        }
                }
            
            return res;
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            Nn.Analysis();
        }

        //Многозадачные методы------------------//

        private int[] Fill_1dim_array(int[,,] arr)
        {
            int k = 0;
            int[] res = new int[i_count];

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

        //События--------------------------------------------------//
        // исправить
        private void Save_as_Click(object sender, EventArgs e)
        {
            SaveFileDialog SaveF = new SaveFileDialog();
            saved_file_name = Path.GetFileName(SaveF.FileName);
            //SaveF.FileName = "*";
            SaveF.DefaultExt = "bmp";
            SaveF.ValidateNames = true;

            SaveF.Filter = "Bitmap Image (.bmp)|*.bmp|JPEG Image (.jpg)|*.jpeg|Png Image (.png)|*.png";
            if (SaveF.ShowDialog() == DialogResult.OK)
                try
                {
                    pictureBox3.Image.Save(saved_file_name); //, SaveF.DefaultExt
                }
                catch
                {
                    MessageBox.Show("Невозможно сохранить картинку", "FATAL ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

            //SaveF.ShowDialog();
        }

        private void Open_directory_Click(object sender, EventArgs e)
        {
            Get_files_list();
            Initiate_learning();
            //backgroundWorker1_DoWork(Nn, (DoWorkEventArgs)e);
        }

        private void Open_file_Click(object sender, EventArgs e)
        {
            Get_files_list();
            Enable_dir_elements();
        }
        // исправить
        private void Open_rgb_Click(object sender, EventArgs e)
        {
            Get_files_list();
            Enable_dir_elements();
        }

        private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex > -1)
            {
                Bitmap pic_rgb = new Bitmap(files_array[0, comboBox1.SelectedIndex]);
                Bitmap pic_ndvi = new Bitmap(files_array[0, comboBox1.SelectedIndex].Substring(0, files_array[0, comboBox1.SelectedIndex].Length - 4) + "ndvi" + files_array[2, comboBox1.SelectedIndex] + files_array[3, comboBox1.SelectedIndex] + ".png");

                pictureBox1.Image = pic_rgb;
                pictureBox2.Image = pic_ndvi;
                
                Rects_from_pic(pic_rgb, pic_ndvi);

                //comboBox2.SelectedIndex = 0;
                Combobox_change_elements();
                Pictures_change_elements();
                //pictureBox4.Image.Save("C:\\Users\\user\\Desktop\\results\\" + files_array[1, comboBox1.SelectedIndex] + ".png", System.Drawing.Imaging.ImageFormat.Png);
            }
        }

        private void Form_ChangedSize(object sender, EventArgs e)
        {
            Combobox_change_elements();
            Pictures_change_elements();
        }
    }

    public class NeuralNetwork
    {
        private const bool use_sigmoid = true;

        private bool has_answer = false;

        private int input_count,
                    hidden_count,
                    h_layers_count;

        private int[] taken_ans,
                      output_data;

        private double learning_rate; /*precision*/

        private double[] input_layer,
                         first_hidden_layer,
                       m_first_hidden_layer,
                         second_hidden_layer,
                       m_second_hidden_layer,
                         third_hidden_layer,
                       m_third_hidden_layer,
                         output_layer,
                       m_output_layer,
                         error,
                         weights_1_delta,
                         weights_2_delta,
                         weights_3_delta,
                         weights_o_delta,
                         w_1,
                         w_2,
                         w_3,
                         w_o;

        private double[,] weights_1,
                          weights_2,
                          weights_3,
                          weights_o;

        public void Init(int rect_size, int hl1_size)
        {
            input_count = rect_size * rect_size * 3;
            hidden_count = hl1_size;
        }

        public void Get_weights(double[,] w1, double[,] wo) // weights_1 = w1; weights_o = wo;
        {
            first_hidden_layer = new double[hidden_count];
            m_first_hidden_layer = new double[hidden_count];
            w_1 = new double[input_count * hidden_count];
            weights_1 = new double[hidden_count, input_count];

            output_layer = new double[input_count];
            m_output_layer = new double[input_count];
            w_o = new double[input_count * hidden_count];
            weights_o = new double[hidden_count, input_count];

            w_1 = W_to_vector(w1, input_count);
            w_o = W_to_vector(wo, input_count);
            h_layers_count = 1;
        }
        public void Get_weights(double[,] w1, double[,] w2, double[,] wo)
        {
            Get_weights(w1, wo);

            second_hidden_layer = new double[hidden_count];
            m_second_hidden_layer = new double[hidden_count];
            w_2 = new double[hidden_count * hidden_count];
            weights_2 = new double[hidden_count, hidden_count];

            w_2 = W_to_vector(w2, hidden_count);
            h_layers_count = 2;
        }
        public void Get_weights(double[,] w1, double[,] w2, double[,] w3, double[,] wo)
        {
            Get_weights(w1, w2, wo);

            third_hidden_layer = new double[hidden_count];
            m_third_hidden_layer = new double[hidden_count];
            w_3 = new double[hidden_count * hidden_count];
            weights_3 = new double[hidden_count, hidden_count];

            w_3 = W_to_vector(w3, hidden_count);
            h_layers_count = 3;
        }

        public void Take_rgb(int[] pic)
        {
            input_layer = new double[input_count];
            output_data = new int[input_count];

            for (int i = 0; i < input_count; i++)
                input_layer[i] = (double)pic[i] / 255;
        }

        public void Take_ndvi(int[] ans, double lr)
        {
            learning_rate = lr;
            taken_ans = new int[input_count];

            for (int i = 0; i < input_count; i++)
                taken_ans[i] = ans[i];

            has_answer = true;
        }

        public int[] Analysis()
        {
            for (int i = 0; i <= h_layers_count; i++)
            {
                Fill_next_layer(i); // заполняется слой, выполняется ~10 мс
                S_activation_function(i); // мод. слой заполняется сигмоидом этого же слоя
                if (i == h_layers_count)
                    Output_convertation();
            }

            if (has_answer)
            {
                weights_1_delta = new double[hidden_count];
                if (h_layers_count >= 2)
                    weights_2_delta = new double[hidden_count];
                else if (h_layers_count == 3)
                    weights_3_delta = new double[hidden_count];
                weights_o_delta = new double[input_count];

                for(int i = 0; i <= h_layers_count; i++)
                {
                    Error_catching(i);
                    Get_weights_delta(i);
                    Change_weights(i);
                }

                weights_1 = W_to_matrix(w_1, input_count);
                if (h_layers_count >= 2)
                    weights_2 = W_to_matrix(w_2, hidden_count);
                else if (h_layers_count == 3)
                    weights_3 = W_to_matrix(w_3, hidden_count);
                weights_o = W_to_matrix(w_o, input_count);
            }

            return output_data;
        }
        
        public double Return_precision()
        {
            return 0; // precision
        }

        public double[,] Return_weights(int layer)
        {
            if(layer == 1)
                return weights_1;
            else if(layer == 2)
                return weights_2;
            else if(layer == 3)
                return weights_3;
            else
                return weights_o;
        }

        //Анализ данных---------------//

        private void Fill_next_layer(int it) // ~10 мс \\ ~5 мс //
        {
            for (int j = 0; j < hidden_count; j++)
                switch (it)
                {
                    case 0:
                        for (int i = 0; i < input_count; i++)
                            first_hidden_layer[j] = first_hidden_layer[j] + input_layer[i] * w_1[j * input_count + i];
                        break;
                    case 1:
                        if (h_layers_count == 1)
                            for (int i = 0; i < input_count; i++)
                                output_layer[i] = output_layer[i] + m_first_hidden_layer[j] * w_o[j * input_count + i];
                        else
                            for (int i = 0; i < hidden_count; i++)
                                second_hidden_layer[j] = second_hidden_layer[j] + m_first_hidden_layer[j] * w_2[j * hidden_count + i];
                        break;
                    case 2:
                        if (h_layers_count == 2)
                            for (int i = 0; i < input_count; i++)
                                output_layer[i] = output_layer[i] + m_second_hidden_layer[j] * w_o[j * input_count + i];
                        else
                            for (int i = 0; i < hidden_count; i++)
                                third_hidden_layer[j] = third_hidden_layer[j] + m_second_hidden_layer[j] * w_3[j * hidden_count + i];
                        break;
                    case 3:
                        for (int i = 0; i < input_count; i++)
                            output_layer[i] = output_layer[i] + m_third_hidden_layer[j] * w_o[j * input_count + i];
                        break;
                }
        }

        private void Output_convertation()
        {
            for (int i = 0; i < input_count; i++)
                output_data[i] = (int)(m_output_layer[i] * 255);
        }

        //Обучение-----------------//

        private void Error_catching(int it) // ~20 мс/~1 мс \\ ~5 мс/~1 мс //
        {
            switch (it)
            {
                case 0:
                    error = new double[input_count];

                    for (int i = 0; i < input_count; i++)
                        error[i] = m_output_layer[i] - (taken_ans[i] / 255.0);
                    break;
                case 1:
                    error = new double[hidden_count];

                    for (int j = 0; j < hidden_count; j++)
                        for (int i = 0; i < input_count; i++)
                            error[j] = w_o[j * input_count + i] * weights_o_delta[i];
                    break;
                case 2:
                    error = new double[hidden_count];

                    for (int j = 0; j < hidden_count; j++)
                        for (int i = 0; i < hidden_count; i++)
                            if (h_layers_count == 3)
                                error[j] = w_3[j * input_count + i] * weights_3_delta[i];
                            else
                                error[j] = w_2[j * input_count + i] * weights_2_delta[i];
                    break;
                case 3:
                    error = new double[hidden_count];

                    for (int j = 0; j < hidden_count; j++)
                        for (int i = 0; i < hidden_count; i++)
                            error[j] = w_2[j * input_count + i] * weights_2_delta[i];
                    break;
                //((double)taken_ans[i] / 255) - m_output_layer[i]
                //m_output_layer[i] - ((double)taken_ans[i] / 255)
                //m_output_layer[i] - Sigmoid((double)taken_ans[i] / 255)
                //error[i] = Sigmoid(m_output_data[i] - taken_ans[i]);
                //precision = precision + error[i];
            }
        }

        private void Get_weights_delta(int it)
        {
            switch (it)
            {
                case 0:
                    for (int i = 0; i < input_count; i++)
                        weights_o_delta[i] = error[i] * m_output_layer[i] * (1 - m_output_layer[i]);
                    break;
                case 1:
                    for (int i = 0; i < hidden_count; i++)
                        if (h_layers_count == 3)
                            weights_3_delta[i] = error[i] * m_third_hidden_layer[i] * (1 - m_third_hidden_layer[i]);
                        else if (h_layers_count == 2)
                            weights_2_delta[i] = error[i] * m_second_hidden_layer[i] * (1 - m_second_hidden_layer[i]);
                        else
                            weights_1_delta[i] = error[i] * m_first_hidden_layer[i] * (1 - m_first_hidden_layer[i]);
                    break;
                case 2:
                    for (int i = 0; i < hidden_count; i++)
                        if (h_layers_count == 3)
                            weights_2_delta[i] = error[i] * m_second_hidden_layer[i] * (1 - m_second_hidden_layer[i]);
                        else
                            weights_1_delta[i] = error[i] * m_first_hidden_layer[i] * (1 - m_first_hidden_layer[i]);
                    break;
                case 3:
                    for (int i = 0; i < hidden_count; i++)
                        weights_1_delta[i] = error[i] * m_first_hidden_layer[i] * (1 - m_first_hidden_layer[i]);
                    break;
            }
        }
        
        private void Change_weights(int it) // ~10 мс/~20 мс \\ ~5мс //
        {
            for (int j = 0; j < hidden_count; j++)
                switch (it)
                {
                    case 0:
                        for (int i = 0; i < input_count; i++)
                            if (h_layers_count == 3)
                                w_o[j * input_count + i] = w_o[j * input_count + i] - m_third_hidden_layer[j] * weights_o_delta[i] * learning_rate;
                            else if (h_layers_count == 2)
                                w_o[j * input_count + i] = w_o[j * input_count + i] - m_second_hidden_layer[j] * weights_o_delta[i] * learning_rate;
                            else
                                w_o[j * input_count + i] = w_o[j * input_count + i] - m_first_hidden_layer[j] * weights_o_delta[i] * learning_rate;
                        break;
                    case 1:
                        if (h_layers_count == 3)
                            for (int i = 0; i < hidden_count; i++)
                                w_3[j * hidden_count + i] = w_3[j * hidden_count + i] - m_second_hidden_layer[i] * weights_3_delta[j] * learning_rate;
                        else if (h_layers_count == 2)
                            for (int i = 0; i < hidden_count; i++)
                                w_2[j * hidden_count + i] = w_2[j * hidden_count + i] - m_first_hidden_layer[i] * weights_2_delta[j] * learning_rate;
                        else
                            for (int i = 0; i < input_count; i++)
                                w_1[j * input_count + i] = w_1[j * input_count + i] - input_layer[i] * weights_1_delta[j] * learning_rate;
                        break;
                    case 2:
                        if (h_layers_count == 3)
                            for (int i = 0; i < hidden_count; i++)
                                w_2[j * hidden_count + i] = w_2[j * hidden_count + i] - m_first_hidden_layer[i] * weights_2_delta[j] * learning_rate;
                        else
                            for (int i = 0; i < input_count; i++)
                                w_1[j * input_count + i] = w_1[j * input_count + i] - input_layer[i] * weights_1_delta[j] * learning_rate;
                        break;
                    case 3:
                        for (int i = 0; i < input_count; i++)
                            w_1[j * input_count + i] = w_1[j * input_count + i] - input_layer[i] * weights_1_delta[j] * learning_rate;
                        break;
                }
        }

        //Многозадачные функции-------------------------//

        private double[] W_to_vector(double[,] w, int second_size)
        {
            double[] res = new double[hidden_count * second_size];

            for (int j = 0, k = 0; j < hidden_count; j++)
                for (int i = 0; i < second_size; i++, k++)
                    res[k] = w[j, i];

            return res;
        }

        private double[,] W_to_matrix(double[] w, int second_size)
        {
            double[,] res = new double[hidden_count, second_size];

            for (int j = 0, k = 0; j < hidden_count; j++)
                for (int i = 0; i < second_size; i++, k++)
                    res[j, i] = w[k];

            return res;
        }

        private void S_activation_function(int it)
        {
            switch (it)
            {
                case 0:
                    for (int i = 0; i < hidden_count; i++)
                        m_first_hidden_layer[i] = Sigmoid(first_hidden_layer[i]);
                    break;
                case 1:
                    if (h_layers_count == 1)
                        for (int i = 0; i < input_count; i++)
                            m_output_layer[i] = Sigmoid(output_layer[i]);
                    else
                        for (int i = 0; i < hidden_count; i++)
                            m_second_hidden_layer[i] = Sigmoid(second_hidden_layer[i]);
                    break;
                case 2:
                    if (h_layers_count == 2)
                        for (int i = 0; i < input_count; i++)
                            m_output_layer[i] = Sigmoid(output_layer[i]);
                    else
                        for (int i = 0; i < hidden_count; i++)
                            m_third_hidden_layer[i] = Sigmoid(third_hidden_layer[i]);
                    break;                   
                case 3:
                    for (int i = 0; i < input_count; i++)
                        m_output_layer[i] = Sigmoid(output_layer[i]);
                    break;
            }
        }

        private double Sigmoid(double x)
        {
            return 1 / (1 + Math.Pow(Math.E, -x));
        }

        private double Tangent(double x)
        {
            return (Math.Pow(Math.E, 2 * x) - 1) / (Math.Pow(Math.E, 2 * x) + 1);
        }
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
}