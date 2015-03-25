using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using Microsoft.Win32;
using System.Diagnostics;

namespace KML2SQL
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        MapUploader myUploader;

        readonly string _appFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) 
            + ConfigurationManager.AppSettings["AppFolder"];

        public MainWindow()
        {
            InitializeComponent();
            if (!Directory.Exists(_appFolder))
                Directory.CreateDirectory(_appFolder);
            RestoreSettings();
        }

        private void myUploader_progressUpdate(string text)
        {
            resultTextBox.Text = text;
        }

        private void serverNameBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (serverNameBox.Text == "foo.myserver.com")
                serverNameBox.Clear();
        }

        private void KMLFileLocationBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (KMLFileLocationBox.Text == "C:\\...")
                KMLFileLocationBox.Clear();
        }

        private void userNameBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (userNameBox.Text == "username")
                userNameBox.Clear();
        }

        private void passwordBox_GotFocus(object sender, RoutedEventArgs e)
        {
            passwordBox.Clear();
        }

        private void tableBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (tableBox.Text == "myTable")
                tableBox.Clear();
        }

        private void columnNameBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (columnNameBox.Text == "polygon")
                columnNameBox.Clear();
        }

        private void sridCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            sridBox.IsEnabled = true;
        }

        private void sridCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            sridBox.Text = "4326";
            sridBox.IsEnabled = false;
        }

        private void CreateDatabaseButton_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
            bool geography;
            if (geographyMode.IsChecked != null)
                geography = (bool)geographyMode.IsChecked;
            else
                geography = false;
            int srid = ParseSRID(geography);
            if (srid != 0)
            {
                try
                {
                    myUploader = new MapUploader(BuildConnectionString());
                    Binding b = new Binding();
                    b.Source = myUploader;
                    b.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                    b.Path = new PropertyPath("Progress");
                    resultTextBox.SetBinding(TextBlock.TextProperty, b);
                    myUploader.Upload(columnNameBox.Text, KMLFileLocationBox.Text, tableBox.Text, srid, geography);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("The process failed with the following error. See the log for details: \r\n\r\n " 
                        + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveSettings()
        {
            var settings = new Settings();
            settings.DatabaseName = databaseNameBox.Text;
            settings.ServerName = serverNameBox.Text;
            settings.KMLFileName = KMLFileLocationBox.Text;
            settings.TableName = tableBox.Text;
            settings.ShapeColumnName = columnNameBox.Text;
            settings.Login = userNameBox.Text;
            settings.SRID = sridBox.Text;
            settings.SRIDEnabled = sridCheckBox.IsChecked.Value;
            settings.Geography = geographyMode.IsChecked.Value;
            settings.UseIntegratedSecurity = integratedSecurityCheckbox.IsChecked.Value;
            new SettingsPersister().Persist(settings);
        }
        private void RestoreSettings()
        {
            var settings = new SettingsPersister().Retrieve();
            if (settings != null)
            {
                geographyMode.IsChecked = settings.Geography;
                sridCheckBox.IsChecked = settings.SRIDEnabled;
                sridBox.Text = settings.SRID;
                userNameBox.Text = settings.Login;
                columnNameBox.Text = settings.ShapeColumnName;
                tableBox.Text = settings.TableName;
                KMLFileLocationBox.Text = settings.KMLFileName;
                serverNameBox.Text = settings.ServerName;
                databaseNameBox.Text = settings.DatabaseName;
                integratedSecurityCheckbox.IsChecked = settings.UseIntegratedSecurity;
            }
        }

        private string BuildConnectionString()
        {
            string connString = "Data Source=" + serverNameBox.Text + ";Initial Catalog=" + databaseNameBox.Text + ";Persist Security Info=True;";
            if (integratedSecurityCheckbox.IsEnabled)
                connString += "Integrated Security = SSPI;";
            else
                connString += "User ID=" + userNameBox.Text + ";Password=" + passwordBox.Password;
            return connString;
        }

        private int ParseSRID(bool geographyMode)
        {
            if (!geographyMode)
            {
                return 4326;
            }
            else
            {
                int srid;
                if (int.TryParse(sridBox.Text, out srid))
                    return srid;
                else
                    MessageBox.Show("SRID must be a valid four digit number");
                return srid;
            }
        }

        private void databaseNameBox_GotFocus(object sender, RoutedEventArgs e)
        {

            if (databaseNameBox.Text == "myDatabase")
                databaseNameBox.Clear();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog myOpenFileDialog = new OpenFileDialog();
            myOpenFileDialog.InitialDirectory = Environment.SpecialFolder.MyDocuments.ToString();
            myOpenFileDialog.Filter = "KML Files (*.kml|*.kml|All Files (*.*)|*.*";
            myOpenFileDialog.FileName = "myFile.kml";
            Nullable<bool> result = myOpenFileDialog.ShowDialog();
            if (result == true)
            {
                try
                {
                    KMLFileLocationBox.Text = myOpenFileDialog.FileName;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("An error occured while opening the file" + myOpenFileDialog.FileName + "\n" + ex.Message, "Unable to open KML file.");
                }
            }
        }

        private void geometryMode_Checked(object sender, RoutedEventArgs e)
        {
            if (sridCheckBox != null)
                sridCheckBox.IsEnabled = false;
            if (sridBox != null)
                sridBox.Text = "NA";
        }

        private void geographyMode_Checked(object sender, RoutedEventArgs e)
        {
            if (sridCheckBox != null)
                sridCheckBox.IsEnabled = true;
            if (sridBox != null)
                sridBox.Text = "4326";
        }

        private void About_MouseEnter(object sender, MouseEventArgs e)
        {
            About.Opacity = 1;
        }

        private void About_MouseLeave(object sender, MouseEventArgs e)
        {
            About.Opacity = .25;
        }

        private void About_MouseDown(object sender, MouseButtonEventArgs e)
        {
            About about = new About();
            about.Show();
        }


        private void Log_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(_appFolder);
        }

        private void IntegratedSecurityCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            if (integratedSecurityCheckbox.IsChecked.Value)
            {
                userNameBox.IsEnabled = false;
                passwordBox.IsEnabled = false;
            }
            else
            {
                userNameBox.IsEnabled = true;
                passwordBox.IsEnabled = true;
            }
        }
    }
}
