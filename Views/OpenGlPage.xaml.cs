namespace MultipathSignal.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Avalonia;
using MultipathSignal.Core;

public partial class OpenGlPage : Avalonia.Controls.Window
{
    public readonly IList<Vector3> points = Array.Empty<Vector3>();
    public readonly IList<ushort> indices = Array.Empty<ushort>();

    public OpenGlPage() { InitializeComponent(); }

    public OpenGlPage(IList<Vector3> points, IList<ushort> indices) {
        this.points = points;
        this.indices = indices;
        InitializeComponent();
    }

    public OpenGlPage(IList<Vector3> points, IList<ushort> indices, string flavorText) 
        : this(points, indices) { this.FlavorTextBlock.Text = flavorText; }

    public static OpenGlPage FromValues(double[,] values, Rect bounds) 
    {
        int width = values.GetLength(0);
        int height = values.GetLength(1);
        var points = new Vector3[width * height];
        var indices = new ushort[(width - 1) * (height - 1) * 6];
        Parallel.For(0, width, i => {
            Parallel.For(0, height, j => {
                float x = (float)(bounds.Left + i / bounds.Width);
                float y = (float)(bounds.Top + j / bounds.Height);
                points[j * width + i] = new Vector3(x, y, (float)values[i, j]);
            });
        });
        Parallel.For(0, width - 1, i => {
            Parallel.For(0, height - 1, j => {
                ushort k = (ushort)(j * width + i); // Anchor point (top-left) index
                ushort k2 = (ushort)(k + width);    // Bottom-left point index
                int p = 6 * (j * (width - 1) + i);  // Triangle point index
                // Top triangle:
                indices[p++] = k++;
                indices[p++] = k;
                indices[p++] = k2;
                // Bottom triangle:
                indices[p++] = k;
                indices[p++] = k2++;
                indices[p]   = k2;

                // // Top triangle:
                // indices[p  ] = (ushort)(k);
                // indices[p+1] = (ushort)(k + 1);
                // indices[p+2] = (ushort)(k + width);
                // // Bottom triangle:
                // indices[p+3] = (ushort)(k + 1);
                // indices[p+4] = (ushort)(k + width);
                // indices[p+5] = (ushort)(k + width + 1);
            });
        });
        return new OpenGlPage(points, indices);
    }

    public static OpenGlPage FromValues(double[,] values) => 
        FromValues(values, new(-1, -1, 2, 2));

    public static OpenGlPage FromValues(
        double[,] values, 
        IList<double> samplesX, 
        IList<double> samplesY,
        double centerX,
        double centerY,
        Func<double, float>? normFunc = null,
        string flavorText = ""
    ) {
        int width = values.GetLength(0);
        if (width != samplesX.Count) throw new ArgumentException();
        int height = values.GetLength(1);
        if (height != samplesY.Count) throw new ArgumentException();
        
        double extentX = Math.Max(
            Math.Abs(samplesX.Max() - centerX), 
            Math.Abs(centerX - samplesX.Min()));
        double extentY = Math.Max(
            Math.Abs(samplesY.Max() - centerY), 
            Math.Abs(centerY - samplesY.Min())
        );

        var points = new Vector3[width * height];
        var indices = new ushort[(width - 1) * (height - 1) * 6];
        if (normFunc is null) 
            normFunc = v => (float)v;
        Parallel.For(0, width, i => 
            Parallel.For(0, height, j => 
                points[j * width + i] = new Vector3(
                    (float)((samplesX[i] - centerX) / extentX), 
                    (float)((samplesY[j] - centerY) / extentY), 
                    (float)normFunc(values[i, j])
                )
            )
        );
        Parallel.For(0, width - 1, i => {
            Parallel.For(0, height - 1, j => {
                ushort k = (ushort)(j * width + i); // Anchor point (top-left) index
                ushort k2 = (ushort)(k + width);    // Bottom-left point index
                int p = 6 * (j * (width - 1) + i);  // Triangle point index
                // Top triangle:
                indices[p++] = k++;
                indices[p++] = k;
                indices[p++] = k2;
                // Bottom triangle:
                indices[p++] = k;
                indices[p++] = k2++;
                indices[p]   = k2;

                // // Top triangle:
                // indices[p  ] = (ushort)(k);
                // indices[p+1] = (ushort)(k + 1);
                // indices[p+2] = (ushort)(k + width);
                // // Bottom triangle:
                // indices[p+3] = (ushort)(k + 1);
                // indices[p+4] = (ushort)(k + width);
                // indices[p+5] = (ushort)(k + width + 1);
            });
        });
        return new OpenGlPage(points, indices, flavorText);
    }

