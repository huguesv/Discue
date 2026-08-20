// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Discue.Avalonia.Views;

using global::Avalonia;
using global::Avalonia.Controls;
using global::Avalonia.Media;
using Microsoft.Extensions.DependencyInjection;
using Woohoo.Audio.Services;

public partial class VisualizationPage : ContentPage
{
    private readonly double[] plotPsd;
    private readonly double[] plotWave;
    private readonly double[] plotBands;
    private readonly ScottPlot.Bar[] plotBars;
    private readonly IVisualizationProviderService visualizationProviderService;

    public VisualizationPage()
    {
        this.InitializeComponent();

        this.visualizationProviderService = (App.Current as App)!.Host.Services.GetRequiredService<IVisualizationProviderService>();
        this.visualizationProviderService.DataAvailable += this.VisualizationProviderService_DataAvailable;

        this.FftPlot.Plot.DataBackground.Color = ScottPlot.Colors.Transparent;
        this.FftPlot.Plot.FigureBackground.Color = ScottPlot.Colors.Transparent;
        this.WavePlot.Plot.DataBackground.Color = ScottPlot.Colors.Transparent;
        this.WavePlot.Plot.FigureBackground.Color = ScottPlot.Colors.Transparent;
        this.BandPlot.Plot.DataBackground.Color = ScottPlot.Colors.Transparent;
        this.BandPlot.Plot.FigureBackground.Color = ScottPlot.Colors.Transparent;

        this.plotPsd = new double[257];
        this.plotWave = new double[441];
        this.plotBands = new double[8];

        // Media Player Equalizer bands:
        // 62 Hz, 125 Hz, 250 Hz, 500 Hz, 1 kHz, 2 kHz, 4 kHz, 8 kHz, 16 kHz
        this.FftPlot.Plot.Axes.SetLimits(0, 44100 / 2, -100, 0);
        this.FftPlot.Plot.Add.Signal(this.plotPsd, 44100.0 / this.plotPsd.Length);
        this.FftPlot.Plot.Layout.Frameless();
        this.FftPlot.Plot.HideGrid();
        this.FftPlot.Plot.PlotControl?.Menu?.Clear();
        this.FftPlot.Plot.PlotControl?.UserInputProcessor.Disable();
        this.FftPlot.Refresh();

        this.WavePlot.Plot.Add.Signal(this.plotWave, 44100.0 / 1000);
        this.WavePlot.Plot.Axes.SetLimitsY(-1.0, 1.0);
        this.WavePlot.Plot.Layout.Frameless();
        this.WavePlot.Plot.HideGrid();
        this.WavePlot.Plot.PlotControl?.Menu?.Clear();
        this.WavePlot.Plot.PlotControl?.UserInputProcessor.Disable();
        this.WavePlot.Refresh();

        // TODO: Check out this Histogram sample code, it can update itself
        // and bin the data automatically: https://scottplot.net/cookbook/5.0/Histograms/HistogramBars/
        this.plotBars =
        [
            new ScottPlot.Bar() { Value = 0, Position = 1 },
            new ScottPlot.Bar() { Value = 0, Position = 2 },
            new ScottPlot.Bar() { Value = 0, Position = 3 },
            new ScottPlot.Bar() { Value = 0, Position = 4 },
            new ScottPlot.Bar() { Value = 0, Position = 5 },
            new ScottPlot.Bar() { Value = 0, Position = 6 },
            new ScottPlot.Bar() { Value = 0, Position = 7 },
            new ScottPlot.Bar() { Value = 0, Position = 8 },
        ];

        this.BandPlot.Plot.Add.Bars(this.plotBars);
        this.BandPlot.Plot.Axes.SetLimitsY(0, 100);
        this.BandPlot.Plot.Layout.Frameless();
        this.BandPlot.Plot.HideGrid();
        this.BandPlot.Plot.PlotControl?.Menu?.Clear();
        this.BandPlot.Plot.PlotControl?.UserInputProcessor.Disable();
        this.BandPlot.Refresh();
    }

    private void VisualizationProviderService_DataAvailable(object? sender, VisualizationEventArgs e)
    {
        e.Visualization.CopyTo(this.plotPsd, this.plotBands, this.plotWave);

        for (int i = 0; i < this.plotBars.Length; i++)
        {
            this.plotBars[i].Value = 100.0 + this.plotBands[i];
        }

        this.FftPlot.Refresh();
        this.WavePlot.Refresh();
        this.BandPlot.Refresh();
    }
}
