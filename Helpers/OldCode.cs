/// moving average
//if (klines.Count > 100)
//{
//    Path path = klinesView.Children.OfType<Path>().Where(p => p.Tag.ToString() == "MA").FirstOrDefault();
//    if (path != null)
//        klinesView.Children.Remove(path);

//    PointCollection points = new PointCollection();
//    int iminutes = -15 * Utils.IntervalInMinutes(Interval);
//    IEnumerable<Skender.Stock.Indicators.SmaResult> smaResults = Skender.Stock.Indicators.Indicator.GetSma(klines, 50);

//    double lowestBS = (double)smaResults.Where(o => o.Sma > 0).Min(o => o.Sma);
//    double highestBS = (double)smaResults.Max(o => o.Sma);
//    foreach (Skender.Stock.Indicators.SmaResult smaResult in smaResults)
//        if (smaResult.Sma.HasValue && smaResult.Sma.Value > 0)
//        {
//            double X1 = Utils.CalculateViewWidth(viewWidth, firstKline.OriginalKLine.OpenTime.Ticks, lastKline.OriginalKLine.CloseTime.Ticks, smaResult.Date.AddMinutes(iminutes).Ticks);
//            double Y1 = viewHeight - Utils.CalculateViewHeight(viewHeight, lowestBS, highestBS, (double)smaResult.Sma.Value);
//            points.Add(new Point(X1, Y1));
//        }

//    path = new Path();
//    path.Tag = "MA";
//    path.Stroke = System.Windows.Media.Brushes.Gray;
//    path.StrokeThickness = 1;

//    PolyLineSegment pls = new PolyLineSegment(points, true);
//    pls.Freeze();
//    PathFigure pf = new PathFigure();
//    pf.StartPoint = points.First();
//    pf.Segments.Add(pls);
//    pf.Freeze();

//    PathGeometry pg = new PathGeometry();
//    pg.Figures.Add(pf);
//    pg.Freeze();

//    path.Data = pg;
//    klinesView.Children.Add(path);
//}


/// MAoc
//if (klines.Count > 100)
//{
//    Path path = klinesView.Children.OfType<Path>().Where(p => p.Tag.ToString() == "MAoc").FirstOrDefault();
//    if (path != null)
//        klinesView.Children.Remove(path);

//    Point[] points = new Point[klines.Count * 2];

//    // add high values from left to right
//    for (int i = 0; i < klines.Count; i++)
//    {
//        CandleStick cs = klines[i];
//        double x = Utils.CalculateViewWidth(viewWidth, firstKline.OriginalKLine.OpenTime.Ticks, lastKline.OriginalKLine.CloseTime.Ticks, cs.OriginalKLine.CloseTime.Ticks);
//        double highY = viewHeight - Utils.CalculateViewHeight(viewHeight, lowestLowPrice, highestHighPrice, Math.Max(cs.Open, cs.Close));
//        double lowY = viewHeight - Utils.CalculateViewHeight(viewHeight, lowestLowPrice, highestHighPrice, Math.Min(cs.Open, cs.Close));
//        points[i].X = x;
//        points[i].Y = highY;
//        points[klines.Count * 2 - i - 1].X = x;
//        points[klines.Count * 2 - i - 1].Y = lowY;
//    }

//    path = new Path();
//    path.Tag = "MAoc";
//    path.Stroke = Brushes.Gray;
//    path.Fill = Brushes.Gray;
//    path.StrokeThickness = 1;

//    PolyLineSegment pls = new PolyLineSegment(points, true);
//    pls.Freeze();
//    PathFigure pf = new PathFigure();
//    pf.StartPoint = points.First();
//    pf.Segments.Add(pls);
//    pf.Freeze();

//    PathGeometry pg = new PathGeometry();
//    pg.Figures.Add(pf);
//    pg.Freeze();

//    path.Data = pg;
//    klinesView.Children.Add(path);
//}


/// MAlh
//if (klines.Count > 100)
//{
//    Path path = klinesView.Children.OfType<Path>().Where(p => p.Tag.ToString() == "MAlh").FirstOrDefault();
//    if (path != null)
//        klinesView.Children.Remove(path);

//    Point[] points = new Point[klines.Count * 2];

//    // add high values from left to right
//    for (int i = 0; i < klines.Count; i++)
//    {
//        CandleStick cs = klines[i];
//        double x = Utils.CalculateViewWidth(viewWidth, firstKline.OriginalKLine.OpenTime.Ticks, lastKline.OriginalKLine.CloseTime.Ticks, cs.OriginalKLine.CloseTime.Ticks);
//        double highY = viewHeight - Utils.CalculateViewHeight(viewHeight, lowestLowPrice, highestHighPrice, cs.High);
//        double lowY = viewHeight - Utils.CalculateViewHeight(viewHeight, lowestLowPrice, highestHighPrice, cs.Low);
//        points[i].X = x;
//        points[i].Y = highY;
//        points[klines.Count * 2 - i - 1].X = x;
//        points[klines.Count * 2 - i - 1].Y = lowY;
//    }

//    path = new Path();
//    path.Tag = "MAlh";
//    path.Stroke = Brushes.Gray;
//    path.Fill = Brushes.Gray;
//    path.StrokeThickness = 1;

//    PolyLineSegment pls = new PolyLineSegment(points, true);
//    pls.Freeze();
//    PathFigure pf = new PathFigure();
//    pf.StartPoint = points.First();
//    pf.Segments.Add(pls);
//    pf.Freeze();

//    PathGeometry pg = new PathGeometry();
//    pg.Figures.Add(pf);
//    pg.Freeze();

//    path.Data = pg;
//    klinesView.Children.Add(path);
//}


