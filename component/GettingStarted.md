[Twilio Client for Mobile](http://twilio.com/client/mobile) enables mobile developers to create VoIP applications on the iOS and Android platforms. Xamarin allows C#  developers to build native iOS and Android applications. By simply adding a few lines of code your application will be able to call any phone on the global telecom network as well as other Twilio Client apps running on the web, iOS or Android. The Twilio Client component will talk to the [Twilio](http://twilio.com) backend to handle presence updates and connect calls.

To make all of this work we will need some server code to generate [capability tokens](https://www.twilio.com/docs/client/capability-tokens). A capability token grants the client access to use your Twilio account to make outbound or accept incoming calls. Yes you read that correctly, the client application will be spending your precious Twilio credits. In your server application you'll probably want to validate users before granting them a token. You'll also want to consider limiting the amount of time the client can connect to Twilio so that you don't rack up huge charges.

## Creating a Twilio Account

To use Twilio Client you'll need a Twilio account. Follow [this guide](http://devangel-board.appspot.com/tools/getstarted.html) to create an account.

## Generating capability tokens in ASP.NET MVC

We'll need some server code to generate capability tokens. You can use whatever server technology you'd like. This tutorial will use Visual Studio 2013 Update 2. Fire up Visual Studio and create a new ASP.NET Web Application named TwilioClientAzure and configure it to use the Empty Project template and include MVC and Azure deployment.

Install the `Twilio.Mvc` and `Twilio.Client` NuGet packages in your ASP.NET MVC app.

Next, add a Controller to your ASP.NET MVC project that will serve up the capability tokens to your client application. Right-click on the project node and select `Add->Controller...`. Select `MVC 5 Controller - Empty` in the resulting dialog and then click `Add`. Name the controller `ClientController`. Add the following using statement to the top of the ClientController class:

```csharp
using Twilio;
```
Replace the `Index()` method in the ClientController class with this:

```csharp
// GET: Client/Token?ClientName=foo
public ActionResult Token(string clientName = "default")
{
	// Create a TwilioCapability object passing in our credentials.
    var capability = new TwilioCapability(*** Your AccountSid ***, *** Your AuthToken ***);

	// Specify that this token allows receiving of incoming calls
    capability.AllowClientIncoming(clientName);

	// Return the token as text
    return Content(capability.GenerateToken());
}
```

Publish your site to Azure and access http://your_azure_site_url/Client/Token. You should see a long string in the browser if it is working. 

If you're only developing for Android, skip the next section.

## Adding Twilio Client functionality on iOS

Assuming you have an application already created and the Twilio Client component is added to it, add the `using` statement to your View Controller:

```csharp
using TwilioClient.iOS;
```

Add fields for a TCDevice and a TCConnection to the top of your View Controller above the constructor:

```csharp
TCDevice _device;
TCConnection _connection;
```

Edit the ViewDidLoad method so that it contains the following code:

```csharp
public async override void ViewDidLoad ()
{
	base.ViewDidLoad ();
	
	// Create an HTTPClient object and use it to fetch
	// a capability token from our site on Azure. By default this
	// will give us a client name of 'xamarin'
	var client = new HttpClient ();
	var token = await client.GetStringAsync("http://your-Azure-site-URL/Client/Token");

	// Create a new TCDevice object passing in the token.
	_device = new TCDevice (token, null);

	// Set up the event handlers for the device
	SetupDeviceEvents ();
}
```

This code gets a capability token for a client named 'xamarin' from the Azure site you set up earlier. Next, setup the event handler for an incoming call in the `SetupDeviceEvents` method. Add the following code to the view controller:

```csharp
void SetupDeviceEvents ()
{
	if (_device != null) 
	{
		// When a new connection comes in, store it and use it to accept the incoming call.
		_device.ReceivedIncomingConnection += (sender, e) => {
			_connection = e.Connection;
			_connection.Accept();
		};
	}
}
```

If you're not developing for Android, skip the next section.

## Adding Twilio Client functionality on Android

Assuming you have an application already created and the Twilio Client component is added to it we need to first add some permissions to `AndroidManifest.xml`. Open this file and check the following permissions:

* AccessNetworkState
* AccessWifiState
* ModifyAudioSettings
* RecordAudio

If you want to use the sounds that come with the Twilio Client Android SDK for incoming, outgoing and disconnect events you can get them from the sample included with the Twilio Client component. Add the `outgoing.wav`, `incoming.wav` and `disconnect.wav` files to the `Resources\raw` directory of your application.

Add the following `<service>` tag to the application tag in your `AndroidManifest.xml`'s Source view to declare the TwilioClientService:

```
<service android:name="com.twilio.client.TwilioClientService" android:exported="false" />
```

Add the folowing `using` statement to your Activity:

```csharp
using TwilioClient.Android;
```

Add fields for a Device and an IConnection to the top of your Activity:

```csharp
private Device _device;
private IConnection _connection;
```

Edit the OnCreate method so that it contains the following code:

```csharp
protected override void OnCreate (Bundle bundle)
{
    base.OnCreate (bundle);

    // Set our view from the "main" layout resource
    SetContentView (Resource.Layout.Main);

    Twilio.Initialize (this.ApplicationContext, this);
}
```
Implement the Twilio.IInitListener interface:

```csharp
#region IInitListener
public void OnError (Java.Lang.Exception p0)
{
    Log.Info (TAG, "Error initializing Twilio.");
}

public async void OnInitialized ()
{
    try {
        var clientName = "xamarin";

        var client = new HttpClient ();
        var token = await client.GetStringAsync ("http://your-Azure-site-url/Client/Token?clientName=" + clientName);

        _device = Twilio.CreateDevice(token, null);

        var intent  = new Intent(this.ApplicationContext, typeof(MainActivity));
        var pendingIntent = PendingIntent.GetActivity(this.ApplicationContext, 0, intent, PendingIntentFlags.UpdateCurrent);

        _device.SetIncomingIntent(pendingIntent);

    } catch (Exception ex) {
        Log.Info(TAG, "Error: " + ex.Message);
    }
}
#endregion
```

Add the following code that will handle incoming calls via the device's IncomingIntent to the activity:

```csharp
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
        HandleIncomingConnection(device, connection);
    }
}

void HandleIncomingConnection (Device device, IConnection connection)
{
    if (_connection != null)
        _connection.Disconnect();
    _connection = connection;
    _connection.Accept();
}
```

At this point, the application is ready to receive incoming calls.

## Responding to an incoming call

Head back over to the ASP.NET MVC application and add the following using statements to ClientController.cs:

```csharp
using Twilio.TwiML;
using Twilio.TwiML.Mvc;
```
Next, add the following method to the class:

```csharp
// /Client/CallXamarin
public ActionResult CallXamarin()
{
    var response = new TwilioResponse();
    response.Dial(new Client("xamarin"));
    return new TwiMLResult(response);
}
```

This code produces TwiML that we will use to tell Twilio to connect us to the client. Publish your site to Azure so that the new endpoint will be available.

Go to your [phone number list](https://www.twilio.com/user/account/phone-numbers/incoming) in your Twilio account and click on the number you created earlier. Underneath Voice where it says "Request URL" enter: http://your_azure_url/Client/CallXamarin and then click the red Save button at the bottom of the screen.

Head back to Xamarin Studio and run your iPadPhone application in the iPad simulator or your Android application in the emulator of your choice. The app will get a capability token and then sit there ready for you to call it. Call your Twilio number using your normal voice phone. Twilio will connect you to the app running in the simulator/emulator and you should be able to hear yourself echoing through your computer speakers. You just turned your iPad/Android device into a phone! If our app could only receive incoming phone calls it wouldn't be much of a phone. Let's set up a few things to enable it to make outbound calls as well.

## Creating a TwiML app

When we update the capability token we send to the client we will need to provide the ID to a [TwiML application](https://www.twilio.com/user/account/apps).  A TwiML application has a few uses. One common use case is to allow a bunch of Twilio phone numbers to use the same Voice and SMS URLs, so you don’t have to copy/paste your server configuration in the website’s UI over and over again. The other use is handling outbound calls for Twilio Client apps.

The iPad/Android client will initiate an outbound call to a number the user enters into a text field when the user taps a button. The Voice URL of a TwiML app will be requested when this call is initiated to tell Twilio's backend how to handle the call. Twilio Client knows which TwiML application to use because we will generate the capability token with the unique identifier of a TwiML application. Let's create a TwiML application now which will use a Voice URL from our Azure application. [You can create a TwiML app here](https://www.twilio.com/user/account/apps/add). 

Set the Voice URL to: http://your-azure-url/Client/InitiateCall. Save your TwiML app and then click its name in the list. Copy the "Sid" property at the top of the screen to the clipboard. You'll need it in the next step.

## Updating the server app to handle outgoing calls

Back in the ASP.NET MVC app update the `Token()` method of your `ClientController` class to add the following line:

```csharp
// Replace "AP*****" with the TwiML app Sid you copied in the last step
capability.AllowClientOutgoing("AP********************************");
```

Test your app and make sure the `/Client/Token` endpoint is still returning a token before moving on.
 
Next, add the endpoint that we specified in our TwiML application's Voice URL. Add the following method to the class:

```csharp
// /Client/InitiateCall?source=5551231234&target=5554561212
public ActionResult InitiateCall(string source, string target)
{
    var response = new TwilioResponse();

    // Add a <Dial> tag that specifies the callerId attribute
    response.Dial(target, new { callerId = source });

    return new TwiMLResult(response);
}
```

Publish your server app to Azure before moving on.

If you are not developing for iOS, skip the next section.

## Placing an outbound call from your iOS app

This code assumes you have a button named `callButton` that will initiate the call and a text field named `numberField` that is used for inputting a number to dial. Add the following code to the end of the ViewDidLoad method in your app making sure to note the TODO item:

```csharp
// Add code to TouchUpInside to place the call when the user taps the button
callButton.TouchUpInside += (sender, e) => {
	// Setup the numbers to use for the call
	// TODO: Update this code to include your Twilio number
	NSDictionary parameters = NSDictionary.FromObjectsAndKeys (
		new object[] { "*** Your Twilio Number ***", numberField.Text },
		new object[] { "Source", "Target" }
	);

	// Make the call
	_connection = _device.Connect(parameters, null);
};
```

Run the iPadPhone app, enter a phone number in the text field and tap the button. You should hear some tones indicating Twilio Client is placing the call and then it will start ringing. You've just turned your iPad into a phone!

## Placing an outbound call from your Android app

This code assumes your layout includes a Button named `callButton` that will initiate the call and an EditText named `numberEditText` that is used for inputting a number to dial. Add the following code to your Activity's OnCreate method:

```csharp
// Get our button from the layout resource,
// and attach an event to it
Button callButton = FindViewById<Button> (Resource.Id.callButton);
EditText numberEditText = FindViewById<EditText> (Resource.Id.numberEditText);

callButton.Click += delegate {
    if(_connection != null && _connection.State == ConnectionState.Connected) 
    {
        _connection.Disconnect();
    }
    else
    {
        MakeOutgoingCall(numberEditText.Text);
    }
};
```

Add the MakeOutgoingCall method to your Activity:

```csharp
void MakeOutgoingCall (string target)
{
    var parameters = new Dictionary<string, string> () {
        { "Source", "*** Your Twilio Number"},
        { "Target", target}
    };

    _connection = _device.Connect (parameters, null);

    if (_connection == null) {
        Log.Info (TAG, "Failed to create connection.");
    }
}
```

Run the app, enter a phone number in the text field and tap the button. You should hear some tones indicating Twilio Client is placing the call and then it will start ringing. Your Android app is now a VoIP phone!

## Other Resources

* [Twilio](http://twilio.com)
* [Twilio Client for Mobile](http://twilio.com/client/mobile)
* [Twilio Client for iOS Bindings](https://github.com/brentschooley/twilio-client-xamarin)
* Need help? Reach out to Brent Schooley on Twitter [@brentschooley](http://twitter.com/brentschooley)