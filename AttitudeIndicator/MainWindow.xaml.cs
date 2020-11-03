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
using Microsoft.FlightSimulator.SimConnect;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace AttitudeIndicator
{
	public enum DEFINITIONS
	{
		Struct1 = 0
	};

	public enum REQUEST
	{
		Dummy = 0
	};

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
	struct Struct1
	{

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
		public string title;
		public double PITCH;
		public double BANK;

	};

	/// <summary>
	/// MainWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class MainWindow : Window
    {
		SimConnect simconnect = null;
		const int WM_USER_SIMCONNECT = 0x0402;

		// ウィンドウハンドルを取得
		public IntPtr Handle
		{
			get
			{
				var helper = new System.Windows.Interop.WindowInteropHelper(this);
				return helper.Handle;
			}
		}

		public MainWindow()
        {
            InitializeComponent();
        }

		#region プロシージャ関連

		protected HwndSource GetHWinSource()
		{
			return PresentationSource.FromVisual(this) as HwndSource;
		}

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);
			GetHWinSource()?.AddHook(WndProc);
		}

		private IntPtr WndProc(IntPtr hWnd, int iMsg, IntPtr hWParam, IntPtr hLParam, ref bool bHandled)
		{
			if (iMsg == WM_USER_SIMCONNECT)
			{
				simconnect?.ReceiveMessage();
			}

			return IntPtr.Zero;
		}

		#endregion

		/// <summary>
		/// ウィンドウが表示されたときのイベント
		/// </summary>
		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			try
			{
				simconnect = new SimConnect("Managed Data Request", this.Handle, WM_USER_SIMCONNECT, null, 0);

				/// Listen to connect and quit msgs
				simconnect.OnRecvOpen += new SimConnect.RecvOpenEventHandler(SimConnect_OnRecvOpen);
				simconnect.OnRecvQuit += new SimConnect.RecvQuitEventHandler(SimConnect_OnRecvQuit);

				/// Listen to exceptions
				simconnect.OnRecvException += new SimConnect.RecvExceptionEventHandler(SimConnect_OnRecvException);

				/// Catch a simobject data request
				simconnect.OnRecvSimobjectData += SimConnect_OnRecvSimobjectData;


				simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "Title", null, SIMCONNECT_DATATYPE.STRING256, 0, SimConnect.SIMCONNECT_UNUSED);
				simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ATTITUDE INDICATOR PITCH DEGREES", "Radians", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
				simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ATTITUDE INDICATOR BANK DEGREES", "Radians", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);

				simconnect.RegisterDataDefineStruct<Struct1>(DEFINITIONS.Struct1);

				simconnect.RequestDataOnSimObject(REQUEST.Dummy, DEFINITIONS.Struct1, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SIM_FRAME, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);

			}
			catch (COMException ex)
			{
				MessageBox.Show("シミュレーターに接続出来ませんでした。\n終了します。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
				this.Close();
			}
		}

		private void SimConnect_OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
		{
			
		}

		private void SimConnect_OnRecvQuit(SimConnect sender, SIMCONNECT_RECV data)
		{
			SimConnectDisconnect();
			this.Close();
		}

		private void SimConnect_OnRecvException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data)
		{
			SIMCONNECT_EXCEPTION eException = (SIMCONNECT_EXCEPTION)data.dwException;
			Console.WriteLine("SimConnect_OnRecvException: " + eException.ToString());
		}

		private void SimConnect_OnRecvSimobjectData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data)
		{
			Struct1 struct1 = (Struct1)data.dwData[0];
			SetPith(struct1.PITCH);
			SetBank(struct1.BANK);
		}

		/// <summary>
		/// ウィンドウが閉じたときのイベント
		/// </summary>
		private void Window_Closed(object sender, EventArgs e)
		{
			SimConnectDisconnect();
		}

		/// <summary>
		/// シムコネクトの切断を行います。
		/// </summary>
		private void SimConnectDisconnect()
		{
			if (simconnect != null)
			{
				simconnect.Dispose();
				simconnect = null;
			}
		}

		/// <summary>
		/// ピッチを設定します。
		/// </summary>
		/// <param name="pithDegree"></param>
		private void SetPith(double pithRadian)
		{
			double degree = pithRadian * 180 / Math.PI;

			TransformGroup transformGroup = new TransformGroup();
			transformGroup.Children.Add(new TranslateTransform(0, degree * 10 * -1));

			pitchCanvas.RenderTransform = transformGroup;
		}

		/// <summary>
		/// バンクを設定します。
		/// </summary>
		/// <param name="bankRadian"></param>
		private void SetBank(double bankRadian)
		{
			double degree = bankRadian * 180 / Math.PI;

			TransformGroup transformGroup = new TransformGroup();
			transformGroup.Children.Add(new RotateTransform(degree));

			rollGrid.RenderTransform = transformGroup;
		}

	}
}
