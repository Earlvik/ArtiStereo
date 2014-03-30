using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Shapes = System.Windows.Shapes;
using Microsoft.Win32;
using System.Windows.Controls.Primitives;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;


namespace Earlvik.ArtiStereo

{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const int PixelPerMeter = 40;
        const int BUTTON_NUMBER=4;
        private int chosenButton = -1;
        private Sound BaseSound;
        private Sound ResultSound;
        List<UIElement> Markers = new List<UIElement>();
        List<MouseButtonEventHandler> CanvasMousehandlers = new List<MouseButtonEventHandler>();
        List<IRoomObject> RedoElements = new List<IRoomObject>();
        List<IRoomObject> AddedElements = new List<IRoomObject>();
        Room Room;
        ToggleButton[] toolButtons = new ToggleButton[BUTTON_NUMBER];
        public MainWindow()
        {
            InitializeComponent();
            toolButtons[0] = RectButton;
            toolButtons[1] = LineButton;
            toolButtons[2] = SourceButton;
            toolButtons[3] = ListenerButton;
            foreach (ToggleButton button in toolButtons)
            {
                button.Checked += ToolButtonToggled;
                button.Unchecked += (sender, args) => { 
                    chosenButton = -1;
                    Markers.Clear();
                    DrawRoom();
                    foreach (MouseButtonEventHandler handler in CanvasMousehandlers)
                    {
                        RoomCanvas.MouseUp -= handler;
                    }
                    RoomCanvas.MouseUp -= RoomCanvas_MouseUp;
                    RoomCanvas.MouseUp += RoomCanvas_MouseUp;
                };
            }
            Room = new Room();
            //--------------------TESTING---TESTING---TESTING----------------
            Room.AddWall(new Wall(2,2,7,2,Wall.MaterialPreset.Glass));
            Room.AddWall(new Wall(2,4,7,4,Wall.MaterialPreset.Brick));
            Room.AddListener(new ListenerPoint(6,6,new Line(0,0,1,-1),ListenerPoint.Cardioid ));
            Room.AddSource(new SoundPoint(10,10));


            
            //---------------------------------------------------------------
            Loaded += delegate
            {
                DrawRoom();
                
            };
            

        }

