# 音频频谱可视化

---

使用C#编写的音乐可视化，基于FFT算法实现

## 效果图



## 移植

如果需要移植到其他地方，比如Unity，WPF等平台，可以迁移 `FFT.cs` `FFTF.cs` `MusicFFTView.cs` `AudioFileReader.cs（可选，依赖NAudio库）`

使用方法：

```csharp

var (sampleRate, raw) = AudioHelper.ReadAudioFile(fd.FileName);

//根据实际播放进度进行PCM数据切片，time表示当前播放的位置（秒）
var sampleBlock = raw.AsSpan((int)(time * sampleRate), sampleRate / 20);
var fftResult = FFTF.Transform(sampleBlock);
var freqMap = FFTF.GetMagnitude(fftResult);

int bands = 32; //你需要显示的柱子数量
//频域分量幅度切成指定数量当作柱子高度
float[] bandValues = MusicFFTView.SplitFreqMap(sampleRate, freqMap, bands);

//使用bandValues渲染你自己的频谱

```

---
