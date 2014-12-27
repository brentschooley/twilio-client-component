using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using TwilioClient.Android;
using System.Collections.Generic;
using System.Net.Http;
using System.Globalization;

namespace TwilioClientTest.Android
{
	[Activity (Label = "TwilioTestClient", MainLauncher = true, Icon="@drawable/icon")]
	public class MainActivity : Activity
	{
		MonkeyPhone phone;

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			// Set our view from the "main" layout resource
			SetContentView (Resource.Layout.Main);

			phone = new MonkeyPhone (this.ApplicationContext);

			phone.StatusChanged += (sender, e) => {
				UpdateStatus();
			};

			// Get our button from the layout resource,
			// and attach an event to it
			Button dialButton = FindViewById<Button> (Resource.Id.dialButton);
			//Button hangupButton = FindViewById<Button> (Resource.Id.hangupButton);
			EditText targetEditText = FindViewById<EditText> (Resource.Id.targetText);

			dialButton.Click += delegate {
				if(phone.Connection != null && phone.Connection.State == ConnectionState.Connected) 
				{
					phone.Connection.Disconnect();
				}
				else
				{
					phone.Connect(targetEditText.Text);
				}
			};

			SetupSoundOptionEvents ();
			SetupOptions ();
		}

		protected override void OnNewIntent (Intent intent)
		{
			base.OnNewIntent (intent);
			this.Intent = intent;
		}

		protected override void OnResume ()
		{
			base.OnResume ();

			var intent = this.Intent;
			var device = intent.GetParcelableExtra(Device.ExtraDevice) as Device;
			//var connection = intent.GetParcelableExtra(Device.ExtraConnection) as IConnection;
			var connection = intent.GetParcelableExtra(Device.ExtraConnection).JavaCast<IConnection>();
			if (device != null && connection != null) {
				intent.RemoveExtra(Device.ExtraDevice);
				intent.RemoveExtra(Device.ExtraConnection);
				phone.HandleIncomingConnection(device, connection);
			}
		}

		void SetupSoundOptionEvents ()
		{
			var disconnectSoundToggle = FindViewById<ToggleButton> (Resource.Id.disconnectSound);
			var incomingSoundToggle = FindViewById<ToggleButton> (Resource.Id.incomingSound);
			var outgoingSoundToggle = FindViewById<ToggleButton> (Resource.Id.outgoingSound);

			disconnectSoundToggle.CheckedChange += (sender, e) => {
				if(phone.Device != null) {
					phone.Device.DisconnectSoundEnabled = disconnectSoundToggle.Checked;
				}
			};

			incomingSoundToggle.CheckedChange += (sender, e) => {
				if(phone.Device != null) {
					phone.Device.IncomingSoundEnabled = incomingSoundToggle.Checked;
				}
			};

			outgoingSoundToggle.CheckedChange += (sender, e) => {
				if(phone.Device != null) {
					phone.Device.OutgoingSoundEnabled = outgoingSoundToggle.Checked;
				}
			};
		}

		void SetupOptions ()
		{
			var mutedToggle = FindViewById<ToggleButton> (Resource.Id.muted);
			var deviceListeningToggle = FindViewById<ToggleButton> (Resource.Id.deviceListening);

			mutedToggle.CheckedChange += (sender, e) => {
				if(phone.Connection != null && phone.Connection.State == ConnectionState.Connected) {
					phone.Connection.Muted = mutedToggle.Checked;
				}
			};

			deviceListeningToggle.CheckedChange += (sender, e) => {
				if(phone.Device != null) {
					if(deviceListeningToggle.Checked) {
						phone.Device.Listen();
					}
					else {
						phone.Device.Unlisten();
					}
				}
			};
		}

		void UpdateStatus ()
		{
			RunOnUiThread (() => {
				var deviceState = FindViewById<TextView> (Resource.Id.deviceStatus);
				var connectionState = FindViewById<TextView> (Resource.Id.connectionStatus);
				var dialButton = FindViewById<Button> (Resource.Id.dialButton);

				var textInfo = System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo;

				deviceState.Text = textInfo.ToTitleCase(phone.Device.GetState ().ToString ().ToLower());


				if (phone.Connection != null) {
					switch (phone.Connection.State.ToString ().ToLower()) {
					case "connected":
						dialButton.Text = GetString (Resource.String.hangup);
						break;
					case "disconnected":
						dialButton.Text = GetString (Resource.String.dial);
						break;
					default:
						break;
					}
					connectionState.Text = textInfo.ToTitleCase(phone.Connection.State.ToString ().ToLower());
				}
			});
		}
	}
}


