
using System;
using System.Windows;
using System.Runtime.Serialization;
using System.Threading;
using System.Runtime.CompilerServices;

using WPCordovaClassLib.Cordova;
using WPCordovaClassLib.Cordova.Commands;
using WPCordovaClassLib.Cordova.JSON;

using com.nuance.nmdp.speechkit;
using com.nuance.nmdp.speechkit.oem;
using com.nuance.nmdp.speechkit.util;
using com.nuance.nmdp.speechkit.util.audio;
using PhoneGapSpeechTest;

namespace com.nuance.speechkit.phonegap
{

    public delegate void CancelSpeechKitEventHandler();

    /// <summary>
    ///  Sample PhoneGap plugin to call Nuance Speech Kit
    /// </summary>
    public class PhoneGapSpeechPlugin : BaseCommand, RecognizerListener, VocalizerListener
    {
        // Return code constants
	    public const int RC_SUCCESS = 0;
	    public const int RC_FAILURE = -1;
	    public const int RC_NOT_INITIALIZED = -2;
	    public const int RC_RECO_NOT_STARTED = -3;
	    public const int RC_RECO_NO_RESULT_AVAIL = -4;
	    public const int RC_TTS_NOT_STARTED = -5;
	    public const int RC_RECO_FAILURE = -6;
	    public const int RC_TTS_TEXT_INVALID = -7;
	    public const int RC_TTS_PARAMS_INVALID = -8;
	
        // Event constants
	    public const string EVENT_INIT_COMPLETE = "InitComplete";
	    public const string EVENT_CLEANUP_COMPLETE = "CleanupComplete";
        public const string EVENT_RECO_STARTED = "RecoStarted";
	    public const string EVENT_RECO_COMPLETE = "RecoComplete";
	    public const string EVENT_RECO_STOPPED = "RecoStopped";
	    public const string EVENT_RECO_PROCESSING = "RecoProcessing";
	    public const string EVENT_RECO_ERROR = "RecoError";
        public const string EVENT_RECO_ALREADY_STARTED = "RecoAlreadyStarted";
	    public const string EVENT_RECO_VOLUME_UPDATE = "RecoVolumeUpdate";
	    public const string EVENT_TTS_STARTED = "TTSStarted";
	    public const string EVENT_TTS_PLAYING = "TTSPlaying";
	    public const string EVENT_TTS_STOPPED = "TTSStopped";
	    public const string EVENT_TTS_COMPLETE = "TTSComplete";

        // variables to support recognition
        private SpeechKit speechKit = null;
        private Recognizer currentRecognizer = null;
        private Recognition lastResult = null;
        // State variable to track if recording is active
        private bool recording = false;

        // variables to support TTS
        private Vocalizer vocalizerInstance = null;
        private Object _lastTtsContext = null;

        /// <summary>
        /// Object used to store data returned to the javascript layer from a plugin call
        /// </summary>
        [DataContract]
        public class ReturnObject
        {
            [DataMember]
            public int returnCode { get; set; }
            [DataMember]
            public string returnText { get; set; }
            [DataMember]
            public string eventName { get; set; }
            [DataMember]
            public string result { get; set; }
            [DataMember]
            public float volumeLevel { get; set; }
            [DataMember]
            public RecoResult[] results { get; set; }
        }

        /// <summary>
        /// Object to store recognition result data
        /// </summary>
        [DataContract]
        public class RecoResult
        {
            [DataMember]
            public string value { get; set; }
            [DataMember]
            public double confidence { get; set; }

        }

        /// <summary>
        ///  Object containing request parameters for initialization
        /// </summary>
        [DataContract]
        public class InitParameters
        {
            [DataMember]
            public string credentialClassName { get; set; }
            [DataMember]
            public string serverName { get; set; }
            [DataMember]
            public int port { get; set; }
            [DataMember]
            public bool sslEnabled { get; set; }
        }

        /// <summary>
        ///  Object containing request parameters for starting recognition
        /// </summary>
        [DataContract]
        public class StartRecoParameters
        {
            [DataMember]
            public string recognitionType { get; set; }
            [DataMember]
            public string language { get; set; }
        }