        private void DrawRoom()
        {
            RoomCanvas.Children.Clear();

            //==================Grid=============================
            double dx = 20;
            double dy = 20;
            int i = 0;
            while(i*dy<RoomCanvas.ActualHeight-20){
                i++;
                Shapes.Line lineH = new Shapes.Line();
                lineH.StrokeThickness = 0.1;
                lineH.Stroke = System.Windows.Media.Brushes.Black;
                lineH.X1 = 0;
                lineH.Y1 = i * dy;
                lineH.X2 = RoomCanvas.ActualWidth;
                lineH.Y2 = i * dy;
                lineH.Visibility = Visibility.Visible;
                RoomCanvas.Children.Add(lineH);
            }
            i = 0;
            while(i*dx<RoomCanvas.ActualWidth-20){
                i++;
                Shapes.Line lineV = new Shapes.Line();
                lineV.StrokeThickness = 0.1;
                lineV.Stroke = System.Windows.Media.Brushes.Black;
                lineV.X1 = i*dx;
                lineV.Y1 = 0;
                lineV.X2 = i*dx;
                lineV.Y2 = RoomCanvas.ActualHeight;
                lineV.Visibility = System.Windows.Visibility.Visible;

                RoomCanvas.Children.Add(lineV);
                
            }

            //=========================WALLS========================

            foreach (Wall wall in Room.Walls)
            {
                Shapes.Line line = new Shapes.Line
                {
                    X1 = PixelPerMeter*wall.Start.X,
                    X2 = PixelPerMeter*wall.End.X,
                    Y1 = PixelPerMeter*wall.Start.Y,
                    Y2 = PixelPerMeter*wall.End.Y,
                    StrokeThickness = 3
                };
                switch (wall.MatPreset)
                {
                    case Wall.MaterialPreset.Brick:
                        {
                            line.Stroke = System.Windows.Media.Brushes.Orange;
                            break;
                        }
                    case Wall.MaterialPreset.Glass:
                        {
                            line.Stroke = System.Windows.Media.Brushes.CornflowerBlue;
                            break;
                        }
                    case Wall.MaterialPreset.Granite:
                        {
                            line.Stroke = System.Windows.Media.Brushes.LightSlateGray;
                            break;
                        }
                    case Wall.MaterialPreset.OakWood:
                    {
                        line.Stroke = System.Windows.Media.Brushes.Chocolate;
                        break;
                    }
                    case Wall.MaterialPreset.Rubber:
                    {
                        line.Stroke = System.Windows.Media.Brushes.DarkSalmon;
                        break;
                    }
                default:
                    {
                        line.Stroke = System.Windows.Media.Brushes.Goldenrod;
                        break;
                    }
                }
                line.Visibility = Visibility.Visible;
                RoomCanvas.Children.Add(line);
            }

            //===============================SOUND POINTS=======================
            double picSize = 20;
            foreach (ListenerPoint listener in Room.Listeners)
            {
                
                var image = new System.Windows.Controls.Image
                {
                    Source = new BitmapImage(new Uri("pack://application:,,,/Images/micPict.png")),
                    Width = picSize,
                    Height = picSize
                };
                double X = listener.X*PixelPerMeter-(picSize/2);
                double Y = listener.Y*PixelPerMeter-(picSize/2);
                Canvas.SetTop(image,Y);
                Canvas.SetLeft(image,X);
                
                RoomCanvas.Children.Add(image);

                if (listener.Directional)
                {
                    double angle = listener.DirectionAngle;
                    Shapes.Line line = new Shapes.Line();
                    line.X1 = X + (picSize/2);
                    line.Y1 = Y + (picSize/2);
                    line.X2 = X+(picSize/2) + Math.Cos(angle) * (picSize/2);
                    line.Y2 = Y+(picSize/2) + Math.Sin(angle) * (picSize/2);
                    line.Stroke = System.Windows.Media.Brushes.Red;
                    line.StrokeThickness = 1;
                    RoomCanvas.Children.Add(line);
                }
                
            }

            foreach (SoundPoint source in Room.Sources)
            {
                var image = new System.Windows.Controls.Image()
                {
                    Source = new BitmapImage(new Uri("pack://application:,,,/Images/notePict.png")),
                    Width = picSize,
                    Height = picSize
                };
                double X = source.X * PixelPerMeter - (picSize / 2);
                double Y = source.Y * PixelPerMeter - (picSize / 2);
                Canvas.SetTop(image, Y);
                Canvas.SetLeft(image, X);

                RoomCanvas.Children.Add(image);
            }
            //===================MARKERS==============
            foreach (UIElement marker in Markers)
            {
                RoomCanvas.Children.Add(marker);
            }
        }

