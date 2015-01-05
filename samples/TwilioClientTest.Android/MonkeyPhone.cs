using System;
using TwilioClient.Android;
using System.Collections.Generic;
using System.Net.Http;
using Android.Content;
using Android.OS;
using Android.App;
using Android.Util;
using Android.Runtime;

namespace TwilioClientTest.Android
{
	public delegate void StatusChangedEventHandler(object sender, EventArgs e);

	public class MonkeyPhone : Java.Lang.Object, Twilio.IInitListener, IDeviceListener, IConnectionListener
	{
		private const string TAG = "MonkeyPhone";
		Context context;

		public IConnection Connection {
			get;
			set;
		}
		public Device Device {
			get;
			set;
		}

		public event StatusChangedEventHandler StatusChanged;

		protected virtual void OnStatusChanged (EventArgs e)
		{
			if (StatusChanged != null) {
				StatusChanged (this, e);
			}
		}

		public MonkeyPhone (Context context)
		{
			this.context = context;
			Twilio.Initialize (context, this);
		}

		public void Connect(string target)
		{
			var parameters = new Dictionary<string, string> () {
				{ "Source", "+12152407664"},
				{ "Target", target}
			};

			Connection = Device.Connect (parameters, null);
			Connection.SetConnectionListener (this);

			if (Connection == null) {
				Console.WriteLine ("Failed to create connection.");
			}
		}

		public void Disconnect()
		{
			if (Connection != null) {
				Connection.Disconnect();
				Connection = null;
			}
		}

		public void HandleIncomingConnection(Device inDevice, IConnection inConnection)
		{
			Log.Info(TAG, "Device received incoming connection");

			this.Device = inDevice;

			if (Connection != null)
				Connection.Disconnect();
			Connection = inConnection;
			Connection.SetConnectionListener (this);
			Connection.Accept();
			OnStatusChanged (EventArgs.Empty);
		}

		void SetupConnectionEvents ()
		{
			if (Connection != null) {
				Connection.SetConnectionListener (this);
			}
		}

		#region IDeviceListener
		public void OnPresenceChanged (Device device, PresenceEvent presenceEvent)
		{
			Console.WriteLine ("Received Presence Update");
		}

		public void OnStartListening (Device device)
		{
			Console.WriteLine("Started listening for incoming connections...");
			OnStatusChanged(EventArgs.Empty);
		}

		public void OnStopListening (Device device)
		{
			Console.WriteLine("Stopped listening for incoming connections..."); 
			OnStatusChanged(EventArgs.Empty);
		}

		public void OnStopListeningWithError (Device device, int errorCode, string errorMessage)
		{
			Console.WriteLine("Stopped listening for incoming connections..."); 
			OnStatusChanged(EventArgs.Empty);
		}

		public bool ReceivePresenceEvents (Device device)
		{
			return true;
		}
		#endregion

		#region IConnectionListener

		public void OnConnected (IConnection connection)
		{
			OnStatusChanged (EventArgs.Empty);
		}

		public void OnConnecting (IConnection connection)
		{
			OnStatusChanged (EventArgs.Empty);
		}

		public void OnDisconnected (IConnection connection)
		{
			OnStatusChanged (EventArgs.Empty);
			Connection = null;

		}

		public void OnDisconnectedWithError (IConnection connection, int errorCode, string errorMessage)
		{
			OnStatusChanged (EventArgs.Empty);
			Connection = null;
		}

		#endregion

		#region IInitListener implementation

		public void OnError (Java.Lang.Exception p0)
		{
			Console.WriteLine ("Twilio SDK couldn't start: " + p0.LocalizedMessage);
		}

		public async void OnInitialized ()
		{
			Console.WriteLine ("Twilio SDK is ready.");

			try {
				var clientName = "xamarin";

				var client = new HttpClient ();
				var token = await client.GetStringAsync ("http://twilioclientazure.azurewebsites.net/Client/Token");

				Device = Twilio.CreateDevice(token, null);

				var intent  = new Intent(context, typeof(MainActivity));
				var pendingIntent = PendingIntent.GetActivity(context, 0, intent, PendingIntentFlags.UpdateCurrent);

				Device.SetIncomingIntent(pendingIntent);

				Device.SetDeviceListener(this);

			} catch (Exception ex) {
				Console.WriteLine ("Error: " + ex.Message);
			}
		}

		#endregion

		protected override void Dispose (bool disposing)
		{
			base.Dispose (disposing);
			if (Connection != null) {
				Connection.Disconnect();
				Connection = null;
			}
		}
	}
}

