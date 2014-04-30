using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace Earlvik.ArtiStereo

{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        
        private const double closeDistance = 0.25;
        //Number of toolbar buttons
        const int buttonNumber=5;
// ReSharper disable once InconsistentNaming
        int pixelPerMeter = 40;
        private const int minPixelPerMeter = 5;
        private const int maxPixelPerMeter = 320;
        private const int rightPanelWidth = 250;
        //List of handlers assigned to canvas mouseUp event
        readonly List<MouseButtonEventHandler> mCanvasMousehandlers = new List<MouseButtonEventHandler>();
        
        readonly List<UIElement> mMarkers = new List<UIElement>();
        readonly List<IRoomObject> mRedoElements = new List<IRoomObject>();
        readonly ToggleButton[] mToolButtons = new ToggleButton[buttonNumber];
        readonly List<IRoomObject> mUndoElements = new List<IRoomObject>();
        private Sound mBaseSound;
        private int mChosenButton = -1;
        private Sound mResultSound;
        private double mXDrawOffset = 0;
        private double mYDrawOffset = 0;
        //Room object
        Room mRoom;
        private IRoomObject mSelectedRoomObject;
        private const double numFieldWidth = 40;

        public MainWindow()
        {
            
            InitializeComponent();
            mToolButtons[0] = RectButton;
            mToolButtons[1] = LineButton;
            mToolButtons[2] = SourceButton;
            mToolButtons[3] = ListenerButton;
            mToolButtons[4] = MoveButton;
            foreach (ToggleButton button in mToolButtons)
            {
                button.Checked += ToolButtonToggled;
                button.Unchecked += (sender, args) => { 
                    mChosenButton = -1;
                    mMarkers.Clear();
                    DrawRoom();
                    foreach (MouseButtonEventHandler handler in mCanvasMousehandlers)
                    {
                        RoomCanvas.MouseUp -= handler;
                    }
                    RoomCanvas.MouseUp -= RoomCanvas_MouseUp;
                    RoomCanvas.MouseUp += RoomCanvas_MouseUp;
                };
            }
            mRoom = new Room();
            mRoom.CeilingHeight = 2;
            mRoom.CeilingMaterial = Wall.Material.Brick;
            mRoom.FloorMaterial =Wall.Material.Brick;
            //--------------------TESTING---TESTING---TESTING----------------
            mRoom.AddWall(new Wall(2,2,7,2,Wall.MaterialPreset.Glass));
            mRoom.AddWall(new Wall(2,4,7,4,Wall.MaterialPreset.Brick));
            mRoom.AddListener(new ListenerPoint(6,6,new Line(0,0,1,-1),ListenerPoint.Cardioid ));
            mRoom.AddSource(new SoundPoint(10,10,2));


            
            //---------------------------------------------------------------
            Loaded += delegate
            {
                DrawRoom();
                RoomPresetBox.ItemsSource = Enum.GetNames(typeof (Room.RoomPreset));

            };
            

        }

        private void AddMarker(System.Windows.Point position)
        {
            var first = new Ellipse { Fill = Brushes.Black, Stroke = Brushes.Black };
            first.Width = first.Height = 5;
            Canvas.SetLeft(first, position.X);
            Canvas.SetTop(first, position.Y);
            first.Visibility = Visibility.Visible;
            mMarkers.Add(first);
        }

        private void ClearMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ((TextBlock) PropsPanel.Children[0]).Text = "Properties";
            PropsPanel.Children.RemoveRange(1, PropsPanel.Children.Count - 1);
            mRoom = new Room();
            mSelectedRoomObject = null;
            mMarkers.Clear();
            foreach (MouseButtonEventHandler handler in mCanvasMousehandlers)
            {
                RoomCanvas.MouseUp -= handler;
            }
            RoomCanvas.MouseUp -= RoomCanvas_MouseUp;
            RoomCanvas.MouseUp += RoomCanvas_MouseUp;
            DrawRoom();
        }

        /// <summary>
        /// Method of repainting room objects on canvas based on mRoom objects
        /// </summary>
        private void DrawRoom()
        {
            RoomCanvas.Children.Clear();

            //==================Grid=============================
            const double dx = 20;
            const double dy = 20;
            int i = 0;
            while(i*dy<RoomCanvas.ActualHeight-20){
                i++;
                var lineH = new System.Windows.Shapes.Line
                {
                    StrokeThickness = 0.1,
                    Stroke = Brushes.Black,
                    X1 = 0,
                    Y1 = i*dy,
                    X2 = RoomCanvas.ActualWidth,
                    Y2 = i*dy,
                    Visibility = Visibility.Visible
                };
                RoomCanvas.Children.Add(lineH);
            }
            i = 0;
            while(i*dx<RoomCanvas.ActualWidth-20){
                i++;
                var lineV = new System.Windows.Shapes.Line
                {
                    StrokeThickness = 0.1,
                    Stroke = Brushes.Black,
                    X1 = i*dx,
                    Y1 = 0,
                    X2 = i*dx,
                    Y2 = RoomCanvas.ActualHeight,
                    Visibility = Visibility.Visible
                };

                RoomCanvas.Children.Add(lineV);
                
            }

            //=========================WALLS========================

            foreach (Wall wall in mRoom.Walls)
            {
                var line = new System.Windows.Shapes.Line
                {
                    X1 = pixelPerMeter*wall.Start.X+mXDrawOffset,
                    X2 = pixelPerMeter*wall.End.X+mXDrawOffset,
                    Y1 = pixelPerMeter*wall.Start.Y+mYDrawOffset,
                    Y2 = pixelPerMeter*wall.End.Y+mYDrawOffset,
                    StrokeThickness = 3
                };
                if (ReferenceEquals(mSelectedRoomObject, wall))
                {
                    line.Stroke = Brushes.Chartreuse;
                }
                else
                {
                    switch (wall.MatPreset)
                    {
                        case Wall.MaterialPreset.Brick:
                        {
                            line.Stroke = Brushes.Orange;
                            break;
                        }
                        case Wall.MaterialPreset.Glass:
                        {
                            line.Stroke = Brushes.CornflowerBlue;
                            break;
                        }
                        case Wall.MaterialPreset.Granite:
                        {
                            line.Stroke = Brushes.LightSlateGray;
                            break;
                        }
                        case Wall.MaterialPreset.OakWood:
                        {
                            line.Stroke = Brushes.Chocolate;
                            break;
                        }
                        case Wall.MaterialPreset.Rubber:
                        {
                            line.Stroke = Brushes.DarkSalmon;
                            break;
                        }
                        default:
                        {
                            line.Stroke = Brushes.SteelBlue;
                            break;
                        }
                    }
                }
                line.Visibility = Visibility.Visible;
                RoomCanvas.Children.Add(line);
            }

            //===============================SOUND POINTS=======================
            const double picSize = 20;
            foreach (ListenerPoint listener in mRoom.Listeners)
            {
                
                var image = new Image
                {
                    Source = new BitmapImage(new Uri("pack://application:,,,/Images/micPict.png")),
                    Width = picSize,
                    Height = picSize
                };
                double x = listener.X*pixelPerMeter-(picSize/2) + mXDrawOffset;
                double y = listener.Y*pixelPerMeter-(picSize/2) + mYDrawOffset;
                Canvas.SetTop(image,y);
                Canvas.SetLeft(image,x);
                if (ReferenceEquals(listener, mSelectedRoomObject))
                {
                    var ellipse = new Ellipse
                    {
                        Width = picSize,
                        Height = picSize,
                        Stroke = Brushes.Chartreuse,
                        Fill = Brushes.Chartreuse
                    };
                    Canvas.SetTop(ellipse,y);
                    Canvas.SetLeft(ellipse,x);
                    ellipse.Visibility = Visibility.Visible;
                    RoomCanvas.Children.Add(ellipse);
                }
                RoomCanvas.Children.Add(image);

                if (listener.Directional)
                {
                    double angle = listener.DirectionAngle;
                    var line = new System.Windows.Shapes.Line
                    {
                        X1 = x + (picSize/2),
                        Y1 = y + (picSize/2),
                        X2 = x + (picSize/2) + Math.Cos(angle)*(picSize/2),
                        Y2 = y + (picSize/2) + Math.Sin(angle)*(picSize/2),
                        Stroke = Brushes.Red,
                        StrokeThickness = 1
                    };
                    RoomCanvas.Children.Add(line);
                }
                
            }

            foreach (SoundPoint source in mRoom.Sources)
            {
                var image = new Image
                {
                    Source = new BitmapImage(new Uri("pack://application:,,,/Images/notePict.png")),
                    Width = picSize,
                    Height = picSize
                };
                double x = source.X * pixelPerMeter - (picSize / 2) +mXDrawOffset;
                double y = source.Y * pixelPerMeter - (picSize / 2) +mYDrawOffset;
                Canvas.SetTop(image, y);
                Canvas.SetLeft(image, x);
                if (ReferenceEquals(source, mSelectedRoomObject))
                {
                    Ellipse ellipse = new Ellipse
                    {
                        Width = picSize,
                        Height = picSize,
                        Stroke = Brushes.Chartreuse,
                        Fill = Brushes.Chartreuse
                    };
                    Canvas.SetTop(ellipse, y);
                    Canvas.SetLeft(ellipse, x);
                    ellipse.Visibility = Visibility.Visible;
                    RoomCanvas.Children.Add(ellipse);
                }
                RoomCanvas.Children.Add(image);
            }
            //===================MARKERS==============
            foreach (UIElement marker in mMarkers)
            {
                RoomCanvas.Children.Add(marker);
            }
            //===================ZOOM=BUTTONS=========
            Button zoomInButton = new Button{Content = "Zoom In"};
            Button zoomOutButton = new Button {Content = "Zoom Out"};
            Canvas.SetRight(zoomOutButton,10);
            Canvas.SetTop(zoomOutButton,5);
            Canvas.SetRight(zoomInButton,70);
            Canvas.SetTop(zoomInButton,5);
            RoomCanvas.Children.Add(zoomOutButton);
            RoomCanvas.Children.Add(zoomInButton);

            zoomInButton.Click += delegate
            {
                if (pixelPerMeter < maxPixelPerMeter)
                {
                    pixelPerMeter *= 2;
                    DrawRoom();
                    zoomOutButton.IsEnabled = true;
                    if (pixelPerMeter >= maxPixelPerMeter)
                    {
                        zoomInButton.IsEnabled = false;
                    }
                }
            };

            zoomOutButton.Click+=delegate
            {
                if (pixelPerMeter > minPixelPerMeter)
                {
                    pixelPerMeter /= 2;
                    DrawRoom();
                    zoomInButton.IsEnabled = true;
                    if (pixelPerMeter <= minPixelPerMeter)
                    {
                        zoomOutButton.IsEnabled = false;
                    }
                }
            };
        }

        private void RecordButton_Click(object sender, RoutedEventArgs e)
        {
            if (!mRoom.IsValid())
            {
                MessageBox.Show(this, "Room is invalid, it should be closed by walls");
                return;
            }
            try
            {
                ListenerPoint left = mRoom.Listeners[0];
                ListenerPoint right = mRoom.Listeners[0];
                for (int i = 0; i < mRoom.Listeners.Count; i++)
                {
                    if (left.X > mRoom.Listeners[i].X) left = mRoom.Listeners[i];
                    if (right.X < mRoom.Listeners[i].X) right = mRoom.Listeners[i];
                }
                mRoom.CalculateSound();
                mResultSound = new Sound(mRoom.Listeners.Count, mBaseSound.DiscretionRate, mBaseSound.BitsPerSample);
                mResultSound.Add(left.Sound, 0, 0, 0);
                mResultSound.Add(right.Sound, 0, 1, 0);
                mResultSound.AdjustVolume(0.75);
                SoundSaveMenuItem.IsEnabled = true;
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, "Error ocured during recording process: " + exception.Message);
            }
        }

        private void RedoMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ((TextBlock) PropsPanel.Children[0]).Text = "Properties";
            PropsPanel.Children.RemoveRange(1, PropsPanel.Children.Count - 1);
            IRoomObject lastObject = mRedoElements.Last();
            mUndoElements.Add(lastObject);
            mRedoElements.Remove(lastObject);
            if (lastObject is Wall)
            {
                mRoom.AddWall(lastObject as Wall);
            }
            else if (lastObject is ListenerPoint)
            {
                mRoom.AddListener(lastObject as ListenerPoint);
            }
            else
            {
                mRoom.AddSource(lastObject as SoundPoint);
            }
            if (mRedoElements.Count == 0) RedoMenuItem.IsEnabled = false;
            UndoMenuItem.IsEnabled = true;
            DrawRoom();
        }

        private void RoomCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (mChosenButton == 4)
            {
                double xPrev = e.GetPosition(this).X;
                double yPrev = e.GetPosition(this).Y;
                MouseEventHandler moveHandler = null;
                
                moveHandler = (moveSender, args) =>
                {

                    if (args.LeftButton == MouseButtonState.Released)
                    {
                        RoomCanvas.Cursor = Cursors.Arrow;
                        RoomCanvas.MouseMove -= moveHandler;
                        moveHandler = null;
                        DrawRoom();
                        return;
                    }
                    RoomCanvas.Cursor = Cursors.ScrollAll;
                    double dx = (args.GetPosition(this).X - xPrev);
                    double dy = (args.GetPosition(this).Y - yPrev);
                    mXDrawOffset += dx;
                    mYDrawOffset += dy;
                    xPrev = args.GetPosition(this).X;
                    yPrev = args.GetPosition(this).Y;
                    DrawRoom();
                    
                };
                RoomCanvas.MouseMove += moveHandler;
                return;
            }
            if (mSelectedRoomObject != null)
            {
                double xPrev = e.GetPosition(RoomCanvas).X;
                double yPrev = e.GetPosition(RoomCanvas).Y;
                if (mSelectedRoomObject is SoundPoint && Geometry.Distance(new Point((xPrev-mXDrawOffset)/pixelPerMeter,(yPrev-mYDrawOffset)/pixelPerMeter), mSelectedRoomObject as SoundPoint )<closeDistance)
                {
                    MouseEventHandler moveHandler = null;
                    moveHandler = (moveSender, args) =>
                    {
                       
                        if (args.LeftButton == MouseButtonState.Released)
                        {
                          
                            RoomCanvas.MouseMove -= moveHandler;
                            moveHandler = null;
                            DrawRoom();
                            UpdateProps(mSelectedRoomObject);
                            return;
                        }
                        double dx = (args.GetPosition(RoomCanvas).X - xPrev);
                        double dy = (args.GetPosition(RoomCanvas).Y - yPrev);
                        ((SoundPoint)mSelectedRoomObject).X += dx / pixelPerMeter;
                        ((SoundPoint)mSelectedRoomObject).Y += dy / pixelPerMeter;
                        xPrev = args.GetPosition(RoomCanvas).X;
                        yPrev = args.GetPosition(RoomCanvas).Y;
                        UpdateProps(mSelectedRoomObject);
                        DrawRoom();
                    };


                    RoomCanvas.MouseMove += moveHandler;

                }
                else if (mSelectedRoomObject is Wall)
                {
                    Point clickPoint = new Point((xPrev-mXDrawOffset)/pixelPerMeter, (yPrev-mYDrawOffset)/pixelPerMeter);
                    Point point =
                        (Geometry.Distance(clickPoint,
                            (mSelectedRoomObject as Wall).Start) <
                         Geometry.Distance(clickPoint,
                             (mSelectedRoomObject as Wall).End))
                            ? (mSelectedRoomObject as Wall).Start
                            : (mSelectedRoomObject as Wall).End;
                    if (Geometry.Distance(clickPoint, point) > closeDistance) return;
                    MouseEventHandler moveHandler = null;
                    moveHandler = (moveSender, moveE) =>
                    {
                        RoomCanvas.Cursor = Cursors.None;
                        if (moveE.LeftButton == MouseButtonState.Released)
                        {
                            //Magneting
                            foreach (Wall wall in mRoom.Walls)
                            {
                                if (Geometry.Distance(wall.Start, point) < closeDistance)
                                {
                                    point.X = wall.Start.X;
                                    point.Y = wall.Start.Y;
                                    wall.Start = point;
                                    break;
                                }
                                if (Geometry.Distance(wall.End, point) < closeDistance)
                                {
                                    point.X = wall.End.X;
                                    point.Y = wall.End.Y;
                                    wall.End = point;
                                    break;
                                }
                            }
                            RoomCanvas.Cursor = Cursors.Arrow;
                            RoomCanvas.MouseMove -= moveHandler;
                            moveHandler = null;
                            DrawRoom();
                            UpdateProps(mSelectedRoomObject);
                            return;
                        }
                        double dx = (moveE.GetPosition(RoomCanvas).X - xPrev);
                        double dy = (moveE.GetPosition(RoomCanvas).Y - yPrev);
                        point.X += dx/pixelPerMeter;
                        point.Y += dy/pixelPerMeter;
                        
                        xPrev = moveE.GetPosition(RoomCanvas).X;
                        yPrev = moveE.GetPosition(RoomCanvas).Y;
                        UpdateProps(mSelectedRoomObject);
                        DrawRoom();
                    };
                    RoomCanvas.MouseMove += moveHandler;
                }
            }
        }

        private void RoomCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (mSelectedRoomObject is Wall)
            {
                double xPrev = e.GetPosition(RoomCanvas).X;
                double yPrev = e.GetPosition(RoomCanvas).Y;
                Point clickPoint = new Point((xPrev-mXDrawOffset)/pixelPerMeter, (yPrev-mYDrawOffset)/pixelPerMeter);
                Point point =
                    (Geometry.Distance(clickPoint,
                        (mSelectedRoomObject as Wall).Start) <
                     Geometry.Distance(clickPoint,
                         (mSelectedRoomObject as Wall).End))
                        ? (mSelectedRoomObject as Wall).Start
                        : (mSelectedRoomObject as Wall).End;
                if (Geometry.Distance(clickPoint, point) >closeDistance)
                {
                    RoomCanvas.Cursor = Cursors.Arrow;
                    return;
                }
                RoomCanvas.Cursor = Cursors.Cross;
            }
            else
            {
                RoomCanvas.Cursor = Cursors.Arrow;
            }
        }

        private void RoomCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            switch (mChosenButton)
            {
                case 0:
                {
                    //Add wall rectangle
                    System.Windows.Point position = e.GetPosition(RoomCanvas);
                    AddMarker(position);
                    Point leftTop = new Point((position.X-mXDrawOffset)/pixelPerMeter, (position.Y-mYDrawOffset)/pixelPerMeter);
                    RoomCanvas.MouseUp -= RoomCanvas_MouseUp;
                    MouseButtonEventHandler roomCanvasRectDraw = null;
                    roomCanvasRectDraw=(sndr, earg) =>
                    {
                        mMarkers.Remove(mMarkers.Last());
                        RoomCanvas.MouseUp-=roomCanvasRectDraw;
                        RoomCanvas.MouseUp += RoomCanvas_MouseUp;
                        position = earg.GetPosition(RoomCanvas);
                        Point bottomRight = new Point((position.X - mXDrawOffset) / pixelPerMeter, (position.Y - mYDrawOffset) / pixelPerMeter);
                        if (Geometry.Distance(leftTop, bottomRight) < 0.25)
                        {
                            mMarkers.Clear();
                            DrawRoom();
                            return;
                        }
                        Point topRight = new Point(bottomRight.X,leftTop.Y);
                        Point bottomLeft = new Point(leftTop.X,bottomRight.Y);
                        mUndoElements.Add(mRoom.AddWall(new Wall(leftTop,topRight,Wall.MaterialPreset.OakWood)));
                        mUndoElements.Add(mRoom.AddWall(new Wall(topRight, bottomRight, Wall.MaterialPreset.OakWood)));
                        mUndoElements.Add(mRoom.AddWall(new Wall(bottomRight, bottomLeft, Wall.MaterialPreset.OakWood)));
                        mUndoElements.Add(mRoom.AddWall(new Wall(leftTop, bottomLeft, Wall.MaterialPreset.OakWood)));
                        UndoMenuItem.IsEnabled = true;
                        DrawRoom();
                        
                    };
                    mCanvasMousehandlers.Add(roomCanvasRectDraw);
                    RoomCanvas.MouseUp += roomCanvasRectDraw;
                    break;
                }
                case 1:
                {
                    //Add wall
                   
                    System.Windows.Point position = e.GetPosition(RoomCanvas);
                    AddMarker(position);
                    Point first = new Point((position.X-mXDrawOffset) / pixelPerMeter, (position.Y - mYDrawOffset) / pixelPerMeter);
                    //Magneting
                    foreach (Wall wall in mRoom.Walls)
                    {
                        if (Geometry.Distance(wall.Start, first) < closeDistance)
                        {
                            first = wall.Start;
                            break;
                        }
                        if (Geometry.Distance(wall.End, first) < closeDistance)
                        {
                            first = wall.End;
                            break;
                        }
                    }
                    RoomCanvas.MouseUp -= RoomCanvas_MouseUp;
                    MouseButtonEventHandler roomCanvasLineDraw = null;
                    roomCanvasLineDraw = (sndr, earg) =>
                    {
                        mMarkers.Remove(mMarkers.Last());
                        RoomCanvas.MouseUp -= roomCanvasLineDraw;
                        RoomCanvas.MouseUp+=RoomCanvas_MouseUp;
                        position = earg.GetPosition(RoomCanvas);
                        Point second = new Point((position.X-mXDrawOffset) / pixelPerMeter, (position.Y - mYDrawOffset) / pixelPerMeter);
                        if (Geometry.Distance(first, second) < closeDistance)
                        {
                            mMarkers.Clear();
                            DrawRoom();
                            return;
                        }
                        //Magneting
                        foreach (Wall wall in mRoom.Walls)
                        {
                            if (Geometry.Distance(wall.Start, second) < closeDistance)
                            {
                                second = wall.Start;
                                break;
                            }
                            if (Geometry.Distance(wall.End, second) < closeDistance)
                            {
                                second = wall.End;
                                break;
                            }
                        }
                        mUndoElements.Add(mRoom.AddWall(new Wall(first,second,Wall.MaterialPreset.OakWood)));
                        UndoMenuItem.IsEnabled = true;
                        DrawRoom();
                    };
                    mCanvasMousehandlers.Add(roomCanvasLineDraw);
                    RoomCanvas.MouseUp += roomCanvasLineDraw;
                    break;
                }
                case 2:
                {
                    //Add source
                    System.Windows.Point position = e.GetPosition(RoomCanvas);
                    SoundPoint point = new SoundPoint((position.X-mXDrawOffset)/pixelPerMeter, (position.Y-mYDrawOffset)/pixelPerMeter);
                    if (mBaseSound != null)
                    {
                        point.Sound = mBaseSound;
                    }
                    mUndoElements.Add(point);
                    mRoom.AddSource(point);
                    break;
                }
                case 3:
                {
                    //Add listener
                    System.Windows.Point position = e.GetPosition(RoomCanvas);
                    ListenerPoint point = new ListenerPoint((position.X-mXDrawOffset) / pixelPerMeter, (position.Y-mYDrawOffset) / pixelPerMeter);
                    mUndoElements.Add(point);
                    mRoom.AddListener(point);
                    break;
                }
                case 4:
                {
                    break;
                }
                default:
                {
                    //Select element
                    System.Windows.Point position = e.GetPosition(RoomCanvas);
                    Point point = new Point((position.X-mXDrawOffset) / pixelPerMeter, (position.Y-mYDrawOffset) / pixelPerMeter);
                    mSelectedRoomObject = SelectObject(point);
                    if (mSelectedRoomObject == null)
                    {
                        ((TextBlock) PropsPanel.Children[0]).Text = "Properties";
                        PropsPanel.Children.RemoveRange(1,PropsPanel.Children.Count-1);
                        mSelectedRoomObject = null;
                        DrawRoom();
                        return;
                    }
                    if (mSelectedRoomObject is Wall)
                    {
                        Wall wall = mSelectedRoomObject as Wall;
                        UpdateWallProps(wall);

                    }
                    else if (mSelectedRoomObject is ListenerPoint)
                    {
                        ListenerPoint listener = mSelectedRoomObject as ListenerPoint;
                        UpdateListenerProps(listener);
                    }
                    else if (mSelectedRoomObject is SoundPoint)
                    {
                        SoundPoint source = mSelectedRoomObject as SoundPoint;
                        UpdateSourceProps(source);
                    }
                    break;
                }
            }
            DrawRoom();
        }

        private void RoomCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawRoom();
        }

        private void RoomOpenMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog { CheckFileExists = true, Filter = "Serialized room files (*.asr)|*.asr" };
            if (dialog.ShowDialog(this) == true)
            {
                BinaryFormatter formatter = new BinaryFormatter();
                using (FileStream stream = new FileStream(dialog.FileName, FileMode.Open))
                {
                    try
                    {
                        mRoom = (Room) formatter.Deserialize(stream);
                        mXDrawOffset = 0;
                        mYDrawOffset = 0;
                        DrawRoom();
                    }
                    catch (Exception exception)
                    {
                        MessageBox.Show(this, "Error occured during opening process: " + exception.Message);
                    }
                }
            }
        }

        private void RoomSaveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog { AddExtension = true, CheckPathExists = true, Filter = "Serialized room files (*.asr)|*.asr" };
            bool? result = dialog.ShowDialog(this);
            if (result == true)
            {
                BinaryFormatter formatter = new BinaryFormatter();
                using (FileStream stream = new FileStream(dialog.FileName, FileMode.OpenOrCreate))
                {
                    try
                    {
                        formatter.Serialize(stream, mRoom);
                    }
                    catch (Exception exception)
                    {
                        MessageBox.Show(this, "Error occured during saving process: " + exception.Message);
                    }
                }
            }
        }

        private IRoomObject SelectObject(Point point)
        {
            IRoomObject result = null;
            double distance = 0.3;
            foreach (Wall wall in mRoom.Walls)
            {
                if (Geometry.Distance(point, wall) < distance && Geometry.ParallelProjection(wall,point,false)!=null)
                {
                    distance = Geometry.Distance(point, wall);
                    result = wall;
                }
            }
            foreach (SoundPoint source in mRoom.Sources)
            {
                if (Geometry.Distance(point, source) < distance)
                {
                    distance = Geometry.Distance(point, source);
                    result = source;
                }
            }
            foreach (ListenerPoint listener in mRoom.Listeners)
            {
                if (Geometry.Distance(point, listener) < distance)
                {
                    distance = Geometry.Distance(point, listener);
                    result = listener;
                }
            }
            return result;
        }

        private void SoundOpenMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog {CheckPathExists = true, CheckFileExists = true, Filter = "Wave Sound Files (*.wav)|*.wav"};
            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                mBaseSound = Sound.GetSoundFromWav(dialog.FileName);
                RecordButton.IsEnabled = true;
                MessageBox.Show("Successfully opened " + dialog.FileName,"Sound Open",MessageBoxButton.OK);
                foreach (SoundPoint source in mRoom.Sources)
                {
                    source.Sound = mBaseSound;
                }
            }
        }

        private void SoundSaveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (mResultSound != null)
            {
                SaveFileDialog dialog = new SaveFileDialog { AddExtension = true, CheckPathExists = true, Filter = "Wave Sound Files (*.wav)|*.wav" };
                if (dialog.ShowDialog(this) == true)
                {
                    mResultSound.CreateWav(dialog.FileName);
                }
            }
        }

        private void ToolButtonToggled(object sender, RoutedEventArgs e)
        {
            ((TextBlock) PropsPanel.Children[0]).Text = "Properties";
            PropsPanel.Children.RemoveRange(1, PropsPanel.Children.Count - 1);
            mSelectedRoomObject = null;
            int chosen = -1;
            mMarkers.Clear();
            DrawRoom();
            foreach (MouseButtonEventHandler handler in mCanvasMousehandlers)
            {
                RoomCanvas.MouseUp -= handler;
            }
            RoomCanvas.MouseUp -= RoomCanvas_MouseUp;
            RoomCanvas.MouseUp+=RoomCanvas_MouseUp;
            for (int i = 0; i < buttonNumber; i++)
            {
                
                if (mToolButtons[i] == sender)
                {
                    chosen = i;
                }
                else
                {
                    mToolButtons[i].IsChecked = false;
                }
                mChosenButton = chosen;
            }
        }

        private void UndoMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ((TextBlock) PropsPanel.Children[0]).Text = "Properties";
            PropsPanel.Children.RemoveRange(1, PropsPanel.Children.Count - 1);
            IRoomObject lastObject = mUndoElements.Last();
            mRedoElements.Add(lastObject);
            mUndoElements.Remove(lastObject);
            if (lastObject is Wall)
            {
                mRoom.RemoveWall(lastObject as Wall);
            }
            else if (lastObject is ListenerPoint)
            {
                mRoom.RemoveListener(lastObject as ListenerPoint);
            }
            else
            {
                mRoom.RemoveSource(lastObject as SoundPoint);
            }
            if (mUndoElements.Count == 0) UndoMenuItem.IsEnabled = false;
            RedoMenuItem.IsEnabled = true;
            DrawRoom();
        }

        private void UpdateListenerProps(ListenerPoint listener)
        {
            PropsPanel.Children.Clear();
            TextBlock name = new TextBlock
            {
                FontSize = 20,
                Text = "LISTENER",
                TextAlignment = TextAlignment.Center,
                Width = rightPanelWidth
            };
            PropsPanel.Children.Add(name);
            TextBlock location = new TextBlock { FontSize = 14, Text = "Location", Width = rightPanelWidth, TextAlignment = TextAlignment.Center };
            PropsPanel.Children.Add(location);
            StackPanel locationPanel = new StackPanel { Orientation = Orientation.Horizontal, Width = rightPanelWidth };
            TextBlock xblock = new TextBlock { FontSize = 10, Text = "X: ", Margin = new Thickness(5, 5, 5, 0) };
            locationPanel.Children.Add(xblock);
            TextBox xbox = new TextBox{ FontSize = 10, Text = listener.X + "", Width = numFieldWidth, TextAlignment = TextAlignment.Left };
            locationPanel.Children.Add(xbox);
            TextBlock yblock = new TextBlock { FontSize = 10, Text = "Y: ", Margin = new Thickness(5, 5, 5, 0) };
            locationPanel.Children.Add(yblock);
            TextBox ybox = new TextBox{ FontSize = 10, Text = listener.Y + "", Width = numFieldWidth, TextAlignment = TextAlignment.Left };
            locationPanel.Children.Add(ybox);
            PropsPanel.Children.Add(locationPanel);

            CheckBox directional = new CheckBox{Width = rightPanelWidth, Content = "Directional", IsChecked = listener.Directional};
            PropsPanel.Children.Add(directional);
            TextBox xdirbox = null;
            TextBox ydirbox = null;
            if (listener.Directional)
            {
                TextBlock direction = new TextBlock { FontSize = 14, Text = "Direction vector", Width = rightPanelWidth, TextAlignment = TextAlignment.Center };
                PropsPanel.Children.Add(direction);
                StackPanel directionPanel = new StackPanel { Orientation = Orientation.Horizontal, Width = rightPanelWidth };
                TextBlock xdirblock = new TextBlock { FontSize = 10, Text = "X: ", Margin = new Thickness(5, 5, 5, 0) };
                directionPanel.Children.Add(xdirblock);
                xdirbox = new TextBox { FontSize = 10, Text = listener.DirectionX + "", Width = numFieldWidth, TextAlignment = TextAlignment.Left };
                directionPanel.Children.Add(xdirbox);
                TextBlock ydirblock = new TextBlock { FontSize = 10, Text = "Y: ", Margin = new Thickness(5, 5, 5, 0) };
                directionPanel.Children.Add(ydirblock);
                ydirbox = new TextBox { FontSize = 10, Text = listener.DirectionY + "", Width = numFieldWidth, TextAlignment = TextAlignment.Left };
                directionPanel.Children.Add(ydirbox);
                PropsPanel.Children.Add(directionPanel);
            }

            directional.Checked += delegate
            {
                for (int i = 0; i < mRoom.Listeners.Count; i++)
                {
                    if (mRoom.Listeners[i] == listener)
                    {
                        mRoom.Listeners[i]=new ListenerPoint(listener,new Line(0,0,1,1),ListenerPoint.Cardioid );
                        mSelectedRoomObject = mRoom.Listeners[i];
                        DrawRoom();
                        UpdateListenerProps(mRoom.Listeners[i]);
                        return;
                    }
                }
            };

            directional.Unchecked += delegate
            {
                for (int i = 0; i < mRoom.Listeners.Count; i++)
                {
                    if (mRoom.Listeners[i] == listener)
                    {
                        mRoom.Listeners[i] = new ListenerPoint(listener);
                        mSelectedRoomObject = mRoom.Listeners[i];
                        DrawRoom();
                        UpdateListenerProps(mRoom.Listeners[i]);
                    }
                }
            };

            xbox.TextChanged += delegate
            {
                double newValue;
                if (!double.TryParse(xbox.Text, out newValue)) return;
                for (int i = 0; i < mRoom.Listeners.Count; i++)
                {
                    if (mRoom.Listeners[i] == (ListenerPoint)mSelectedRoomObject)
                    {
                        mRoom.Listeners[i].X = newValue;
                        DrawRoom();
                        return;
                    }
                }
                
            };
            xbox.LostFocus += delegate
            {
                var listenerPoint = mSelectedRoomObject as ListenerPoint;
                if (listenerPoint != null)
                    xbox.Text = listenerPoint.X+"";
            };

            ybox.TextChanged += delegate
            {
                double newValue;
                if (!double.TryParse(ybox.Text, out newValue)) return;
                for (int i = 0; i < mRoom.Listeners.Count; i++)
                {
                    if (mRoom.Listeners[i] == (ListenerPoint)mSelectedRoomObject)
                    {
                        mRoom.Listeners[i].Y = newValue;
                        DrawRoom();
                        return;
                    }
                }

            };
            ybox.LostFocus += delegate
            {
                var listenerPoint = mSelectedRoomObject as ListenerPoint;
                if (listenerPoint != null)
                    ybox.Text = listenerPoint.Y + "";
            };

            if (xdirbox != null)
            {
                xdirbox.TextChanged += delegate
                {
                    double newValue;
                    if (!double.TryParse(xdirbox.Text, out newValue)) return;
                    for (int i = 0; i < mRoom.Listeners.Count; i++)
                    {
                        if (mRoom.Listeners[i] == (ListenerPoint) mSelectedRoomObject)
                        {
                            mRoom.Listeners[i].DirectionX = newValue;
                            DrawRoom();
                            return;
                        }
                    }

                };
                xdirbox.LostFocus += delegate
                {
                    var listenerPoint = mSelectedRoomObject as ListenerPoint;
                    if (listenerPoint != null)
                        xdirbox.Text = listenerPoint.DirectionX + "";
                };
            }

            if (ydirbox != null)
            {
                ydirbox.TextChanged += delegate
                {
                    double newValue;
                    if (!double.TryParse(ydirbox.Text, out newValue)) return;
                    for (int i = 0; i < mRoom.Listeners.Count; i++)
                    {
                        if (mRoom.Listeners[i] == (ListenerPoint)mSelectedRoomObject)
                        {
                            mRoom.Listeners[i].DirectionY = newValue;
                            DrawRoom();
                            return;
                        }
                    }

                };
                ydirbox.LostFocus += delegate
                {
                    var listenerPoint = mSelectedRoomObject as ListenerPoint;
                    if (listenerPoint != null)
                        ydirbox.Text = listenerPoint.DirectionY + "";
                };

                
            }
            Button deleteButton = new Button { Content = "Delete listener", HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(10, 10, 10, 10) };
            PropsPanel.Children.Add(deleteButton);
            deleteButton.Click += delegate
            {
                ((TextBlock)PropsPanel.Children[0]).Text = "Properties";
                PropsPanel.Children.RemoveRange(1, PropsPanel.Children.Count - 1);
                mRoom.RemoveListener(mSelectedRoomObject as ListenerPoint);
                mSelectedRoomObject = null;
                DrawRoom();
            };
        }

        private void UpdateProps(IRoomObject obj)
        {
            if (obj is Wall)
            {
                UpdateWallProps((Wall)obj);
            }
            else if (obj is ListenerPoint)
            {
                UpdateListenerProps((ListenerPoint) obj);
            }
            else if(obj is SoundPoint)
            {
                UpdateSourceProps((SoundPoint)obj);
            }
        }

        private void UpdateSourceProps(SoundPoint source)
        {
            PropsPanel.Children.Clear();
            TextBlock name = new TextBlock
            {
                FontSize = 20,
                Text = "SOURCE",
                TextAlignment = TextAlignment.Center,
                Width = rightPanelWidth
            };
            PropsPanel.Children.Add(name);
            TextBlock location = new TextBlock { FontSize = 14, Text = "Location", Width = rightPanelWidth, TextAlignment = TextAlignment.Center };
            PropsPanel.Children.Add(location);
            StackPanel locationPanel = new StackPanel { Orientation = Orientation.Horizontal, Width = rightPanelWidth };
            TextBlock xblock = new TextBlock { FontSize = 10, Text = "X: ", Margin = new Thickness(5, 5, 5, 0) };
            locationPanel.Children.Add(xblock);
            TextBox xbox = new TextBox { FontSize = 10, Text = source.X + "", Width = numFieldWidth, TextAlignment = TextAlignment.Left };
            locationPanel.Children.Add(xbox);
            TextBlock yblock = new TextBlock { FontSize = 10, Text = "Y: ", Margin = new Thickness(5, 5, 5, 0) };
            locationPanel.Children.Add(yblock);
            TextBox ybox = new TextBox { FontSize = 10, Text = source.Y + "", Width = numFieldWidth, TextAlignment = TextAlignment.Left };
            locationPanel.Children.Add(ybox);
            PropsPanel.Children.Add(locationPanel);
            Button deleteButton = new Button { Content = "Delete source", HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(10, 10, 10, 10) };
            PropsPanel.Children.Add(deleteButton);

            xbox.TextChanged += delegate
            {
                double newValue;
                if (!double.TryParse(xbox.Text, out newValue)) return;
                for (int i = 0; i < mRoom.Sources.Count; i++)
                {
                    if (mRoom.Sources[i] == (SoundPoint)mSelectedRoomObject)
                    {
                        mRoom.Sources[i].X = newValue;
                        DrawRoom();
                        return;
                    }
                }

            };
            xbox.LostFocus += delegate
            {
                var sourcePoint = mSelectedRoomObject as SoundPoint;
                if (sourcePoint != null)
                    xbox.Text = sourcePoint.X + "";
            };

            ybox.TextChanged += delegate
            {
                double newValue;
                if (!double.TryParse(ybox.Text, out newValue)) return;
                for (int i = 0; i < mRoom.Sources.Count; i++)
                {
                    if (mRoom.Sources[i] == (SoundPoint)mSelectedRoomObject)
                    {
                        mRoom.Sources[i].Y = newValue;
                        DrawRoom();
                        return;
                    }
                }

            };
            ybox.LostFocus += delegate
            {
                var sourcePoint = mSelectedRoomObject as SoundPoint;
                if (sourcePoint != null)
                    ybox.Text = sourcePoint.Y + "";
            };

            
            deleteButton.Click += delegate
            {
                ((TextBlock)PropsPanel.Children[0]).Text = "Properties";
                PropsPanel.Children.RemoveRange(1, PropsPanel.Children.Count - 1);
                mRoom.RemoveSource(mSelectedRoomObject as SoundPoint);
                mSelectedRoomObject = null;
                DrawRoom();
            };
        }

        private void UpdateWallProps(Wall wall)
        {
            PropsPanel.Children.Clear();
            TextBlock name = new TextBlock
            {
                FontSize = 20,
                Text = "WALL",
                TextAlignment = TextAlignment.Center,
                Width = rightPanelWidth
            };
            PropsPanel.Children.Add(name);
            TextBlock material = new TextBlock
            {
                FontSize = 14,
                Text = "Material",
                Width = rightPanelWidth,
                TextAlignment = TextAlignment.Center
            };
            PropsPanel.Children.Add(material);
            ComboBox materialBox = new ComboBox {Width = 180};
            materialBox.Items.Add(Wall.MaterialPreset.Brick);
            materialBox.Items.Add(Wall.MaterialPreset.Glass);
            materialBox.Items.Add(Wall.MaterialPreset.Granite);
            materialBox.Items.Add(Wall.MaterialPreset.OakWood);
            materialBox.Items.Add(Wall.MaterialPreset.Rubber);
            materialBox.Items.Add(Wall.MaterialPreset.None);
            materialBox.SelectedItem = wall.MatPreset;
            PropsPanel.Children.Add(materialBox);
            TextBlock matParams = new TextBlock { FontSize = 14, Text = "Material paramaters", Width = rightPanelWidth, TextAlignment = TextAlignment.Center };
            PropsPanel.Children.Add(matParams);
            TextBlock density = new TextBlock { FontSize = 10, Text = "Density", Width = rightPanelWidth, TextAlignment = TextAlignment.Left };
            PropsPanel.Children.Add(density);
            TextBox densityBox = new TextBox { FontSize = 10, Text = wall.WallMaterial.Density + "", Width = rightPanelWidth, TextAlignment = TextAlignment.Left };
            PropsPanel.Children.Add(densityBox);
            TextBlock soundspeed = new TextBlock { FontSize = 10, Text = "Speed of sound", Width = rightPanelWidth, TextAlignment = TextAlignment.Left };
            PropsPanel.Children.Add(soundspeed);

            TextBox soundspeedBox = new TextBox
            {
                FontSize = 10,
                Text = wall.WallMaterial.SoundSpeed + "",
                Width = rightPanelWidth,
                TextAlignment = TextAlignment.Left
            };
            PropsPanel.Children.Add(soundspeedBox);

            TextBlock location = new TextBlock { FontSize = 14, Text = "Location", Width = rightPanelWidth, TextAlignment = TextAlignment.Center };
            PropsPanel.Children.Add(location);
            TextBlock beginning = new TextBlock { FontSize = 10, Text = "First point", Width = rightPanelWidth, TextAlignment = TextAlignment.Left };
            PropsPanel.Children.Add(beginning);
            StackPanel firstPanel = new StackPanel{Orientation = Orientation.Horizontal, Width = rightPanelWidth};
            TextBlock xbegblock = new TextBlock{FontSize = 10, Text = "X: ",Margin = new Thickness(5,5,5,0)};
            firstPanel.Children.Add(xbegblock);
            TextBox xbegbox = new TextBox{ FontSize = 10, Text = wall.Start.X+"",Width=numFieldWidth,TextAlignment = TextAlignment.Left};
            firstPanel.Children.Add(xbegbox);
            TextBlock ybegblock = new TextBlock { FontSize = 10, Text = "Y: ", Margin = new Thickness(5, 5, 5, 0) };
            firstPanel.Children.Add(ybegblock);
            TextBox ybegbox = new TextBox { FontSize = 10, Text = wall.Start.Y + "", Width = numFieldWidth, TextAlignment = TextAlignment.Left };
            firstPanel.Children.Add(ybegbox);
            PropsPanel.Children.Add(firstPanel);

            TextBlock ending = new TextBlock { FontSize = 10, Text = "Second point", Width = rightPanelWidth, TextAlignment = TextAlignment.Left };
            PropsPanel.Children.Add(ending);
            StackPanel secondPanel = new StackPanel { Orientation = Orientation.Horizontal, Width = rightPanelWidth };
            TextBlock xendblock = new TextBlock { FontSize = 10, Text = "X: ", Margin = new Thickness(5, 5, 5, 0) };
            secondPanel.Children.Add(xendblock);
            TextBox xendbox = new TextBox { FontSize = 10, Text = wall.End.X + "", Width = numFieldWidth, TextAlignment = TextAlignment.Left };
            secondPanel.Children.Add(xendbox);
            TextBlock yendblock = new TextBlock { FontSize = 10, Text = "Y: ", Margin = new Thickness(5, 5, 5, 0) };
            secondPanel.Children.Add(yendblock);
            TextBox yendbox = new TextBox{ FontSize = 10, Text = wall.End.Y + "", Width = numFieldWidth, TextAlignment = TextAlignment.Left };
            secondPanel.Children.Add(yendbox);
            PropsPanel.Children.Add(secondPanel);

            TextBlock filtering = new TextBlock
            {
                FontSize = 14,
                Text = "Level of filtering by frequency",
                Width = rightPanelWidth,
                TextAlignment = TextAlignment.Center
            };
            PropsPanel.Children.Add(filtering);
            StackPanel freqPanel = new StackPanel{Orientation = Orientation.Horizontal,Width = rightPanelWidth};
            TextBlock lowBlock = new TextBlock {FontSize = 10, Text = "LOW: ", Margin = new Thickness(5, 5, 5, 0)};
            TextBox lowBox = new TextBox{FontSize = 10,Text = wall.WallMaterial.Low+"",Width = numFieldWidth,TextAlignment = TextAlignment.Left};
            freqPanel.Children.Add(lowBlock);
            freqPanel.Children.Add(lowBox);

            TextBlock medBlock = new TextBlock { FontSize = 10, Text = "MED: ", Margin = new Thickness(5, 5, 5, 0) };
            TextBox medBox = new TextBox { FontSize = 10, Text = wall.WallMaterial.Medium + "", Width = numFieldWidth, TextAlignment = TextAlignment.Left };
            freqPanel.Children.Add(medBlock);
            freqPanel.Children.Add(medBox);

            TextBlock highBlock = new TextBlock { FontSize = 10, Text = "HIGH: ", Margin = new Thickness(5, 5, 5, 0) };
            TextBox highBox = new TextBox { FontSize = 10, Text = wall.WallMaterial.High + "", Width = numFieldWidth, TextAlignment = TextAlignment.Left };
            freqPanel.Children.Add(highBlock);
            freqPanel.Children.Add(highBox);

            PropsPanel.Children.Add(freqPanel);
            Button deleteButton = new Button{Content = "Delete wall", HorizontalAlignment = HorizontalAlignment.Center,Margin = new Thickness(10,10,10,10)};
            PropsPanel.Children.Add(deleteButton);
            deleteButton.Click += delegate
            {
                ((TextBlock) PropsPanel.Children[0]).Text = "Properties";
                PropsPanel.Children.RemoveRange(1, PropsPanel.Children.Count - 1);
                mRoom.RemoveWall(mSelectedRoomObject as Wall);
                mSelectedRoomObject = null;
                DrawRoom();
            };
            materialBox.SelectionChanged += delegate
            {
                for (int i = 0; i < mRoom.Walls.Count; i++)
                {

                    if (mRoom.Walls[i] == wall)
                    {
                        if ((Wall.MaterialPreset) materialBox.SelectedItem == Wall.MaterialPreset.None)
                        {
                            mRoom.Walls[i].MatPreset = Wall.MaterialPreset.None;
                            return;
                        }
                        mRoom.Walls[i] = new Wall(wall.Start, wall.End,
                            (Wall.MaterialPreset) materialBox.Items[materialBox.SelectedIndex]);
                        mSelectedRoomObject = mRoom.Walls[i];
                        UpdateWallProps(mSelectedRoomObject as Wall);
                    }
                }
            };

            densityBox.TextChanged += delegate
            {
                double newValue;
                if (!double.TryParse(densityBox.Text, out newValue) || newValue<0) return;
                wall.WallMaterial.Density = newValue;
                wall.MatPreset = Wall.MaterialPreset.None;
                materialBox.SelectedItem = Wall.MaterialPreset.None;
            };
            densityBox.LostFocus += delegate
            {
                var wall1 = mSelectedRoomObject as Wall;
                if (wall1 != null)
                    densityBox.Text = wall1.WallMaterial.Density+"";
            };
            soundspeedBox.TextChanged += delegate
            {
                double newValue;
                if (!double.TryParse(soundspeedBox.Text, out newValue) || newValue<0) return;
                wall.WallMaterial.SoundSpeed = newValue;
                wall.MatPreset = Wall.MaterialPreset.None;
                materialBox.SelectedItem = Wall.MaterialPreset.None;
            };
            soundspeedBox.LostFocus += delegate
            {
                var wall1 = mSelectedRoomObject as Wall;
                if (wall1 != null)
                    soundspeedBox.Text = wall1.WallMaterial.SoundSpeed + "";
            };

            lowBox.TextChanged += delegate
            {
                double newValue;
                if (!double.TryParse(lowBox.Text, out newValue) || newValue > 1 || newValue < 0) return;
                wall.WallMaterial.Low = newValue;
                wall.MatPreset = Wall.MaterialPreset.None;
                materialBox.SelectedItem = Wall.MaterialPreset.None;
            };
            lowBox.LostFocus += delegate
            {
                var wall1 = mSelectedRoomObject as Wall;
                if (wall1 != null)
                    lowBox.Text = wall1.WallMaterial.Low + "";
            };


            medBox.TextChanged += delegate
            {
                double newValue;
                if (!double.TryParse(medBox.Text, out newValue) || newValue > 1 || newValue < 0) return;
                wall.WallMaterial.Medium = newValue;
                wall.MatPreset = Wall.MaterialPreset.None;
                materialBox.SelectedItem = Wall.MaterialPreset.None;

            };
            medBox.LostFocus += delegate
            {
                var wall1 = mSelectedRoomObject as Wall;
                if (wall1 != null)
                    medBox.Text = wall1.WallMaterial.Medium + "";
            };


            highBox.TextChanged += delegate
            {
                double newValue;
                if (!double.TryParse(highBox.Text, out newValue) || newValue > 1 || newValue < 0) return;
                wall.WallMaterial.High = newValue;
                wall.MatPreset = Wall.MaterialPreset.None;
                materialBox.SelectedItem = Wall.MaterialPreset.None;

            };
            highBox.LostFocus += delegate
            {
                var wall1 = mSelectedRoomObject as Wall;
                if (wall1 != null)
                    highBox.Text = wall1.WallMaterial.High + "";
            };
            xbegbox.TextChanged += delegate
            {
                double newValue;
                if (!double.TryParse(xbegbox.Text, out newValue)) return;
                ((Wall) mSelectedRoomObject).Start.X = newValue;
                DrawRoom();

            };
            xbegbox.LostFocus += delegate
            {
                var wall1 = mSelectedRoomObject as Wall;
                if (wall1 != null) xbegbox.Text = wall1.Start.X+"";
            };
            ybegbox.TextChanged += delegate
            {
                double newValue;
                if (!double.TryParse(ybegbox.Text, out newValue)) return;
                ((Wall) mSelectedRoomObject).Start.Y = newValue;
                DrawRoom();

            };
            ybegbox.LostFocus += delegate
            {
                var wall1 = mSelectedRoomObject as Wall;
                if (wall1 != null) ybegbox.Text = wall1.Start.Y + "";
            };
            xendbox.TextChanged += delegate
            {
                double newValue;
                if (!double.TryParse(xendbox.Text, out newValue)) return;
                ((Wall) mSelectedRoomObject).End.X = newValue;
                DrawRoom();

            };
            xendbox.LostFocus += delegate
            {
                var wall1 = mSelectedRoomObject as Wall;
                if (wall1 != null) xendbox.Text = wall1.End.X + "";
            };
            yendbox.TextChanged += delegate
            {
                double newValue;
                if (!double.TryParse(yendbox.Text, out newValue)) return;
                ((Wall) mSelectedRoomObject).End.Y = newValue;
                DrawRoom();

            };
            yendbox.LostFocus += delegate
            {
                var wall1 = mSelectedRoomObject as Wall;
                if (wall1 != null) yendbox.Text = wall1.End.Y + "";
            };
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SoundProgressBar.Width = Width / 2-20;
        }

        private void ApplyRoomPresetButton_Click(object sender, RoutedEventArgs e)
        {
            Room.RoomPreset preset;
            if (!Enum.TryParse(RoomPresetBox.SelectedItem.ToString(), out preset)) return;
            mRoom = Room.CreatePresetRoom(preset);
            ((TextBlock)PropsPanel.Children[0]).Text = "Properties";
            PropsPanel.Children.RemoveRange(1, PropsPanel.Children.Count - 1);
            mSelectedRoomObject = null;
            mMarkers.Clear();
            foreach (MouseButtonEventHandler handler in mCanvasMousehandlers)
            {
                RoomCanvas.MouseUp -= handler;
            }
            RoomCanvas.MouseUp -= RoomCanvas_MouseUp;
            RoomCanvas.MouseUp += RoomCanvas_MouseUp;
            DrawRoom();
        }
    }
}