        /// <summary>
        /// Object containing request parameters for starting TTS
        /// </summary>
        [DataContract]
        public class StartTTSParameters
        {
            [DataMember]
            public string text { get; set; }
            [DataMember]
            public string language { get; set; }
            [DataMember]
            public string voice { get; set; }
            [DataMember]
            public string textType { get; set; }
        }


        public PhoneGapSpeechPlugin()
        {
            App.CancelSpeechKit += new CancelSpeechKitEventHandler(App_CancelSpeechKit);
        }

        ~PhoneGapSpeechPlugin()
        {
            System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin.destructor: Entered method.");
            try
            {
                if (speechKit != null)
                {
                    speechKit.release();
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin.destructor: Exception releasing speech kit: " + e.ToString());
            }

            App.CancelSpeechKit -= new CancelSpeechKitEventHandler(App_CancelSpeechKit);
        }

        void App_CancelSpeechKit()
        {
            System.Diagnostics.Debug.WriteLine("App_CancelSpeechKit()");
            cleanup();
        }

    

    /// <summary> Method to initialize speech kit</summary>
    /// <param name="args">JSON encoded request parameters</param>
	public void initSpeechKit(string args){

        System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin.initSpeechKit: Entered method.");

        PluginResult result = null;
        InitParameters initParams = JsonHelper.Deserialize<InitParameters>(args);
		
		// Get parameters to do initialization
        string credentialClassName = initParams.credentialClassName;
        System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin.initSpeechKit: init: Credential Class = [" + credentialClassName + "]");
		string serverName = initParams.serverName;
        System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin.initSpeechKit: init: Server = [" + serverName + "]");
		int port = initParams.port;
        System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin.initSpeechKit: init: Port = [" + port + "]");
		bool sslEnabled = initParams.sslEnabled;
        System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin.initSpeechKit: init: SSL = [" + sslEnabled + "]");

		ReturnObject returnObject = new ReturnObject();
		
		try{
	        if (speechKit == null)
	        {		        	
                Type t = Type.GetType(credentialClassName);
			    ICredentials credentials = (ICredentials)Activator.CreateInstance(t);

	        	String appId = credentials.getAppId();
	        	byte[] appKey = credentials.getAppKey();
	        	
                System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin.initSpeechKit: About to initialize");
	            speechKit = SpeechKit.initialize(appId, serverName, port, sslEnabled, appKey);

                //_beep = _speechKit.defineAudioPrompt("beep.wav");
                //_speechKit.setDefaultRecognizerPrompts(_beep, null, null, null);

	            speechKit.connect();
                System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin.initSpeechKit: Connected.");
                Thread.Sleep(10);

	        }
	        
	        setReturnCode(returnObject, RC_SUCCESS, "Init Success");
	        returnObject.eventName = EVENT_INIT_COMPLETE;
			result = new PluginResult(PluginResult.Status.OK, returnObject);
			result.KeepCallback = false;
		}
		catch(Exception e){
            System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin.initSpeechKit: Error initalizing:" + e.ToString());
			setReturnCode(returnObject, RC_FAILURE, e.ToString());
			result = new PluginResult(PluginResult.Status.OK, returnObject);
		}

        DispatchCommandResult(result);
        System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin.initSpeechKit: Leaving method.");
		//return result;
		
	} // end initSpeechKit


    /// <summary> Utility method to clean up speech kit</summary>
    [MethodImpl(MethodImplOptions.Synchronized)]
    protected void cleanup()
    {
        System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin.cleanup: Entered method.");
        if (currentRecognizer != null)
        {
            try
            {
                currentRecognizer.cancel();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin.cleanupSpeechKit: Error cancelling recognizer: " + e.ToString());
            }
        }
        if (vocalizerInstance != null)
        {
            try
            {
                vocalizerInstance.cancel();
                System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin.cleanupSpeechKit: Vocalizer cancelled.");
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin.cleanupSpeechKit: Error cancelling vocalizer: " + e.ToString());
            }
            vocalizerInstance = null;

        }

        if (speechKit != null)
        {
            try
            {
                speechKit.cancelCurrent();
                speechKit.release();
                System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin.cleanupSpeechKit: Speech kit released.");
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin.cleanupSpeechKit: Error releasing speech kit: " + e.ToString());
            }
            speechKit = null;
        }

    }

    
    /// <summary> Method to clean up speech kit variables if they have been intialized</summary>
    /// <param name="args">JSON encoded request parameters</param>
	public void cleanupSpeechKit(string args) {

		System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin.cleanupSpeechKit: Entered method.");
		PluginResult result = null;

        cleanup();

        ReturnObject returnObject = new ReturnObject();
		setReturnCode(returnObject, RC_SUCCESS, "Cleanup Success");			
		returnObject.eventName = EVENT_CLEANUP_COMPLETE;
		result = new PluginResult(PluginResult.Status.OK, returnObject);

        DispatchCommandResult(result);
        System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin.cleanupSpeechKit: Leaving method.");
		//return result;
	}


	
    /// <summary> Method to start speech recognition</summary>
    /// <param name="args">JSON encoded request parameters</param>
	public void startRecognition(string args) {
		
		System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin.startRecognition: Entered method.");
		PluginResult result = null;

        StartRecoParameters startParams = JsonHelper.Deserialize<StartRecoParameters>(args);

        ReturnObject returnObject = new ReturnObject();
		if (speechKit != null){

            if (recording == false)
            {

                // get the recognition type
                String recognitionType = startParams.recognitionType;
                System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin.execute: startReco: Reco Type = [" + recognitionType + "]");
                String recognizerType = RecognizerRecognizerType.Dictation;
                if ("websearch".Equals(recognitionType, StringComparison.CurrentCultureIgnoreCase))
                {
                    recognizerType = RecognizerRecognizerType.Search;
                }
                // get the language
                String language = startParams.language;
                System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin.execute: startReco: Language = [" + language + "]");

                lastResult = null;

                // create and start the recognizer reference
                Thread thread = new Thread(() =>
                {
                    currentRecognizer = speechKit.createRecognizer(recognizerType, RecognizerEndOfSpeechDetection.Long, language, this, null);
                    currentRecognizer.start();
                });
                thread.Start();
                
                //_currentRecognizer = _speechKit.createRecognizer(recognizerType, RecognizerEndOfSpeechDetection.Long, language, this, new object());
                //_currentRecognizer.start();

                System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin.execute: Recognition started.");
                setReturnCode(returnObject, RC_SUCCESS, "Reco Start Success");
                returnObject.eventName = EVENT_RECO_STARTED;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin.execute: Recognition already started.");
                setReturnCode(returnObject, RC_SUCCESS, "Reco Already Active");
                returnObject.eventName = EVENT_RECO_ALREADY_STARTED;
            }
		}
		else
        {
			System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin.execute: Speech kit was null, initialize not called.");
			setReturnCode(returnObject, RC_NOT_INITIALIZED, "Reco Start Failure: Speech Kit not initialized.");			        	
		}
	
		result = new PluginResult(PluginResult.Status.OK, returnObject);
		result.KeepCallback = true;

        DispatchCommandResult(result);
		System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin.startRecognition: Leaving method.");
		//return result;
		
	} // end startRecogition



    /// <summary> Method to stop speech recognition</summary>
    /// <param name="args">JSON encoded request parameters</param>
	public void stopRecognition(string args){
		
		System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin.stopRecognition: Entered method.");
		PluginResult result = null;		
		ReturnObject returnObject = new ReturnObject();

		if (currentRecognizer != null){
			// stop the recognizer
			currentRecognizer.stopRecording();
            System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin.stopRecognition: Recognition started.");
			setReturnCode(returnObject, RC_SUCCESS, "Reco Stop Success");
            returnObject.eventName = EVENT_RECO_STOPPED;
            recording = false;
		}
		else{
            System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin.stopRecognition: Recognizer was null, start not called.");
			setReturnCode(returnObject, RC_RECO_NOT_STARTED, "Reco Stop Failure: Recognizer not started.");
		}

		result = new PluginResult(PluginResult.Status.OK, returnObject);
        result.KeepCallback = true;
        DispatchCommandResult(result);
        System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin.stopRecognition: Leaving method.");
		//return result;
		
	} // end stopRecogition


	

    /// <summary>Retrieves recognition results from the previous recognition</summary>
    /// <param name="args">JSON encoded request parameters</param>
	public void getRecoResult(string args){

        System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin.getRecoResult: Entered method.");
		PluginResult result = null;		
		ReturnObject returnObject = new ReturnObject();

		if (lastResult != null){
			setReturnCode(returnObject, RC_SUCCESS, "Success");
            String resultString = getResultString(lastResult);
            returnObject.result = resultString;
            System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin.getRecoResult: Result string = [" + resultString + "].");
            RecoResult[] resultArray = getResultArray(lastResult);
            returnObject.results = resultArray;
			
		}
		else{
            System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin.getRecoResult: Last result was null.");
			setReturnCode(returnObject, RC_RECO_NO_RESULT_AVAIL, "No result available.");
		}
		
		result = new PluginResult(PluginResult.Status.OK, returnObject);
        DispatchCommandResult(result);

        System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin.getRecoResult: Leaving method.");
		
	} // end getRecoResult
	
	
	
    /// <summary>Starts TTS playback</summary>
    /// <param name="args">JSON encoded request parameters</param>
	public void startTTS(String args){
		
		System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin.startTTS: Entered method.");
		
		PluginResult result = null;
		ReturnObject returnObject = new ReturnObject();
		
        StartTTSParameters startParams = JsonHelper.Deserialize<StartTTSParameters>(args);
        
    	String ttsText = startParams.text;
		System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin.startTTS Text = ["+ttsText+"]");
		String language = startParams.language;
		System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin.startTTS: Language = ["+language+"]");
		String voice = startParams.voice;
		System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin.startTTS: Voice = ["+voice+"]");
		
		if ((ttsText == null) || ("".Equals(ttsText))){
			setReturnCode(returnObject, RC_TTS_TEXT_INVALID, "TTS Text Invalid");
		}
		else
		if ((language == null) && (voice == null)){
			setReturnCode(returnObject, RC_TTS_PARAMS_INVALID, "Invalid language or voice.");
		}
		else
		if (speechKit != null){
			if (vocalizerInstance == null){

				System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin.startTTS: Vocalizer was not null.");

				if (language != null){
					vocalizerInstance = speechKit.createVocalizerWithLanguage(language, this, null);
				}
				else{
					vocalizerInstance = speechKit.createVocalizerWithVoice(voice, this, null);
				}

			}
			else{

                System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin.startTTS: Vocalizer was null.");
				if (language != null){
					vocalizerInstance.setLanguage(language);
				}
				else{
					vocalizerInstance.setVoice(voice);
				}
			
			}

			_lastTtsContext = new Object();
			System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin.startTTS: Calling speakString.");
			vocalizerInstance.speakString(ttsText, _lastTtsContext);
			System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin.startTTS: Called speakString.");

			setReturnCode(returnObject, RC_SUCCESS, "Success");
		}
		else{
			System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin.execute: Speech kit was null, initialize not called.");
			setReturnCode(returnObject, RC_NOT_INITIALIZED, "TTS Start Failure: Speech Kit not initialized.");			        	
		}
		
		result = new PluginResult(PluginResult.Status.OK, returnObject);
		result.KeepCallback = true;
        DispatchCommandResult(result);
		
		System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin.startTTS: Leaving method.");
		
	} // end startTTS
	

    /// <summary>Stops TTS playback</summary>
    /// <param name="args">JSON encoded request parameters</param>
	public void stopTTS(String args){

		System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin.stopTTS: Entered method.");
		PluginResult result = null;
		ReturnObject returnObject = new ReturnObject();
		
		//ttsCallbackId = callbackId;
		
		if (vocalizerInstance != null){
			vocalizerInstance.cancel();
            System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin.stopTTS: Vocalizer cancelled.");
			setReturnCode(returnObject, RC_SUCCESS, "Success");
			returnObject.eventName = EVENT_TTS_COMPLETE;

		}
		else{
			setReturnCode(returnObject, RC_TTS_NOT_STARTED, "TTS Stop Failure: TTS not started.");
		}
		result = new PluginResult(PluginResult.Status.OK, returnObject);
        DispatchCommandResult(result);

		System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin.stopTTS: Leaving method.");
		
	} // end stopTTS



    /// <summary>Sets the return code and text into the return object passed.</summary>
    /// <param name="returnObject">Return object to set</param>
    /// <param name="returnCode">Return code to set</param>
    /// <param name="returnText">Return text to set</param>
	private void setReturnCode(ReturnObject returnObject, int returnCode, String returnText) {
		returnObject.returnCode = returnCode;
		returnObject.returnText = returnText;
	} // end setReturnCode
	

    
    /// <summary>Creates a RecoResult array representation of the results passed in to be returned to the front end</summary>
    /// <param name="results">Recognition result data</param>
    private RecoResult[] getResultArray(Recognition results){

        RecoResult[] resultArray = null;

    	int resultCount = results.getResultCount(); 
        if (resultCount > 0)
        {
            resultArray = new RecoResult[resultCount];
            System.Diagnostics.Debug.WriteLine("Recognizer.Listener.onResults: Result count: " + resultCount);

            for (int i = 0; i < resultCount; i++)
            {
                resultArray[i] = new RecoResult();
                resultArray[i].value = results.getResult(i).getText();
            	resultArray[i].confidence = results.getResult(i).getScore();
            }
        } 
        return resultArray;
    }


    /// <summary>Utility method to get the first text result from recognition</summary>
    /// <param name="results">Recognition result data</param>
    private String getResultString(Recognition results)
    {
        String output = "";

        if (results.getResultCount() > 0)
        {
            output = results.getResult(0).getText();

            //for (int i = 0; i < results.length; i++)
            //    _.add("[" + results[i].getScore() + "]: " + results[i].getText());
        }
        return output;

    }


            /// <summary>Callback method for recording started event</summary>
            public void onRecordingBegin(Recognizer recognizer) 
            {
                System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin: PhoneGapSpeechPlugin: Recognizer.Listener.onRecordingBegin: Entered method.");
            	System.Diagnostics.Debug.WriteLine("Recording...");
            	recording = true;
            	
            	try{
	            	ReturnObject returnObject = new ReturnObject();
					setReturnCode(returnObject, RC_SUCCESS, "Recording Started");
					returnObject.eventName = EVENT_RECO_STARTED;
					
		            PluginResult result = new PluginResult(PluginResult.Status.OK, returnObject);
		            result.KeepCallback = true;
                    System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin: PhoneGapSpeechPlugin: Recognizer.Listener.onRecordingBegin: Reco Started... ");
                    DispatchCommandResult(result);

	            }
	            catch(Exception e){
	                System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin: Recognizer.Listener.onRecordingBegin: Error setting return: "+e.ToString());
	            }

                Thread thread = new Thread(() =>
                {
                    System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin: Volume level thread starting.");
                    while ((currentRecognizer != null) && (recording == true))
                    {
                        try
                        {
                            ReturnObject returnObject = new ReturnObject();

                            returnObject.eventName = EVENT_RECO_VOLUME_UPDATE;
                            returnObject.volumeLevel =  currentRecognizer.getAudioLevel();

                            PluginResult result = new PluginResult(PluginResult.Status.OK, returnObject);
                            result.KeepCallback = true;

                            DispatchCommandResult(result);

                        }
                        catch (Exception e)
                        {
                            System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin: Recognizer.Listener.onRecordingDone: Error setting return: " + e.ToString());
                        }
                        //_listeningDialog.setLevel(Float.toString(_currentRecognizer.getAudioLevel()));
                        Thread.Sleep(500);
                    }
                    System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin: Volume level thread ending.");

                });
                thread.Start();

                System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin: Recognizer.Listener.onRecordingBegin: Leaving method.");
            }

            /// <summary>Callback method for recording done event</summary>
            public void onRecordingDone(Recognizer recognizer) 
            {
                System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin: Recognizer.Listener.onRecordingDone: Entered method.");
            	recording = false;
            	try{
	            	ReturnObject returnObject = new ReturnObject();
					setReturnCode(returnObject, RC_SUCCESS, "Processing");
					returnObject.eventName = EVENT_RECO_PROCESSING;

					PluginResult result = new PluginResult(PluginResult.Status.OK, returnObject);
		            result.KeepCallback = true;
		            System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin: Recognizer.Listener.onRecordingDone: Reco Done... success:");
                    DispatchCommandResult(result);


                }
	            catch(Exception e){
	                System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin: Recognizer.Listener.onRecordingDone: Error setting return: "+e);
	            }
                System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin: Recognizer.Listener.onRecordingDone: Leaving method.");
            }

            /// <summary>Callback method for recording error event</summary>
            public void onError(Recognizer recognizer, SpeechError error) {

                System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin: Recognizer.Listener.onError: Entered method.");
            	if (recognizer != currentRecognizer) return;
            	currentRecognizer = null;
            	recording = false;

                
                // Display the error + suggestion in the edit box
                String detail = error.getErrorDetail();
                String suggestion = error.getSuggestion();
                System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin: Recognizer.Listener.onError: Detail = " + detail + "Suggestion = "+suggestion);

                ReturnObject returnObject = new ReturnObject();
                try{
					setReturnCode(returnObject, RC_RECO_FAILURE, "Reco Failure");
					returnObject.eventName = EVENT_RECO_ERROR;
					returnObject.result = detail;
                }
                catch(Exception e){
                    System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin: Recognizer.Listener.onError: Error storing results: " + e);
                }
				PluginResult result = new PluginResult(PluginResult.Status.OK, returnObject);
                DispatchCommandResult( result );
                
                // for debugging purpose: printing out the speechkit session id
                System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin: Recognizer.Listener.onError: Leaving method.");
            }


            /// <summary>Callback method for recognition complete with results</summary>
            public void onResults(Recognizer recognizer, Recognition results) {

                System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin: Recognizer.Listener.onResults: Entered method.");
                currentRecognizer = null;
                recording = false;
                
                lastResult = results;
                string resultString = results.getResult(0).getText();

                ReturnObject returnObject = new ReturnObject();
                try{
					setReturnCode(returnObject, RC_SUCCESS, "Reco Success");
					returnObject.eventName = EVENT_RECO_COMPLETE;
                    returnObject.result = resultString;
                    returnObject.results = getResultArray(results);
                	//returnObject.put("results", resultArray);
                }
                catch(Exception je){
                    System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin: Recognizer.Listener.onResults: Error storing results: " + je.ToString());
                }
                //success(returnObject, recognitionCallbackId);
                PluginResult result = new PluginResult(PluginResult.Status.OK, returnObject);
                DispatchCommandResult(result);
                
                // for debugging purpose: printing out the speechkit session id
                System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin: Recognizer.Listener.onResults: Leaving method.  Results = " + resultString);
            }

            /// <summary>Callback method for TTS starting to speak</summary>
	        public void onSpeakingBegin(Vocalizer vocalizer, String text, Object context) {

                System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin: Vocalizer.Listener.onSpeakingBegin: Entered method");

                ReturnObject returnObject = new ReturnObject();

				setReturnCode(returnObject, RC_SUCCESS, "TTS Playback Started");
				returnObject.eventName = EVENT_TTS_STARTED;
				System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin: Vocalizer.Listener.onSpeakingDone: TTS Started...");

                PluginResult result = new PluginResult(PluginResult.Status.OK, returnObject);
                result.KeepCallback = true;
                DispatchCommandResult(result);
	        	
	            System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin: Vocalizer.Listener.onSpeakingBegin: Leaving method.");
	        }

            /// <summary>Callback method for TTS speaking done</summary>
	        public void onSpeakingDone(Vocalizer vocalizer, String text, SpeechError error, Object context) 
	        {
                System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin: Vocalizer.Listener.onSpeakingDone: Context = [" + context + "], last context = " + _lastTtsContext);
	            // Use the context to detemine if this was the final TTS phrase
	            if (context != _lastTtsContext)
	            {
                    ReturnObject returnObject = new ReturnObject();

					setReturnCode(returnObject, RC_SUCCESS, "TTS Playing...");
					returnObject.eventName = EVENT_TTS_PLAYING;

                    System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin: Vocalizer.Listener.onSpeakingDone: TTS Playing...");

	                PluginResult result = new PluginResult(PluginResult.Status.OK, returnObject);
	                result.KeepCallback = true;
                    DispatchCommandResult(result);

	            }
	            else
	            {

                    ReturnObject returnObject = new ReturnObject();

					setReturnCode(returnObject, RC_SUCCESS, "TTS Playback Complete");
					returnObject.eventName = EVENT_TTS_COMPLETE;
                    System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin: Vocalizer.Listener.onSpeakingDone: TTS Complete...");

                    PluginResult result = new PluginResult(PluginResult.Status.OK, returnObject);
                    DispatchCommandResult(result);

	            }

                System.Diagnostics.Debug.WriteLine("PhoneGapSpeechPlugin: Vocalizer.Listener.onSpeakingDone: Leaving method.");

	        }


    }
}