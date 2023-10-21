using Microsoft.Win32;
using Modbus.Device;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
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
using System.Windows.Threading;
using System.Xml;
using System.Xml.Linq;
using static VersaVisual.MainWindow;

namespace VersaVisual
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        SaveFileDialog saveFileDialog = new SaveFileDialog(); //Объявление для работы с файлами (Сохранение)

        OpenFileDialog openFileDialog = new OpenFileDialog(); //Объявление для работы с файлами (Открывание)

        ModbusSerialMaster modbusSerialMaster;  //Объявление мастера для обмена по модбасу

        SerialPort serialPort;  //Объявление для создани порта

        int selectedDeviceId = 0; //Выбранный элемент в поле устройств

        public class Tags //Класс в котором объявляются все данные о тэгах
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public string Address { get; set; }
            public string Value { get; set; }
        }

        public class Devices //Класс в котором объявляются все данные об устройствах
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public string Series { get; set; } //тип устройства (Для версы Ф205 например)
            public string SlaveID { get; set; }

            public List<Tags> tagsList = new List<Tags>(); //Список в котором хранятся Тэги для каждого устройтва (обращение через ID устройства)
        }
        public List<Devices> devicesList = new List<Devices>(); //Список в котором хранятся устройства

        List<string> recentsList = new List<string>();

        DispatcherTimer timerPoll;

        public MainWindow()
        {
            InitializeComponent();

            saveFileDialog.Filter = "Текстовый файл (*txt) |*.txt| XML-файл (*xml) |*.xml";
            saveFileDialog.FileOk += SaveFileDialog_FileOk;

            openFileDialog.Filter = "Тескстовый файл (*txt) |*.txt| XML-файл (*xml) |*.xml";
            openFileDialog.FileOk += OpenFileDialog_FileOk;

            string strRecent = Properties.Settings.Default.Recents;
            string[] recents = strRecent.Split(';');
            if(recents.Length > 1)
            {
                foreach(string rec in recents)
                {
                    if (rec.Trim() != "")
                    {
                        recentsList.Add(rec);

                        MenuItem mnu = new MenuItem();
                        mnu.Header = rec;
                        mnu.Click += MnuRecent_Click; ;
                        mnuRecents.Items.Insert(mnuRecents.Items.Count - 2, mnu);
                    }
                }
            }

            timerPoll = new DispatcherTimer();
            timerPoll.Interval = TimeSpan.FromSeconds(10);
            timerPoll.Tick += TimerPoll_Tick; ;

        }

        private void btnDevAdd_Click(object sender, RoutedEventArgs e) //Кнопка - добавить устройство
        {
            //Разблокирую поля для ввода данных устройства
            txtDevId.IsEnabled = true;
            txtDevName.IsEnabled = true;
            cboDevSeries.IsEnabled = true;
            txtDevSlaveId.IsEnabled = true;
            //Разблокирую окно с полями для добавления
            txtDevMode.Text = "Добавить";

            if(devicesList.Count != 0)
            {
                int max = 0;
                for (int i = 0; i < devicesList.Count; i++)
                {
                    if(max < devicesList[i].ID)
                        max = devicesList[i].ID;
                }
                max++;
                txtDevId.Text = max.ToString();
            }
            else
                txtDevId.Text = devicesList.Count.ToString();

            txtDevName.Text = "";
            grdDevEdit.Visibility = Visibility.Visible;
        }

        private void btnDevEdit_Click(object sender, RoutedEventArgs e) //Кнопка - Изменить данные устройства
        {
            //Разблокирую поля для ввода данных устройства
            txtDevId.IsEnabled = true;
            txtDevName.IsEnabled = true;
            cboDevSeries.IsEnabled = true;
            txtDevSlaveId.IsEnabled = true;
            //Индекс выбранного элемента
            int index = dgDevices.SelectedIndex;
            //Получить элементы по индексу
            if (index >= 0 && devicesList.Count >= index)
            {
                txtDevMode.Text = "Изменить";
                txtDevId.Text = devicesList[index].ID.ToString();
                txtDevName.Text = devicesList[index].Name;
                cboDevSeries.Text = devicesList[index].Series;
                txtDevSlaveId.Text = devicesList[index].SlaveID;
                //Разблокирую окно с полями для изменения
                grdDevEdit.Visibility = Visibility.Visible;
            }
        }

        private void btnDevDelete_Click(object sender, RoutedEventArgs e) //Кнопка - Удалить устройство
        {
            //Вызываю метод кнопки изменения для получения данных выбранного поля
            btnDevEdit_Click(null, null);
            //Заблокирую поля для ввода(изменения) данных устройства
            txtDevId.IsEnabled = false;
            txtDevName.IsEnabled = false;
            cboDevSeries.IsEnabled = false;
            txtDevSlaveId.IsEnabled = false;
            //Изменяю вид Действия
            txtDevMode.Text = "Удалить";
        }

        private void dgDevices_SelectionChanged(object sender, SelectionChangedEventArgs e) //Метод проверки изменения в окне Устройств
        {
            //Скрываю окно с полями для изменения Устройств
            grdDevEdit.Visibility = Visibility.Collapsed;
            //Скрываю окно с полями для изменения Тэгов
            grdTagEdit.Visibility = Visibility.Collapsed;
            //Выбранный элемент в окне устройств
            int index = dgDevices.SelectedIndex;
            //Если идекс выбран разблокирую кнопки изменения удаления устройста и кнопку добавления тэгов
            if(index >= 0)
            {
                btnDevEdit.IsEnabled = true; 
                btnDevDelete.IsEnabled = true;
                btnTagAdd.IsEnabled = true;

                dgTags.ItemsSource = null;
                dgTags.ItemsSource = devicesList[index].tagsList;
                selectedDeviceId = index;
            }
            else
            {
                btnDevEdit.IsEnabled = false;
                btnDevDelete.IsEnabled = false;
                btnTagAdd.IsEnabled = false;

                dgTags.ItemsSource = null;
                selectedDeviceId = 0;
            }
        }

        private void btnDevOk_Click(object sender, RoutedEventArgs e) //Кнопка-подтвердить добавление изменение или удаление устройства
        {
            if(txtDevMode.Text == "Добавить")
            {
                Devices devices = new Devices();
                devices.ID = int.Parse(txtDevId.Text);
                devices.Name = txtDevName.Text;
                devices.Series = cboDevSeries.Text;
                devices.SlaveID = txtDevSlaveId.Text;
                devicesList.Add(devices);
            }
            else if(txtDevMode.Text == "Изменить")
            {
                int index = int.Parse(txtDevId.Text);
                devicesList[index].Name = txtDevName.Text;
                devicesList[index].Series = cboDevSeries.Text;
                devicesList[index].SlaveID = txtDevSlaveId.Text;

            }
            else if(txtDevMode.Text == "Удалить")
            {
                int index = dgDevices.SelectedIndex;
                devicesList.RemoveAt(index);
            }    

            dgDevices.ItemsSource = null;
            dgDevices.ItemsSource = devicesList;

            grdDevEdit.Visibility = Visibility.Collapsed;
        }

        private void btnDevCancel_Click(object sender, RoutedEventArgs e)
        {
            grdDevEdit.Visibility = Visibility.Collapsed;
        }

        private void btnTagAdd_Click(object sender, RoutedEventArgs e)
        {
            txtTagId.IsEnabled = true;
            txtTagName.IsEnabled = true;
            txtTagAddress.IsEnabled = true;

            int indexDevId = selectedDeviceId;

            txtTagMode.Text = "Добавить";

            if (devicesList[indexDevId].tagsList.Count != 0)
            {
                int max = 0;
                for (int i = 0; i < devicesList[indexDevId].tagsList.Count; i++)
                {
                    if (max < devicesList[indexDevId].tagsList[i].ID)
                        max = devicesList[indexDevId].tagsList[i].ID;
                }
                max++;
                txtTagId.Text = max.ToString();
            }
            else
                txtTagId.Text = devicesList[indexDevId].tagsList.Count.ToString();

            txtTagName.Text = "";

            grdTagEdit.Visibility = Visibility.Visible;
        }

        private void btnTagEdit_Click(object sender, RoutedEventArgs e)
        {
            txtTagId.IsEnabled = true;
            txtTagName.IsEnabled = true;
            txtTagAddress.IsEnabled = true;

            int indexDevId = selectedDeviceId;

            int index = dgTags.SelectedIndex;

            if (index >= 0 && devicesList[indexDevId].tagsList.Count >= index)
            {
                txtTagMode.Text = "Изменить";
                txtTagId.Text = devicesList[indexDevId].tagsList[index].ID.ToString();
                txtTagName.Text = devicesList[indexDevId].tagsList[index].Name;
                txtTagAddress.Text = devicesList[indexDevId].tagsList[index].Address;

                grdTagEdit.Visibility = Visibility.Visible;
            }
        }

        private void btnTagDelete_Click(object sender, RoutedEventArgs e)
        {
            btnTagEdit_Click(null, null);

            txtTagId.IsEnabled = false;
            txtTagName.IsEnabled = false; 
            txtTagAddress.IsEnabled = false;

            txtTagMode.Text = "Удалить";
        }

        private void dgTags_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Скрываю окно с полями для изменения Тэгов
            grdTagEdit.Visibility = Visibility.Collapsed;

            int index = dgTags.SelectedIndex;

            if (index >= 0)
            {
                btnTagEdit.IsEnabled = true;
                btnTagDelete.IsEnabled = true;
            }
            else
            {
                btnTagEdit.IsEnabled = false;
                btnTagDelete.IsEnabled = false;
            }
        }

        private void btnTagOk_Click(object sender, RoutedEventArgs e)
        {
            int indexDevId = selectedDeviceId;

            if (txtTagMode.Text == "Добавить")
            {
                Tags tags = new Tags();
                tags.ID = int.Parse(txtTagId.Text);
                tags.Name = txtTagName.Text;
                tags.Address = txtTagAddress.Text;
                tags.Value = "";
                devicesList[indexDevId].tagsList.Add(tags);
            }
            else if (txtTagMode.Text == "Изменить")
            {
                int index = int.Parse(txtTagId.Text);
                devicesList[indexDevId].tagsList[index].Name = txtTagName.Text;
                devicesList[indexDevId].tagsList[index].Address = txtTagAddress.Text;
            }
            else if (txtTagMode.Text == "Удалить")
            {
                int index = dgTags.SelectedIndex;
                devicesList[indexDevId].tagsList.RemoveAt(index);
            }

            dgTags.ItemsSource = null;
            dgTags.ItemsSource = devicesList[indexDevId].tagsList;

            grdTagEdit.Visibility = Visibility.Collapsed;
        }

        private void btnTagCancel_Click(object sender, RoutedEventArgs e)
        {
            grdTagEdit.Visibility = Visibility.Collapsed;
        }

        private void mnuNew_Click(object sender, RoutedEventArgs e)
        {
            devicesList.Clear();
            dgDevices.ItemsSource = null;
            txtFile.Text = "";
        }

        private void mnuOpen_Click(object sender, RoutedEventArgs e)
        {

            openFileDialog.ShowDialog();
        }

        private void OpenFileDialog_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            OpenFile(openFileDialog.FileName);
        }

        private void OpenFile(string fileName)
        {
            try
            {
                devicesList.Clear();
                int id = 0;

                char[] chars = fileName.ToCharArray();
                Array.Reverse(chars);

                string str = "";

                for (int i = 0; i < chars.Length; i++)
                {
                    if (chars[i] != '.')
                    {
                        str += chars[i];
                    }
                    else break;
                }

                switch (str)
                {
                    //Открытие в формате *txt
                    case "txt":
                        using (var reader = new StreamReader(fileName))
                        {
                            while (!reader.EndOfStream)
                            {
                                string sLine = reader.ReadLine();
                                string[] aLine = sLine.Split('=');
                                if (aLine.Length > 1)
                                {
                                    string sKey = aLine[0];
                                    string sValue = aLine[1];
                                    if (sKey == "Device")
                                    {
                                        string[] aValue = sValue.Split(';');
                                        if (aValue.Length > 1)
                                        {
                                            for (int i = 0; i < aValue.Length - 1; i++)
                                            {
                                                string[] sDev = aValue[i].Split(',');

                                                if (i == 0)
                                                {
                                                    Devices devices = new Devices();
                                                    devices.ID = int.Parse(sDev[0]);
                                                    devices.Name = sDev[1];
                                                    devices.Series = sDev[2];
                                                    devices.SlaveID = sDev[3];
                                                    devicesList.Add(devices);
                                                }
                                                else
                                                {
                                                    Tags tags = new Tags();
                                                    tags.ID = int.Parse(sDev[0]);
                                                    tags.Name = sDev[1];
                                                    tags.Address = sDev[2];
                                                    tags.Value = "";
                                                    devicesList[id].tagsList.Add(tags);
                                                }
                                            }
                                        }
                                    }
                                }
                                id++;
                            }
                        }
                        dgDevices.ItemsSource = null;
                        dgDevices.ItemsSource = devicesList;
                        break;
                    //Открытие в формате *xml
                    case "xml":
                        XmlDocument xDoc = new XmlDocument();
                        xDoc.Load(fileName);
                        // получим корневой элемент
                        XmlElement xRoot = xDoc.DocumentElement;
                        if (xRoot != null)
                        {
                            // обход всех узлов в корневом элементе
                            foreach (XmlElement xnode in xRoot)
                            {
                                Devices devices = new Devices();
                                XmlNode attr = xnode.Attributes.GetNamedItem("DevId");
                                devices.ID = int.Parse(attr.Value);

                                foreach (XmlNode childnode in xnode.ChildNodes)
                                {
                                    if(childnode.Name == "DevName") 
                                        devices.Name = childnode.InnerText;
                                    if(childnode.Name == "DevSeries")
                                        devices.Series = childnode.InnerText;
                                    if (childnode.Name == "DevSlaveId")
                                        devices.SlaveID = childnode.InnerText;
                                }
                                devicesList.Add(devices);
                                // обход всех узлов в корневом элементе
                                foreach (XmlNode childnode in xnode.ChildNodes)
                                {
                                    if (childnode.Name == "Tag")
                                    {
                                        Tags tags = new Tags();
                                        attr = childnode.Attributes.GetNamedItem("TagId");
                                        tags.ID = int.Parse(attr.Value);
                                        foreach (XmlNode xmlNode in childnode.ChildNodes)
                                        {
                                            if (xmlNode.Name == "TagName")
                                                tags.Name = xmlNode.InnerText;
                                            if (xmlNode.Name == "TagAddress")
                                                tags.Address = xmlNode.InnerText;
                                        }
                                        devicesList[id].tagsList.Add(tags);
                                    }
                                }
                                id++;
                            }
                        }
                        dgDevices.ItemsSource = null;
                        dgDevices.ItemsSource = devicesList;
                        break;
                }
                txtFile.Text = fileName;

                AddRecent(fileName);
            }
            catch (Exception){ }
        }

        private void mnuSave_Click(object sender, RoutedEventArgs e)
        {
            if(txtFile.Text == "")
            {
                mnuSaveAs_Click(null, null);
            }
            else
            {
               SaveFile(txtFile.Text);
            }
        }

        private void mnuSaveAs_Click(object sender, RoutedEventArgs e)
        {
            saveFileDialog.ShowDialog();
        }

        private void SaveFileDialog_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveFile(saveFileDialog.FileName);
        }

        private void SaveFile(string fileName)
        {
            try
            {
                char[] chars = fileName.ToCharArray();
                Array.Reverse(chars);

                string str = "";

                for (int i = 0; i < chars.Length; i++)
                {
                    if (chars[i] != '.')
                    {
                        str += chars[i];
                    }
                    else break;
                }

                switch (str)
                {
                    //Сохранение в формате *txt
                    case "txt":
                        string sprint = "";

                        for (int i = 0; i < devicesList.Count; i++)
                        {
                            sprint += "Device=";
                            sprint += (devicesList[i].ID + "," + devicesList[i].Name + "," + devicesList[i].Series + "," + devicesList[i].SlaveID + ";");
                            for (int j = 0; j < devicesList[i].tagsList.Count; j++)
                            {
                                sprint += (devicesList[i].tagsList[j].ID + "," + devicesList[i].tagsList[j].Name + "," + devicesList[i].tagsList[j].Address + ";");
                            }
                            sprint += "\r\n";
                        }

                        TextWriter tw = new StreamWriter(fileName);
                        tw.WriteLine(sprint);
                        tw.Close();
                        break;
                    //Сохранение в формате *xml
                    case "xml":
                        XDocument xDocument = new XDocument();
                        XElement elDevices = new XElement("Devices");
                        XElement elDevice;
                        XAttribute elDeviceId;
                        XElement elDeviceName;
                        XElement elDeviceSeries;
                        XElement elDeviceSlaveId;
                        XElement elTag;
                        XAttribute elTagId;
                        XElement elTagName;
                        XElement elTagAddress;

                        for(int i = 0; i < devicesList.Count; i++)
                        {
                            elDevice = new XElement("Device");
                            elDeviceId = new XAttribute("DevId", devicesList[i].ID.ToString());
                            elDeviceName = new XElement("DevName", devicesList[i].Name);
                            elDeviceSeries = new XElement("DevSeries", devicesList[i].Series);
                            elDeviceSlaveId = new XElement("DevSlaveId", devicesList[i].SlaveID);
                            elDevice.Add(elDeviceId);
                            elDevice.Add(elDeviceName);
                            elDevice.Add(elDeviceSeries);
                            elDevice.Add(elDeviceSlaveId);

                            for (int j = 0; j < devicesList[i].tagsList.Count; j++)
                            {
                                elTag = new XElement("Tag");
                                elTagId = new XAttribute("TagId", devicesList[i].tagsList[j].ID.ToString());
                                elTagName = new XElement("TagName", devicesList[i].tagsList[j].Name);
                                elTagAddress = new XElement("TagAddress", devicesList[i].tagsList[j].Address);
                                elTag.Add(elTagId);
                                elTag.Add(elTagName);
                                elTag.Add(elTagAddress);
                                elDevice.Add(elTag);
                            }
                            elDevices.Add(elDevice);
                        }
                        xDocument.Add(elDevices);
                        xDocument.Save(fileName);
                        break;
                    default:
                        break;
                }
                txtFile.Text = fileName;
                AddRecent(fileName);
            }
            catch (Exception){ }
        }
        private void MnuRecent_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mnu = sender as MenuItem;
            OpenFile(mnu.Header.ToString());
        }

        private void mnuClearRecents_Click(object sender, RoutedEventArgs e)
        {
            recentsList.Clear();
            Properties.Settings.Default.Recents = "";
            Properties.Settings.Default.Save();
            if (mnuRecents.Items.Count > 2)
            {
                for(int i = 0; i<mnuRecents.Items.Count - 2; i++)
                {
                    mnuRecents.Items.RemoveAt(0);
                }
            }
        }

        private void AddRecent(string fileName)
        {
            int index = recentsList.IndexOf(fileName);
            if (index >= 0)
            {
                recentsList.RemoveAt(index);
                recentsList.Insert(0, fileName);
            }
            else
            {
                recentsList.Insert(0, fileName);
            }

            string str = "";
            foreach(string s in recentsList)
            {
                str += (s + ";");
            }
            Properties.Settings.Default.Recents = str;
            Properties.Settings.Default.Save();

            if (mnuRecents.Items.Count > 2)
            {
                int items = mnuRecents.Items.Count;
                for (int i = 0; i < items - 2; i++)
                {
                    mnuRecents.Items.RemoveAt(0);
                }
            }

            foreach (string rec in recentsList)
            {
                MenuItem mnu = new MenuItem();
                mnu.Header = rec;
                mnu.Click += MnuRecent_Click; ;
                mnuRecents.Items.Insert(mnuRecents.Items.Count - 2, mnu);
            }
        }

        private void mnuClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void mnuConnectionToModbus_Click(object sender, RoutedEventArgs e)
        {
            ConnectionToModbusWindow connectionToModbusWindow = new ConnectionToModbusWindow();
            connectionToModbusWindow.Show();
            connectionToModbusWindow.ButtonConn.Click += ConnectionToModbusWindowClick;
        }

        private void mnuDisconnectionToModbus_Click(object sender, RoutedEventArgs e)
        {

        }

        private void mnuAboutProgram_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Button_Test_Click(object sender, RoutedEventArgs e)
        {
            //byte slaveID = 1;
            //ushort startAddress = 534;
            //ushort numOfPoints = 3;
            //ushort[] holding_register = modbusSerialMaster.ReadHoldingRegisters(slaveID, startAddress,numOfPoints);

            //foreach (ushort register in holding_register) 
            //{
            //    textOut.Text +="Регистр: " + Convert.ToString(register);
            //}     
        }

        private void ConnectionToModbusWindowClick(object sender, EventArgs e)
        {
            serialPort = new SerialPort();

            serialPort.PortName = Connection.Name;
            serialPort.BaudRate = Connection.BaudRate;
            serialPort.DataBits = Connection.DataBits;

            if (Connection.Parity == "четный")
            {
                serialPort.Parity = Parity.Even;
            }
            else if (Connection.Parity == "нечетный")
            {
                serialPort.Parity = Parity.Odd;
            }
            else
            {
                serialPort.Parity = Parity.None;
            }

            if (Connection.StopBits == 1)
                serialPort.StopBits = StopBits.One;
            else
                serialPort.StopBits = StopBits.Two;

            try
            {
                serialPort.Open();
                modbusSerialMaster = ModbusSerialMaster.CreateRtu(serialPort);
                timerPoll.Start();
            }
            catch(Exception)
            {
                serialPort.Close();
                timerPoll.Stop();
                MessageBox.Show("Выбранный COM не подключен");
            }
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            if(serialPort != null)
                serialPort.Close();
            timerPoll.Stop();
            this.Close();
        }

        private void TimerPoll_Tick(object sender, EventArgs e)
        {
            int indexDevId = selectedDeviceId;
            byte slaveId = byte.Parse(devicesList[indexDevId].SlaveID);

            if (serialPort.IsOpen)
            {
                for (int i = 0; i < devicesList[indexDevId].tagsList.Count; i++)
                {
                    ushort address = ushort.Parse(devicesList[indexDevId].tagsList[i].Address);
                    ushort[] value = modbusSerialMaster.ReadHoldingRegisters(slaveId, address, 1);
                    devicesList[indexDevId].tagsList[i].Value = value[0].ToString();
                }
                dgTags.ItemsSource = null;
                dgTags.ItemsSource = devicesList[indexDevId].tagsList;
            }
        }
    }
}
