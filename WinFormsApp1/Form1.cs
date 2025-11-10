using System.Runtime.InteropServices;
using System.Numerics;
using NAudio.Wave;

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        [DllImport("kernel32.dll")]
        public static extern void Beep(int freq, int time);
        public Form1()
        {
            InitializeComponent();
        }
        double[] ConvertToDouble(float[] raw)
        {
            double[] result = new double[raw.Length];
            for (int i = 0; i < raw.Length; i++)
            {
                result[i] = raw[i];
            }
            return result;
        }

        

        //颜色映射函数，可以自己改其他颜色
        Color MapScaleColor(double scale)
        {
            int r = (int)(scale * 255);
            int g = 255;
            int b = 255 - r;
            return Color.FromArgb(255, r, g, b);
        }
        void DrawImg(int sampleRate, float[] freqMap, Graphics graph, double scale)
        {
            int bands = 32;
            float[] bandValues = MusicFFTView.SplitFreqMap(sampleRate, freqMap, bands);

            graph.Clear(Color.Black);

            float maxValue = bandValues.Length > 0 ? bandValues.Max() : 1;
            if (maxValue <= 0) maxValue = 1;

            // 绘制频谱柱子
            int barWidth = 10;
            int spacing = 5;
            int startX = 25;

            using (var brush = new SolidBrush(Color.Lime))
            {
                for (int i = 0; i < bands; i++)
                {
                    int x = startX + i * (barWidth + spacing);
                    int height = (int)(bandValues[i] / maxValue * 250);
                    height = (int)(height * scale);
                    int y = 280 - height;

                    // 绘制频谱柱
                    brush.Color = MapScaleColor(scale); //将瞬时声压映射到颜色
                    graph.FillRectangle(brush, x, y, barWidth, height);
                    graph.DrawRectangle(Pens.White, x, y, barWidth, height);

                    // 绘制频段标签
                    graph.DrawString($"{i + 1}", new Font("Arial", 8), Brushes.White, x, 285);
                }
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            var fd = new OpenFileDialog();
            if (fd.ShowDialog() == DialogResult.OK)
            {
                var (sampleRate, raw) = AudioHelper.ReadAudioFile(fd.FileName);

                //var rawd = ConvertToDouble(raw);

                Bitmap b = new Bitmap(500, 300);
                var graph = Graphics.FromImage(b);

                pictureBox1.Image = b;


                System.Windows.Controls.MediaElement ene = new System.Windows.Controls.MediaElement();
                ene.LoadedBehavior = System.Windows.Controls.MediaState.Manual;
                ene.UnloadedBehavior = System.Windows.Controls.MediaState.Manual;
                ene.Source = new Uri(fd.FileName);
                ene.Play();
                Task.Run(() =>
                {
                    double prev = 0;
                    while (true)
                    {
                        double time = 0;
                        this.Invoke(() => time = ene.Position.TotalSeconds);
                        var sampleBlock = raw.AsSpan((int)(time * sampleRate), sampleRate / 20);
                        var fftResult = FFTF.Transform(sampleBlock);
                        var freqMap = FFTF.GetMagnitude(fftResult);
                        double average = 0;
                        for (int i = 0; i < sampleBlock.Length; i++)
                        {
                            //average = Math.Max(average, sampleBlock[i]);
                            average += Math.Abs(sampleBlock[i]);
                        }
                        average /= sampleBlock.Length;

                        if (average - prev > 0.05)
                        {
                            Task.Run(() =>
                            {
                                this.Invoke(() => this.Text = "鼓点");
                                Thread.Sleep(200);
                                this.Invoke(() => this.Text = "Music FFT");
                            });
                        }


                        prev = average;

                        //计算一下平均声压缩放一下FFT图，这样看起来舒服一点
                        //缩放了一下，这样颜色变化更明显
                        DrawImg(sampleRate, freqMap, graph, Math.Min(1.0, average * 2));
                        this.Invoke(() =>
                        {
                            pictureBox1.Image = b;
                        });
                        Thread.Sleep(10);
                    }
                });

            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var fd = new OpenFileDialog();
            if (fd.ShowDialog() == DialogResult.OK)
            {
                var (sampleRate, channels, raw) = AudioHelper.ReadAudioFileRaw(fd.FileName);
                if (channels == 2)
                {
                    float[] data = new float[raw.Length / channels];
                    for (int i = 0; i < data.Length; i++)
                    {
                        //data[i] = raw[i * 2] - raw[i * 2 + 1];

                        data[i] = raw[i * 2 + 1] - raw[i * 2];
                    }


                    AudioHelper.PlayPcmAsync(data, 1, sampleRate);
                }
                else
                {
                    MessageBox.Show("不支持" + channels + "声道");
                }
            }
        }
        
    }
}