    public static void CreateAmbiguityFuncPage(
        SignalModulator gen, 
        int BitSeqLength, 
        double receiveDelay, 
        double dopplerShift, 
        IList<double> samplesTime,
        IList<double> samplesDoppler,
        double snrClean = 10.0, 
        double snrDirty = -10.0
    ) {
        double mainFrequency = gen.MainFrequency;
        int bitDelay = (int) Math.Ceiling(receiveDelay * gen.BitRate);
        var baseMod = Utils.RandomBitSeq(3 * BitSeqLength + 1);

        gen.MainFrequency = mainFrequency + dopplerShift;
        var dirtySignal = gen.Modulate(baseMod);
        int initDelay = (int)(SignalGenerator.Samplerate * (bitDelay * gen.BitLength - receiveDelay));
        int dirtyLength = (int)(SignalGenerator.Samplerate * gen.BitLength * BitSeqLength * 3);
        dirtySignal = Utils.ApplyNoise(
            dirtySignal.Skip(initDelay).Take(dirtyLength).ToArray(),
            Math.Pow(10.0, 0.1 * snrDirty)
        );

        var ambiguityValues = new double[samplesTime.Count, samplesDoppler.Count];
        int imax = -1, jmax = -1;
        double maxValue = 0;
        for (int j = 0; j < samplesDoppler.Count; j++) {
            gen.MainFrequency = mainFrequency + samplesDoppler[j];
            var cleanSignal = gen.Modulate(
                baseMod.Skip(bitDelay).Take(BitSeqLength)
            );
            cleanSignal = Utils.ApplyNoise(
                cleanSignal, 
                Math.Pow(10.0, 0.1 * snrClean)
            );
            var correl = CorrelationFft.Calculate(dirtySignal, cleanSignal);
            for (int i = 0; i < samplesTime.Count; i++) {
                int t = (int)(samplesTime[i] * SignalGenerator.Samplerate);
                double v = ambiguityValues[i, j] = correl[t].Magnitude;
                if (v > maxValue) {
                    imax = i;
                    jmax = j;
                    maxValue = v;
                }
            }
        }

        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => {
            Func<double, float> normFunc = 
                v => (float)Math.Pow(v / maxValue, 1.5);

            string flavorText = $"Receive delay: {receiveDelay:F4} s; Doppler shift: {dopplerShift:F1} Hz"; 
            flavorText += $"\nPredicted delay: {samplesTime[imax]:F4} s; Doppler shift: {samplesDoppler[jmax]:F1} Hz";

            imax = samplesTime.Count / 2;
            jmax = samplesDoppler.Count / 2;
            var resultWindow = OpenGlPage.FromValues(
                ambiguityValues,
                samplesTime,
                samplesDoppler,
                samplesTime[imax],
                samplesDoppler[jmax],
                normFunc,
                flavorText
            );
            resultWindow.Show();
        }); 
    }
    
    public static void CreateTeapot()
    {
        float[] coords;
        ushort[] indices;

        string name = typeof(OpenGlPage).Assembly
            .GetManifestResourceNames()
            .First(x => x.Contains("teapot.bin"));
        using (BinaryReader sr = new(
            typeof(OpenGlPage).Assembly
                .GetManifestResourceStream(name) 
                ?? throw new NullReferenceException()
        )) {
            var buf = new byte[sr.ReadInt32()];
            sr.Read(buf, 0, buf.Length);
            coords = new float[buf.Length / 4];
            Buffer.BlockCopy(buf, 0, coords, 0, buf.Length);

            buf = new byte[sr.ReadInt32()];
            sr.Read(buf, 0, buf.Length);
            indices = new ushort[buf.Length / 2];
            Buffer.BlockCopy(buf, 0, indices, 0, buf.Length);
        }

        var points = new Vector3[coords.Length / 3];
        for (var primitive = 0; primitive < coords.Length / 3; primitive++) {
            var srci = primitive * 3;
            points[primitive] = new Vector3(
                coords[srci], 
                coords[srci + 1], 
                coords[srci + 2]
            );
        }

        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => {
            var resultWindow = new OpenGlPage(points, indices);
            resultWindow.Show();
        });
    }
}
