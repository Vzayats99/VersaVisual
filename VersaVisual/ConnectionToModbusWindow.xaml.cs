using System;
using System.Collections.Generic;
using System.IO.Ports;
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
using System.Windows.Shapes;
using Modbus.Device;

namespace VersaVisual
{
    /// <summary>
    /// Логика взаимодействия для ConnectionToModbusWindow.xaml
    /// </summary>
    public partial class ConnectionToModbusWindow : Window
    {
        //COM порты
        public string[] nameWindow {  get; set; }
        //Скорость обмена
        public int[] baudRateWindow {  get; set; }
        //Дата биты
        public int[] dataBitsWindow {  get; set; }
        //Четность
        public string[] parityWindow { get; set; }
        //Стоп биты
        public int[] stopBitsWindow { get; set; }

        public ConnectionToModbusWindow()
        {
            InitializeComponent();

            //Заполнение массива именами COM портов (256)
            nameWindow = new string[256];

            nameWindow = SerialPort.GetPortNames();

            //Заполнение массива скоростями обмена
            baudRateWindow = new int[] { 300, 600, 1200, 2400, 4800, 9600, 14400, 19200, 38400, 56000, 
                                   57600, 115200, 128000, 153600, 230400, 256000, 460800, 921600};

            //Заполнение массива дата биты
            dataBitsWindow = new int[] { 7, 8 };

            //Заполнение массива четности
            parityWindow = new string[] {"Нет", "Четный", "Нечетный"};

            //Заполнение массива стоп битов
            stopBitsWindow = new int[] { 1, 2};

            DataContext = this;
        }

        private void CheckBoxPort_Checked(object sender, RoutedEventArgs e)
        {
            if(checkBoxPort.IsChecked == true)
            {
                textBlockNamePort.Text = "Tcp/Ip";

                comboBoxName.IsEnabled = false;
                comboBoxName.SelectedIndex = -1;

                comboBoxBaudRate.IsEnabled = false;
                comboBoxBaudRate.SelectedIndex = -1;

                comboBoxDataBits.IsEnabled = false;
                comboBoxDataBits.SelectedIndex = -1;

                comboBoxParity.IsEnabled = false;
                comboBoxParity.SelectedIndex = -1;

                comboBoxStopBits.IsEnabled = false;
                comboBoxStopBits.SelectedIndex = -1;
            }
            else if (checkBoxPort.IsChecked == false)
            {
                textBlockNamePort.Text = "SerialPort";

                comboBoxName.IsEnabled = true;
                comboBoxName.SelectedIndex = 0;

                comboBoxBaudRate.IsEnabled = true;
                comboBoxBaudRate.SelectedIndex = 7;

                comboBoxDataBits.IsEnabled = true;
                comboBoxDataBits.SelectedIndex = 1;

                comboBoxParity.IsEnabled = true;
                comboBoxParity.SelectedIndex = 1;

                comboBoxStopBits.IsEnabled = true;
                comboBoxStopBits.SelectedIndex = 0;
            }

        }

        private void Button_Connection_Click(object sender, RoutedEventArgs e)
        {
            Connection.Name = comboBoxName.Text;
            Connection.BaudRate = Convert.ToInt32(comboBoxBaudRate.Text);
            Connection.DataBits = Convert.ToInt32(comboBoxDataBits.Text);
            Connection.Parity = comboBoxParity.Text.Trim();
            Connection.StopBits = Convert.ToInt32(comboBoxStopBits.Text);

            Hide();
        }
    }
}
