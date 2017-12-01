using System;
using System.Collections.Generic;
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
using System.IO.Ports;
using System.Runtime.InteropServices;

namespace RFID
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("kernel32.dll")]
        static extern void Sleep(int dwMilliseconds);

        [DllImport("MasterRD.dll")]
        static extern int lib_ver(ref uint pVer);

        [DllImport("MasterRD.dll")]
        static extern int rf_init_com(int port, int baud);

        [DllImport("MasterRD.dll")]
        static extern int rf_ClosePort();

        [DllImport("MasterRD.dll")]
        static extern int rf_antenna_sta(short icdev, byte mode);

        [DllImport("MasterRD.dll")]
        static extern int rf_init_type(short icdev, byte type);

        [DllImport("MasterRD.dll")]
        static extern int rf_request(short icdev, byte mode, ref ushort pTagType);

        [DllImport("MasterRD.dll")]
        static extern int rf_anticoll(short icdev, byte bcnt, IntPtr pSnr, ref byte pRLength);

        [DllImport("MasterRD.dll")]
        static extern int rf_select(short icdev, IntPtr pSnr, byte srcLen, ref sbyte Size);

        [DllImport("MasterRD.dll")]
        static extern int rf_halt(short icdev);

        [DllImport("MasterRD.dll")]
        static extern int rf_beep(short icdev, char msec);

        [DllImport("MasterRD.dll")]
        static extern int rf_light(short icdev, char color);

        [DllImport("MasterRD.dll")]
        static extern int rf_M1_authentication2(short icdev, byte mode, byte secnr, IntPtr key);

        [DllImport("MasterRD.dll")]
        static extern int rf_M1_initval(short icdev, byte adr, Int32 value);

        [DllImport("MasterRD.dll")]
        static extern int rf_M1_increment(short icdev, byte adr, Int32 value);

        [DllImport("MasterRD.dll")]
        static extern int rf_M1_decrement(short icdev, byte adr, Int32 value);

        [DllImport("MasterRD.dll")]
        static extern int rf_M1_readval(short icdev, byte adr, ref Int32 pValue);

        [DllImport("MasterRD.dll")]
        static extern int rf_M1_read(short icdev, byte adr, IntPtr pData, ref byte pLen);

        [DllImport("MasterRD.dll")]
        static extern int rf_M1_write(short icdev, byte adr, IntPtr pData);


        System.Windows.Threading.DispatcherTimer RFIDTimer = new System.Windows.Threading.DispatcherTimer();

        bool checkRFID = false;
        //เก็บสถานะการเชื่อมต่อ
        bool bConnectedDevice;

        //เก็บข้อมูล serial port ที่เชื่อต่อเข้ากับระบบ
        string[] ports;
        string[] stringSeparators;
        string[] str;
        //เก็บ id ของอุปกรณ์ที่จะสั่งงาน
        short icdev = 0x0000;

        string pass = "ffffffffffffffff";
        //เก็บข้อมูลความเร็วในการสื่อสาร
        int[] buad_rate =
        {
            9600,
            14400,
            19200,
            28800,
            38400,
            57600,
            115200
        };

        int[] Sector =
        {
            1,2,3,4,5,6,7,8,9,10,11,12,13,14,15
        };

        int[] Block =
        {
            0,1,
        };

        //เก็บข้อมูลแสง
        string[] light = {
            "off",
            "red",
            "green",
            "yellow"
        };
        // เก็บตัวเลขฐาน 16
        static char[] hexDigits = {
            '0','1','2','3','4','5','6','7',
            '8','9','A','B','C','D','E','F'};

        //แปลงตัวอักษรเลขฐาน 16 เป็นฐาน 2
        public static byte GetHexBitsValue(byte ch)
        {
            byte sz = 0;
            if (ch <= '9' && ch >= '0')
                sz = (byte)(ch - 0x30);
            if (ch <= 'F' && ch >= 'A')
                sz = (byte)(ch - 0x37);
            if (ch <= 'f' && ch >= 'a')
                sz = (byte)(ch - 0x57);

            return sz;
        }
        //

        #region byteHEX
        /// <summary>
        /// ตัวอักษร 1 ไบต์ เป็นฐาน 16.
        /// </summary>
        /// <param name="ib">ไบต์.</param>
        /// <returns>ข้อมูลที่ถูกแปลงแล้ว.</returns>
        public static String byteHEX(Byte ib)
        {
            String _str = String.Empty;
            try
            {
                char[] Digit = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A',
                'B', 'C', 'D', 'E', 'F' };
                char[] ob = new char[2];
                ob[0] = Digit[(ib >> 4) & 0X0F];
                ob[1] = Digit[ib & 0X0F];
                _str = new String(ob);
            }
            catch (Exception)
            {
                new Exception("对不起有错。");
            }
            return _str;

        }
        #endregion


        //แปลงไบต์อาร์เรยเป็น Hex String
        public static string ToHexString(byte[] bytes)
        {
            String hexString = String.Empty;
            for (int i = 0; i < bytes.Length; i++)
                hexString += byteHEX(bytes[i]);

            return hexString;
        }


        /// <summary>
        /// แปลง Hex String เป็น Byte Array
        /// </summary>
        /// <param name="theHex"> Hex String </param>
        /// <returns>Byte Array</returns>
        public static byte[] ToDigitsBytes(string theHex)
        {
            byte[] bytes = new byte[theHex.Length / 2 + (((theHex.Length % 2) > 0) ? 1 : 0)];
            for (int i = 0; i < bytes.Length; i++)
            {
                char lowbits = theHex[i * 2];
                char highbits;

                if ((i * 2 + 1) < theHex.Length)
                    highbits = theHex[i * 2 + 1];
                else
                    highbits = '0';

                int a = (int)GetHexBitsValue((byte)lowbits);
                int b = (int)GetHexBitsValue((byte)highbits);
                bytes[i] = (byte)((a << 4) + b);
            }

            return bytes;
        }


        public MainWindow()
        {
            InitializeComponent();
        }

        private void cmdConnect_Click(object sender, RoutedEventArgs e)
        {

            if (!bConnectedDevice)
            {
                int port = 0;
                int baud = 0;
                int status;

                port = Convert.ToInt32(str[1]);
                baud = Convert.ToInt32(cbBuadrate.SelectedItem.ToString());

                status = rf_init_com(port, baud);
                if (0 == status)
                {
                    txtCardUID.Content = "";
                    cmdConnect.Content = "Disconnect";
                    bConnectedDevice = true;
                    rf_light(icdev, (char)1);
                    MessageBox.Show("Connect device success!");
                }
                else
                {
                    string strError;
                    strError = "Connect device failed";
                    bConnectedDevice = false;
                    rf_light(icdev, (char)0);
                    MessageBox.Show(strError, "error", MessageBoxButton.OK, MessageBoxImage.Error);
                }


            }
            else
            {
                rf_ClosePort();
                bConnectedDevice = false;
                rf_light(icdev, (char)0);
                cmdConnect.Content = "Connect";
            }
        }

        private void cbComport_DropDownOpened(object sender, EventArgs e)
        {
            // Get a list of serial port names.
            ports = SerialPort.GetPortNames();

            Console.WriteLine("The following serial ports were found:");

            cbComport.Items.Clear();
            // Display each port name to the console.
            foreach (string port in ports)
            {
                cbComport.Items.Add(port);
            }

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            cbBuadrate.ItemsSource = buad_rate;
            txtBeep.Text = "1";
            cbLight.ItemsSource = light;

            cbBuadrate.SelectedIndex = 0;
            cbLight.SelectedIndex = 0;

            cbxMass.ItemsSource = Sector;
            cbxMassRead.ItemsSource = Sector;
            cbxSubmass.ItemsSource = Block;

            RFIDTimer.Tick += RFIDTimer_Tick;
            RFIDTimer.Interval = new TimeSpan(0, 0, 0, 0, 500);

        }

        private void RFIDTimer_Tick(object sender, EventArgs e)
        {
            short icdev = 0x0000;
            int status;
            byte type = (byte)'A'; //mifare one Card inquiry method is A
            byte mode = 0x52;
            ushort TagType = 0;
            byte bcnt = 0x04;//mifare cards are used 4 (must be 4)
            IntPtr pSnr;
            byte len = 255;
            sbyte size = 0;


            if (bConnectedDevice)
            {
                pSnr = Marshal.AllocHGlobal(1024);

                for (int i = 0; i < 2; i++)
                {
                    status = rf_antenna_sta(icdev, 0);//Turn off the antenna
                    if (status != 0)
                        continue;

                    Sleep(20);
                    status = rf_init_type(icdev, type);
                    if (status != 0)
                        continue;

                    Sleep(20);
                    status = rf_antenna_sta(icdev, 1);//Start the antenna
                    if (status != 0)
                        continue;

                    Sleep(50);
                    status = rf_request(icdev, mode, ref TagType);//Search all the cards
                    if (status != 0)
                    {
                        txtCardUID.Content = "";
                        txtDataOne.Text = "";
                        txtDataTwo.Text = "";
                        txtDataThree.Text = "";
                        rf_light(icdev, (char)1);
                        continue;
                    }

                    status = rf_anticoll(icdev, bcnt, pSnr, ref len);//Return the card's serial number
                    if (status != 0)
                        continue;

                    status = rf_select(icdev, pSnr, len, ref size);//Lock one ISO14443-3 TYPE_A card
                    if (status != 0)
                        continue;

                    byte[] szBytes = new byte[len];

                    for (int j = 0; j < len; j++)
                    {
                        szBytes[j] = Marshal.ReadByte(pSnr, j);
                    }

                    String m_cardNo = String.Empty;

                    for (int q = 0; q < len; q++)
                    {
                        m_cardNo += byteHEX(szBytes[q]);
                    }

                    if (txtCardUID.Content.ToString() != m_cardNo)
                    {
                        txtCardUID.Content = m_cardNo;
                        ReadCard(1);
                        rf_beep(icdev, (char)1);
                        rf_light(icdev, (char)2);
                    }
                    break;
                }
            }
        }

        private void cmdBeep_Click(object sender, RoutedEventArgs e)
        {
            int val = 0;
            if (!int.TryParse(txtBeep.Text, out val))
            {
                MessageBox.Show("กรุณากรอกข้อมูลตัวเลข (ตัวเลขที่กรอก * 10ms)");
            }
            else if (bConnectedDevice)
            {
                rf_beep(icdev, (char)val);
            }
            else {
                MessageBox.Show("กรุณาเชื่อมต่ออุปกรณ์");
            }
        }

        private void cmdLight_Click(object sender, RoutedEventArgs e)
        {
            if (bConnectedDevice)
            {
                rf_light(icdev, (char)cbLight.SelectedIndex);
            }
            else
            {
                MessageBox.Show("กรุณาเชื่อมต่ออุปกรณ์");
            }
        }

        private void cmdRFIDTimer_Click(object sender, RoutedEventArgs e)
        {
            if (!checkRFID)
            {
                RFIDTimer.Start();
                cmdRFIDTimer.Content = "ยกเลิก (การตรวจสอบบัตร)";
                checkRFID = true;
            }
            else
            {
                RFIDTimer.Stop();
                cmdRFIDTimer.Content = "เริ่ม (การตรวจสอบบัตร)";
                checkRFID = false;
            }
        }

        private void cbComport_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            stringSeparators = new string[] { "COM" };
            str = cbComport.SelectedItem.ToString().Split(stringSeparators, StringSplitOptions.None);
        }

        private void Read()
        {
            short icdev = 0x0000;
            int status;
            byte mode = 0x60;
            byte secnr = 0x00;

            txtDataOne.Text = "";
            txtDataTwo.Text = "";
            txtDataThree.Text = "";
            txtKeyA.Text = "";
            txtKey.Text = "";
            txtKeyB.Text = "";

            if (!bConnectedDevice)
            {
                MessageBox.Show("Not connect to device!!", "error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (rbtKeyB3.IsChecked.Value)
                mode = 0x61; //密钥

            secnr = Convert.ToByte(cbxMassRead.Text);

            IntPtr keyBuffer = Marshal.AllocHGlobal(1024);

            byte[] bytesKey = ToDigitsBytes(pass);
            for (int i = 0; i < bytesKey.Length; i++)
                Marshal.WriteByte(keyBuffer, i, bytesKey[i]);
            status = rf_M1_authentication2(icdev, mode, (byte)(secnr * 4), keyBuffer);
            Marshal.FreeHGlobal(keyBuffer);
            if (status != 0)
            {
                MessageBox.Show("rf_M1_authentication2 failed!!", "error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            //
            IntPtr dataBuffer = Marshal.AllocHGlobal(1024);
            for (int i = 0; i < 4; i++)
            {
                int j;
                byte cLen = 0;
                status = rf_M1_read(icdev, (byte)((secnr * 4) + i), dataBuffer, ref cLen);

                if (status != 0 || cLen != 16)
                {
                    MessageBox.Show("rf_M1_read failed!!", "error", MessageBoxButton.OK,MessageBoxImage.Error);
                    Marshal.FreeHGlobal(dataBuffer);
                    return;
                }

                byte[] bytesData = new byte[16];
                string str = "";
                for (j = 0; j < bytesData.Length; j++)
                {
                    bytesData[j] = Marshal.ReadByte(dataBuffer, j);

                    if (Marshal.ReadByte(dataBuffer, j) != '\0' && Marshal.ReadByte(dataBuffer, j) != 255) {
                        str += (char)Marshal.ReadByte(dataBuffer, j);
                    }
                }
                    

                if (i == 0)
                    txtDataOne.Text = str;
                else if (i == 1)
                    txtDataTwo.Text = str;
                else if (i == 2)
                    txtDataThree.Text = str;
                else if (i == 3)
                {
                    byte[] byteskeyA = new byte[6];
                    byte[] byteskey = new byte[4];
                    byte[] byteskeyB = new byte[6];

                    for (j = 0; j < 16; j++)
                    {
                        if (j < 6)
                            byteskeyA[j] = bytesData[j];
                        else if (j >= 6 && j < 10)
                            byteskey[j - 6] = bytesData[j];
                        else
                            byteskeyB[j - 10] = bytesData[j];
                    }

                    txtKeyA.Text = ToHexString(byteskeyA);
                    txtKey.Text = ToHexString(byteskey);
                    txtKeyB.Text = ToHexString(byteskeyB);
                }
            }
            Marshal.FreeHGlobal(dataBuffer);
        }

        private void Write() {
            short icdev = 0x0000;
            int status;
            byte mode = 0x60;
            byte secnr = 0x00;
            byte adr;
            int i;

            if (!bConnectedDevice)
            {
                MessageBox.Show("Not connect to device!!", "error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (rbtKeyB3.IsChecked.Value)
                mode = 0x61; //密钥

            secnr = Convert.ToByte(cbxMass.Text);
            adr = (byte)(Convert.ToByte(cbxSubmass.Text) + secnr * 4);

            if (cbxSubmass.SelectedIndex == 3)
            {
                if (MessageBox.Show("Be sure to write block3!", "Warning", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.Cancel)
                    return;
            }

            IntPtr keyBuffer = Marshal.AllocHGlobal(1024);

            byte[] bytesKey = ToDigitsBytes(pass);
            for (i = 0; i < bytesKey.Length; i++)
                Marshal.WriteByte(keyBuffer, i, bytesKey[i]);
            status = rf_M1_authentication2(icdev, mode, (byte)(secnr * 4), keyBuffer);
            Marshal.FreeHGlobal(keyBuffer);
            if (status != 0)
            {
                MessageBox.Show("rf_M1_authentication2 failed!!", "error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            //
            byte[] bytesBlock = { 0xff,0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff };
            if (cbxSubmass.SelectedIndex == 3)
            {
                String strCompont = txtWriteKeyA.Text;
                strCompont += txtWriteKey.Text;
                strCompont += txtWriteKeyB.Text;
                bytesBlock = ToDigitsBytes(strCompont);
            }
            else
            {
                if (txtWriteData.Text.ToCharArray().Length <= 16 ) {
                    bytesBlock = txtWriteData.Text.ToCharArray().Select(c => (byte)c).ToArray();
                }
                
            }

            IntPtr dataBuffer = Marshal.AllocHGlobal(1024);

            for (i = 0; i < bytesBlock.Length; i++)
                Marshal.WriteByte(dataBuffer, i, bytesBlock[i]);
            status = rf_M1_write(icdev, adr, dataBuffer);
            Marshal.FreeHGlobal(dataBuffer);

            if (status != 0)
            {
                MessageBox.Show("rf_M1_write failed!!", "error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        private void btnRead_Click(object sender, RoutedEventArgs e)
        {
            Read();
        }

        private void btnWrite_Click(object sender, RoutedEventArgs e)
        {
            Write();
        }

        private void cbxSubmass_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbxSubmass.SelectedItem == null || cbxMass.SelectedItem == null)
            {
                btnWrite.IsEnabled = false;
            }
            else
            {
                btnWrite.IsEnabled = true;
            }
        }

        private void cbxMass_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbxSubmass.SelectedItem == null || cbxMass.SelectedItem == null)
            {
                btnWrite.IsEnabled = false;
            }
            else
            {
                btnWrite.IsEnabled = true;
            }
        }

        private void ReadCard(int sector)
        {
            short icdev = 0x0000;
            int status;
            byte mode = 0x60;
            byte secnr = 0x00;

            txtDataOne.Text = "";
            txtDataTwo.Text = "";
            txtDataThree.Text = "";

            if (!bConnectedDevice)
            {
                System.Windows.MessageBox.Show("Not connect to device!!", "error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            secnr = Convert.ToByte(sector);

            IntPtr keyBuffer = Marshal.AllocHGlobal(1024);

            byte[] bytesKey = ToDigitsBytes(pass);
            for (int i = 0; i < bytesKey.Length; i++)
                Marshal.WriteByte(keyBuffer, i, bytesKey[i]);
            status = rf_M1_authentication2(icdev, mode, (byte)(secnr * 4), keyBuffer);
            Marshal.FreeHGlobal(keyBuffer);
            if (status != 0)
            {
                System.Windows.MessageBox.Show("rf_M1_authentication2 failed!!", "error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            //
            IntPtr dataBuffer = Marshal.AllocHGlobal(1024);
            for (int i = 0; i < 2; i++)
            {
                int j;
                byte cLen = 0;
                status = rf_M1_read(icdev, (byte)((secnr * 4) + i), dataBuffer, ref cLen);

                if (status != 0 || cLen != 16)
                {
                    System.Windows.MessageBox.Show("rf_M1_read failed!!", "error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Marshal.FreeHGlobal(dataBuffer);
                    return;
                }

                byte[] bytesData = new byte[16];
                string str = "";

                for (j = 0; j < bytesData.Length; j++)
                {
                    bytesData[j] = Marshal.ReadByte(dataBuffer, j);

                    if (Marshal.ReadByte(dataBuffer, j) != '\0' && Marshal.ReadByte(dataBuffer, j) != 0xff)
                    {
                        str += (char)Marshal.ReadByte(dataBuffer, j);
                    }
                }

                if (i == 0)
                {
                    txtDataOne.Text = str;
                }
                else if (i == 1)
                    txtDataTwo.Text = str;
            }
            Marshal.FreeHGlobal(dataBuffer);
        }

        private void WriteCard(int sector, int block, string data)
        {
            short icdev = 0x0000;
            int status;
            byte mode = 0x61;
            byte secnr = 0x00;
            byte adr;
            int i;

            if (!bConnectedDevice)
            {
                System.Windows.MessageBox.Show("Not connect RFID Reader", "error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            secnr = Convert.ToByte(sector);
            adr = (byte)(Convert.ToByte(block) + secnr * 4);

            if (block == 3)
            {
                if (System.Windows.MessageBox.Show("Be sure to write block3!", "Warning", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.Cancel)
                    return;
            }

            IntPtr keyBuffer = Marshal.AllocHGlobal(1024);

            byte[] bytesKey = ToDigitsBytes(pass);
            for (i = 0; i < bytesKey.Length; i++)
                Marshal.WriteByte(keyBuffer, i, bytesKey[i]);
            status = rf_M1_authentication2(icdev, mode, (byte)(secnr * 4), keyBuffer);
            Marshal.FreeHGlobal(keyBuffer);
            if (status != 0)
            {
                System.Windows.MessageBox.Show("ไม่สามารถเข้าภึงข้อมูลการ์ดได้/n*รหัสเข้าถึงข้อมูลไม่ถูกต้อง", "error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            //
            byte[] bytesBlock = { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff };

            if (data.ToCharArray().Length <= 16)
            {
                bytesBlock = data.ToCharArray().Select(c => (byte)c).ToArray();
            }

            IntPtr dataBuffer = Marshal.AllocHGlobal(1024);

            for (i = 0; i < 16; i++)
            {
                if (i < data.ToCharArray().Length)
                    Marshal.WriteByte(dataBuffer, i, bytesBlock[i]);
                else
                    Marshal.WriteByte(dataBuffer, i, 0xff);
            }
            status = rf_M1_write(icdev, adr, dataBuffer);
            Marshal.FreeHGlobal(dataBuffer);

            if (status != 0)
            {
                System.Windows.MessageBox.Show("write failed!!", "error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        private void cbxMassRead_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btnRead.IsEnabled = true;
        }
    }
}
