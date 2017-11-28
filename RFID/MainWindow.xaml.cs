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

        //เก็บ id ของอุปกรณ์ที่จะสั่งงาน
        short icdev = 0x0000;

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

                port = 3;
                baud = Convert.ToInt32(cbBuadrate.SelectedItem.ToString());

                status = rf_init_com(port, baud);
                if (0 == status)
                {
                    cmdConnect.Content = "Disconnect";
                    bConnectedDevice = true;
                    MessageBox.Show("Connect device success!");
                }
                else
                {
                    string strError;
                    strError = "Connect device failed";
                    bConnectedDevice = false;
                    MessageBox.Show(strError, "error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                
            }
            else
            {
                rf_ClosePort();
                bConnectedDevice = false;
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

           
            RFIDTimer.Tick += RFIDTimer_Tick;
            RFIDTimer.Interval = new TimeSpan(0, 0, 0, 0,500);
            


        }

        private void RFIDTimer_Tick(object sender, EventArgs e)
        {
            // code goes here

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
            }
            else
            {
                RFIDTimer.Stop();
                cmdRFIDTimer.Content = "เริ่ม (การตรวจสอบบัตร)";
            }
        }
    }
}
