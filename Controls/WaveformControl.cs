// Copyright (C) Neurosoft

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;

namespace SpeechMarkupEditor.Controls;

public class WaveformControl : RangeBase
{
    private List<Point> _points = new List<Point>();
    private bool _isDragging = false;
    private double _rightClickPosition;

    public static readonly StyledProperty<IBrush> WaveformBrushProperty =
            AvaloniaProperty.Register<WaveformControl, IBrush>(nameof(WaveformBrush), Brushes.DodgerBlue);

    public static readonly StyledProperty<IBrush> PlayedWaveformBrushProperty =
        AvaloniaProperty.Register<WaveformControl, IBrush>(nameof(PlayedWaveformBrush), Brushes.RoyalBlue);

    public static readonly StyledProperty<IBrush> CursorBrushProperty =
        AvaloniaProperty.Register<WaveformControl, IBrush>(nameof(CursorBrush), Brushes.Black);

    public static readonly StyledProperty<double> CursorWidthProperty =
        AvaloniaProperty.Register<WaveformControl, double>(nameof(CursorWidth), 2.0);

    public static readonly DirectProperty<WaveformControl, double> RightClickPositionProperty =
        AvaloniaProperty.RegisterDirect<WaveformControl, double>(
            nameof(RightClickPosition),
            o => o.RightClickPosition);

    public WaveformControl()
    {
        AffectsRender<WaveformControl>(ValueProperty, MinimumProperty, MaximumProperty);
    }

    /// <summary>
    /// Позиция правого клика мыши на waveform
    /// </summary>
    public double RightClickPosition
    {
        get => _rightClickPosition;
        private set => SetAndRaise(RightClickPositionProperty, ref _rightClickPosition, value);
    }

    /// <summary>
    /// Кисть для отрисовки непроигранной части волны
    /// </summary>
    public IBrush WaveformBrush
    {
        get => GetValue(WaveformBrushProperty);
        set => SetValue(WaveformBrushProperty, value);
    }

    /// <summary>
    /// Кисть для отрисовки уже проигранной части волны
    /// </summary>
    public IBrush PlayedWaveformBrush
    {
        get => GetValue(PlayedWaveformBrushProperty);
        set => SetValue(PlayedWaveformBrushProperty, value);
    }

    /// <summary>
    /// Кисть курсора текущей позиции
    /// </summary>
    public IBrush CursorBrush
    {
        get => GetValue(CursorBrushProperty);
        set => SetValue(CursorBrushProperty, value);
    }

    /// <summary>
    /// Толщина курсора текущей позиции
    /// </summary>
    public double CursorWidth
    {
        get => GetValue(CursorWidthProperty);
        set => SetValue(CursorWidthProperty, value);
    }

    /// <summary>
    /// Инициализация точками для отрисовки
    /// </summary>
    /// <param name="points">Точки полученные из сервиса для отрисовки waveform</param>
    public async Task InitializeAsync(List<Point> points)
    {
        try
        {
            _points.Clear();
            _points.AddRange(points);
        }
        finally
        {
            InvalidateVisual();
        }
    }

    /// <summary>
    /// Обработчик начала перетаскивания позиции
    /// </summary>
    /// <param name="e">Данные события</param>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var currentPosition = e.GetCurrentPoint(this);
        double position = currentPosition.Position.X;
        double calcValue = position / Bounds.Width * Maximum;
        if (currentPosition.Properties.IsRightButtonPressed)
        {
            RightClickPosition = calcValue;
            e.Handled = true;
        }
        else
        {
            Value = calcValue;
            _isDragging = true;
            e.Pointer.Capture(this);
        }

    }

    /// <summary>
    /// Обработчик обновления позиции при перетаскивании
    /// </summary>
    /// <param name="e">Данные события</param>
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!_isDragging)
            return;

        double position = e.GetPosition(this).X;
        Value = Math.Max(0, Math.Min(Maximum, position / Bounds.Width * Maximum));
    }

    /// <summary>
    /// Обработчик завершения перетаскивания
    /// </summary>
    /// <param name="e">Данные события</param>
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isDragging = false;
        e.Pointer.Capture(null);
    }

    /// <summary>
    /// Отрисовка waveform и курсора
    /// </summary>
    /// <param name="context">Контекст рисования Avalonia</param>
    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (_points.Count == 0)
            return;

        double width = Bounds.Width;
        double height = Bounds.Height;
        double playedWidth = width * (Value / Maximum);

        context.FillRectangle(
            Brushes.Transparent,
            new Rect(0, 0, width, height));

        for (int i = 0; i < _points.Count; i += 2)
        {
            var point1 = _points[i];
            var point2 = _points[i + 1];

            var brush = point1.X < playedWidth ? PlayedWaveformBrush : WaveformBrush;
            context.DrawLine(
                new Pen(brush, 1.5),
                point1,
                point2);
        }

        double cursorX = playedWidth;
        context.DrawLine(
            new Pen(CursorBrush, CursorWidth),
            new Point(cursorX, 0),
            new Point(cursorX, height));
    }
}