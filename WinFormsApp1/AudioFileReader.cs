using NAudio.Wave;
using System;

public static class AudioHelper
{
    /// <summary>
    /// 读取音频文件并返回采样率和归一化的PCM数据
    /// </summary>
    /// <param name="filePath">音频文件路径（支持MP3, WAV, AIFF等）</param>
    /// <returns>包含采样率和归一化采样数据的元组</returns>
    public static (int sampleRate, float[] normalizedSamples) ReadAudioFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            throw new ArgumentException("文件路径不能为空");
        
        if (!System.IO.File.Exists(filePath))
            throw new System.IO.FileNotFoundException($"音频文件不存在: {filePath}");

        using (var reader = new AudioFileReader(filePath))
        {
            int sampleRate = reader.WaveFormat.SampleRate;
            int channels = reader.WaveFormat.Channels;
            

            int totalSamples = (int)(reader.Length / (reader.WaveFormat.BitsPerSample / 8));
            float[] samples = new float[totalSamples];
            
            int samplesRead = reader.Read(samples, 0, totalSamples);
            
            if (samplesRead < totalSamples)
            {
                Array.Resize(ref samples, samplesRead);
            }

            //如果是多声道，平均以下变成单声道
            if (channels > 1)
            {
                samples = ConvertToMono(samples, channels);
            }

            //归一化
            NormalizeSamples(samples);
            
            return (sampleRate, samples);
        }
    }

    /// <summary>
    /// 读取音频文件并返回采样率和归一化的PCM数据
    /// </summary>
    /// <param name="filePath">音频文件路径</param>
    /// <returns>包含采样率和归一化采样数据的元组</returns>
    public static (int sampleRate,int channels, float[] samples) ReadAudioFileRaw(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            throw new ArgumentException("文件路径不能为空");

        if (!System.IO.File.Exists(filePath))
            throw new System.IO.FileNotFoundException($"音频文件不存在: {filePath}");

        // 使用AudioFileReader（自动检测格式）
        using (var reader = new AudioFileReader(filePath))
        {
            int sampleRate = reader.WaveFormat.SampleRate;
            int channels = reader.WaveFormat.Channels;

            Console.WriteLine($"音频信息: {sampleRate}Hz, {channels}声道, {reader.TotalTime}时长");

            // 计算总采样数
            int totalSamples = (int)(reader.Length / (reader.WaveFormat.BitsPerSample / 8));
            float[] samples = new float[totalSamples];

            int samplesRead = reader.Read(samples, 0, totalSamples);

            if (samplesRead < totalSamples)
            {
                Array.Resize(ref samples, samplesRead);
            }

            NormalizeSamples(samples);

            return (sampleRate, channels,samples);
        }
    }

    private static float[] ConvertToMono(float[] interleavedSamples, int channels)
    {
        int monoLength = interleavedSamples.Length / channels;
        float[] mono = new float[monoLength];
        
        for (int i = 0; i < monoLength; i++)
        {
            float sum = 0;
            for (int channel = 0; channel < channels; channel++)
            {
                sum += interleavedSamples[i * channels + channel];
            }
            mono[i] = sum / channels;
        }
        
        Console.WriteLine($"多声道转换: {channels}声道 -> 单声道, 采样数: {interleavedSamples.Length} -> {monoLength}");
        return mono;
    }

    private static void NormalizeSamples(float[] samples)
    {
        if (samples == null || samples.Length == 0)
            return;

        // 找到绝对值的最大值
        float maxAbs = 0;
        foreach (float sample in samples)
        {
            float abs = Math.Abs(sample);
            if (abs > maxAbs)
                maxAbs = abs;
        }

        // 只有在超过1时才进行归一化
        if (maxAbs > 1.0f)
        {
            float scale = 1.0f / maxAbs;
            for (int i = 0; i < samples.Length; i++)
            {
                samples[i] *= scale;
            }
            Console.WriteLine($"已归一化，缩放因子: {scale}");
        }
        else
        {
            Console.WriteLine("数据已在[-1,1]范围内，无需归一化");
        }
    }



    /// <summary>
    /// 播放PCM数据（支持多声道）并返回可取消的Task
    /// </summary>
    /// <param name="pcmData">PCM数据（交错格式：左、右、左、右...）</param>
    /// <param name="channelCount">声道数量</param>
    /// <param name="sampleRate">采样率</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>可等待和取消的Task</returns>
    public static Task PlayPcmAsync(float[] pcmData, int channelCount, int sampleRate, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            // 创建WaveFormat（32位浮点数）
            var waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channelCount);

            // 创建播放设备
            using (var waveOut = new WaveOutEvent())
            {
                var bufferedWaveProvider = new BufferedWaveProvider(waveFormat);

                bufferedWaveProvider.BufferDuration = TimeSpan.FromSeconds(5); // 设置缓冲区大小

                waveOut.Init(bufferedWaveProvider);
                waveOut.Play();

                // 将float[]转换为byte[]
                byte[] audioData = new byte[pcmData.Length * 4]; // 32位浮点数，每个采样4字节
                Buffer.BlockCopy(pcmData, 0, audioData, 0, audioData.Length);

                int bytesPerSample = 4 * channelCount; // 每个采样时刻的字节数
                int bytesWritten = 0;

                // 分段写入数据，支持实时取消
                while (bytesWritten < audioData.Length && !cancellationToken.IsCancellationRequested)
                {
                    // 计算本次写入的数据量（不超过缓冲区剩余空间）
                    int bytesToWrite = Math.Min(4096, audioData.Length - bytesWritten);

                    // 如果缓冲区太满，等待一下
                    while (bufferedWaveProvider.BufferedBytes > bufferedWaveProvider.BufferLength / 2)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            return;

                        Thread.Sleep(10);
                    }

                    // 写入数据到播放缓冲区
                    bufferedWaveProvider.AddSamples(audioData, bytesWritten, bytesToWrite);
                    bytesWritten += bytesToWrite;
                }

                // 等待播放完成或取消
                while (waveOut.PlaybackState == PlaybackState.Playing &&
                       bufferedWaveProvider.BufferedBytes > 0 &&
                       !cancellationToken.IsCancellationRequested)
                {
                    Thread.Sleep(100);
                }
            }
        }, cancellationToken);
    }
}