        private void SoundOpenMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog {CheckPathExists = true, CheckFileExists = true, Filter = "Wave Sound Files (*.wav)|*.wav"};
            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                BaseSound = Sound.GetSoundFromWav(dialog.FileName);
                MessageBox.Show("Successfully opened " + dialog.FileName,"Sound Open",MessageBoxButton.OK);
            }
        }

        private void ToolButtonToggled(object sender, RoutedEventArgs e)
        {
            int chosen = -1;
            Markers.Clear();
            DrawRoom();
            foreach (MouseButtonEventHandler handler in CanvasMousehandlers)
            {
                RoomCanvas.MouseUp -= handler;
            }
            RoomCanvas.MouseUp -= RoomCanvas_MouseUp;
            RoomCanvas.MouseUp+=RoomCanvas_MouseUp;
            for (int i = 0; i < BUTTON_NUMBER; i++)
            {
                
                if (toolButtons[i] == sender)
                {
                    chosen = i;
                }
                else
                {
                    toolButtons[i].IsChecked = false;
                }
                chosenButton = chosen;
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SoundProgressBar.Width = this.Width / 2-20;
        }

        private void RoomCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawRoom();
        }

        private void RoomCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            switch (chosenButton)
            {
                case 0:
                {
                    System.Windows.Point position = e.GetPosition(RoomCanvas);
                    Shapes.Ellipse first = new Shapes.Ellipse {Fill = Brushes.Black, Stroke = Brushes.Black};
                    first.Width = first.Height = 5;
                    Canvas.SetLeft(first,position.X);
                    Canvas.SetTop(first,position.Y);
                    first.Visibility = Visibility.Visible;
                    Markers.Add(first);
                    Point LeftTop = new Point(position.X/PixelPerMeter, position.Y/PixelPerMeter);
                    RoomCanvas.MouseUp -= RoomCanvas_MouseUp;
                    MouseButtonEventHandler roomCanvasRectDraw = null;
                    roomCanvasRectDraw=(sndr, earg) =>
                    {
                        Markers.Remove(first);
                        RoomCanvas.MouseUp-=roomCanvasRectDraw;
                        RoomCanvas.MouseUp += RoomCanvas_MouseUp;
                        position = e.GetPosition(RoomCanvas);
                        Point BottomRight = new Point(position.X/PixelPerMeter, position.Y/PixelPerMeter);
                        if (Geometry.Distance(LeftTop, BottomRight) < 0.25)
                        {
                            Markers.Clear();
                            DrawRoom();
                            return;
                        }
                        Point TopRight = new Point(BottomRight.X,LeftTop.Y);
                        Point BottomLeft = new Point(LeftTop.X,BottomRight.Y);
                        AddedElements.Add(Room.AddWall(new Wall(LeftTop,TopRight,Wall.MaterialPreset.OakWood)));
                        AddedElements.Add(Room.AddWall(new Wall(TopRight, BottomRight, Wall.MaterialPreset.OakWood)));
                        AddedElements.Add(Room.AddWall(new Wall(BottomRight, BottomLeft, Wall.MaterialPreset.OakWood)));
                        AddedElements.Add(Room.AddWall(new Wall(LeftTop, BottomLeft, Wall.MaterialPreset.OakWood)));
                        UndoMenuItem.IsEnabled = true;
                        DrawRoom();
                        
                    };
                    CanvasMousehandlers.Add(roomCanvasRectDraw);
                    RoomCanvas.MouseUp += roomCanvasRectDraw;
                    break;
                }
                case 1:
                {
                    break;
                }
                case 2:
                {
                    break;
                }
                case 3:
                {
                    break;
                }
                default:
                {
                    break;
                }
            }
            DrawRoom();
        }

        private void UndoMenuItem_Click(object sender, RoutedEventArgs e)
        {
            IRoomObject lastObject = AddedElements.Last();
            RedoElements.Add(lastObject);
            AddedElements.Remove(lastObject);
            if (lastObject is Wall)
            {
                Room.RemoveWall(lastObject as Wall);
            }
            else if (lastObject is ListenerPoint)
            {
                Room.RemoveListener(lastObject as ListenerPoint);
            }
            else
            {
                Room.RemoveSource(lastObject as SoundPoint);
            }
            if (AddedElements.Count == 0) UndoMenuItem.IsEnabled = false;
            RedoMenuItem.IsEnabled = true;
            DrawRoom();
        }

        private void RedoMenuItem_Click(object sender, RoutedEventArgs e)
        {
            IRoomObject lastObject = RedoElements.Last();
            AddedElements.Add(lastObject);
            RedoElements.Remove(lastObject);
            if (lastObject is Wall)
            {
                Room.AddWall(lastObject as Wall);
            }
            else if (lastObject is ListenerPoint)
            {
                Room.AddListener(lastObject as ListenerPoint);
            }
            else
            {
                Room.AddSource(lastObject as SoundPoint);
            }
            if (RedoElements.Count == 0) RedoMenuItem.IsEnabled = false;
            UndoMenuItem.IsEnabled = true;
            DrawRoom();
        }

        private void ClearMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Room = new Room();
            Markers.Clear();
            DrawRoom();
        }

        
       
    }
}
