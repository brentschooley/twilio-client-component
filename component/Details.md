Create rich communication experiences in your Xamarin.iOS and Xamarin.Android apps with [Twilio Client](http://twilio.com/client). The Twilio Client component makes it easy for you to add VoIP calling features to your app which enable it to call other mobile apps and traditional phones.

To learn more about Twilio, visit [http://twilio.com].

The following code shows some basic usage of the component. For more details, have a look at the Getting Started guide or the included Samples.

## iOS

```csharp
using TwilioClient;
//...
TCConnection connection;
TCDevice device;

public override void ViewDidLoad ()
{
			//  Request a client capability token. See the Getting Started guide for details on setting up the server code for the capability token.
			var client = new HttpClient ();
			var token = await client.GetStringAsync("*** capability token url ***");

			// Create a Twilio Client Device passing in the capability token.
			device = new TCDevice (token, null);
			
			// Set up device events
			device.StartedListeningForIncomingConnections += delegate {
			}
			device.StoppedListeningForIncomingConnections += delegate {
			}
			device.ReceivedIncomingConnection += (sender, e) => {
				connection = e.Connection;
				// Set up the connection
				if(connection != null)
				{
					connection.Failed += delegate {
						// ...
					};
					connection.StartedConnecting += delegate {
						// ...
					};
					connection.Connected += delegate {
						// ...
					};
					connection.Disconnected += delegate {
						connection = null;
					};
					
					// Accept call (can also reject the call)
					connection.Accept();
				}
			}
}
```

## Android
``` csharp
public class MainActivity : Activity, Twilio.IInitListener, IDeviceListener, IConnectionListener
{
	public IConnection Connection {
		get;
		set;
	}

	public Device Device {
		get;
		set;
	}

	protected override void OnCreate (Bundle bundle)
	{
		// ...
		
		Twilio.Initialize (this.ApplicationContext, this);
		
		// ...
	}

	#region Twilio.IInitListener
	public void OnInitialized ()
	{
		Console.WriteLine ("Twilio SDK is ready.");

		try {
			var client = new HttpClient ();
			var token = await client.GetStringAsync (*** capability token url ***);

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

	// Set up Device events
	#region IDeviceListener
	public void OnPresenceChanged (Device device, PresenceEvent presenceEvent)
	{
		// ...
	}

	public void OnStartListening (Device device)
	{
		// ...
	}

	public void OnStopListening (Device device)
	{
		// ...
	}

	public void OnStopListeningWithError (Device device, int errorCode, string errorMessage)
	{
		// ...
	}

	public bool ReceivePresenceEvents (Device device)
	{
		// ...
	}
	#endregion

	// Set up Connection events
	#region IConnectionListener
	public void OnConnected (IConnection connection)
	{
		// ...
	}

	public void OnConnecting (IConnection connection)
	{
		// ...
	}

	public void OnDisconnected (IConnection connection)
	{
		// ...
	}

	public void OnDisconnectedWithError (IConnection connection, int errorCode, string errorMessage)
	{
		// ...
	}
	#endregion
}
```
