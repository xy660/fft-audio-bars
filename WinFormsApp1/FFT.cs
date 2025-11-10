using System;
using System.Numerics;

public static class FFT
{
    /// <summary>
    /// 执行快速傅里叶变换(FFT)
    /// </summary>
    /// <param name="input">输入时域信号</param>
    /// <returns>频域分量（复数数组，包含幅度和相位信息）</returns>
    public static Complex[] Transform(ReadOnlySpan<double> input)
    {
        int n = input.Length;

        // 确保输入长度是2的幂次方，如果不是则进行零填充
        if (!IsPowerOfTwo(n))
        {
            n = NextPowerOfTwo(n);
            Span<double> paddedInput = new double[n];
            input.CopyTo(paddedInput);
            // 剩余部分自动为0
            return FFTAlgorithm(paddedInput, false);
        }

        return FFTAlgorithm(input, false);
    }

    /// <summary>
    /// 执行逆快速傅里叶变换(IFFT)
    /// </summary>
    /// <param name="spectrum">频域信号</param>
    /// <returns>时域信号</returns>
    public static double[] InverseTransform(ReadOnlySpan<Complex> spectrum)
    {
        Complex[] timeDomain = FFTAlgorithm(spectrum, true);
        double[] result = new double[timeDomain.Length];

        for (int i = 0; i < timeDomain.Length; i++)
        {
            result[i] = timeDomain[i].Real / timeDomain.Length;
        }

        return result;
    }

    /// <summary>
    /// 获取频域分量的幅度谱
    /// </summary>
    /// <param name="fftResult">FFT结果（复数数组）</param>
    /// <returns>幅度谱（double数组）</returns>
    public static double[] GetMagnitude(ReadOnlySpan<Complex> fftResult)
    {
        double[] magnitude = new double[fftResult.Length];
        for (int i = 0; i < fftResult.Length; i++)
        {
            magnitude[i] = fftResult[i].Magnitude;
        }
        return magnitude;
    }

    /// <summary>
    /// 获取频域分量的相位谱
    /// </summary>
    /// <param name="fftResult">FFT结果（复数数组）</param>
    /// <returns>相位谱（double数组，单位：弧度）</returns>
    public static double[] GetPhase(ReadOnlySpan<Complex> fftResult)
    {
        double[] phase = new double[fftResult.Length];
        for (int i = 0; i < fftResult.Length; i++)
        {
            phase[i] = fftResult[i].Phase;
        }
        return phase;
    }

    /// <summary>
    /// 获取单边幅度谱（只包含正频率部分，适用于实数信号）
    /// </summary>
    /// <param name="fftResult">FFT结果</param>
    /// <returns>单边幅度谱</returns>
    public static double[] GetSingleSidedMagnitude(ReadOnlySpan<Complex> fftResult)
    {
        int n = fftResult.Length;
        int singleSidedLength = n / 2 + 1;
        double[] singleSided = new double[singleSidedLength];

        for (int i = 0; i < singleSidedLength; i++)
        {
            //直流分量
            if (i == 0)
            {
                singleSided[i] = fftResult[i].Magnitude / n;
            }
            //奈奎斯特频率分量
            else if (i == singleSidedLength - 1 && n % 2 == 0)
            {
                singleSided[i] = fftResult[i].Magnitude / n;
            }
            // 其他频率分量（需要乘以2，因为对称）
            else
            {
                singleSided[i] = 2.0 * fftResult[i].Magnitude / n;
            }
        }

        return singleSided;
    }

    /// <summary>
    /// 获取频率轴（Hz）
    /// </summary>
    /// <param name="fftSize">FFT点数</param>
    /// <param name="samplingRate">采样率（Hz）</param>
    /// <returns>频率轴数组</returns>
    public static double[] GetFrequencyAxis(int fftSize, double samplingRate)
    {
        int singleSidedLength = fftSize / 2 + 1;
        double[] frequencies = new double[singleSidedLength];
        double frequencyResolution = samplingRate / fftSize;

        for (int i = 0; i < singleSidedLength; i++)
        {
            frequencies[i] = i * frequencyResolution;
        }

        return frequencies;
    }

    private static Complex[] FFTAlgorithm(ReadOnlySpan<double> input, bool inverse)
    {
        int n = input.Length;

        Span<Complex> complexInput = n <= 256 ? stackalloc Complex[n] : new Complex[n];
        for (int i = 0; i < n; i++)
        {
            complexInput[i] = new Complex(input[i], 0);
        }

        return FFTAlgorithm(complexInput, inverse);
    }

    private static Complex[] FFTAlgorithm(ReadOnlySpan<Complex> input, bool inverse)
    {
        int n = input.Length;

        if (n == 1)
        {
            return new Complex[] { input[0] };
        }

        if (!IsPowerOfTwo(n))
        {
            throw new ArgumentException("输入长度必须是2的幂次方");
        }

        // 分治：奇偶分离
        Span<Complex> even = n <= 512 ? stackalloc Complex[n / 2] : new Complex[n / 2];
        Span<Complex> odd = n <= 512 ? stackalloc Complex[n / 2] : new Complex[n / 2];

        for (int i = 0; i < n / 2; i++)
        {
            even[i] = input[2 * i];
            odd[i] = input[2 * i + 1];
        }

        //递归
        Complex[] evenFFT = FFTAlgorithm(even, inverse);
        Complex[] oddFFT = FFTAlgorithm(odd, inverse);

        //合并
        Complex[] result = new Complex[n];

        double sign = inverse ? 1.0 : -1.0;
        for (int k = 0; k < n / 2; k++)
        {
            double angle = sign * 2.0 * Math.PI * k / n;
            Complex twiddle = new Complex(Math.Cos(angle), Math.Sin(angle));

            result[k] = evenFFT[k] + twiddle * oddFFT[k];
            result[k + n / 2] = evenFFT[k] - twiddle * oddFFT[k];
        }

        return result;
    }

    private static bool IsPowerOfTwo(int n)
    {
        return (n > 0) && ((n & (n - 1)) == 0);
    }

    private static int NextPowerOfTwo(int n)
    {
        int power = 1;
        while (power < n)
        {
            power <<= 1;
        }
        return power;
    }
}