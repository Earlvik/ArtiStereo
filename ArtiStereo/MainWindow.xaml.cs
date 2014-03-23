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
using System.Windows.Shapes;
using Microsoft.Win32;


namespace Earlvik.ArtiStereo

{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Sound BaseSound;
        private Sound ResultSound;
        public MainWindow()
        {
            InitializeComponent();
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

       
    }
}
