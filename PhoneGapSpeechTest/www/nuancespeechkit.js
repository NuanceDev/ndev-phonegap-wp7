/**
 *  
 * @return Object literal singleton instance of DirectoryListing
 */
var NuanceSpeechKitPlugin = function() {
};

/**
  * @param 
  * @param successCallback The callback which will be called when directory listing is successful
  * @param failureCallback The callback which will be called when directory listing encouters an error
  */
NuanceSpeechKitPlugin.prototype.initialize = function (inCredentialClassName, inServerName, inPort, inSslEnabled, successCallback, failureCallback) {
    return cordova.exec(successCallback,
                        failureCallback,
                        "com.nuance.speechkit.phonegap.PhoneGapSpeechPlugin",
                        "initSpeechKit",
                        { credentialClassName : inCredentialClassName, serverName : inServerName, port : inPort, sslEnabled : inSslEnabled });
};

NuanceSpeechKitPlugin.prototype.cleanup = function(successCallback, failureCallback) {
    return cordova.exec(successCallback,    //Success callback from the plugin
	                    failureCallback,     //Error callback from the plugin
	                    'com.nuance.speechkit.phonegap.PhoneGapSpeechPlugin',  //The plugin
	                    'cleanupSpeechKit',              //Tell plugin, which action we want to perform
	                    null);        //Passing list of args to the plugin
};


NuanceSpeechKitPlugin.prototype.startRecognition = function (inRecoType, inLanguage, successCallback, failureCallback) {
    return PhoneGap.exec(successCallback,    //Success callback from the plugin
		                 failureCallback,     //Error callback from the plugin
		                 'com.nuance.speechkit.phonegap.PhoneGapSpeechPlugin',  //The plugin
		                 'startRecognition',              //Tell plugin, which action we want to perform
		                 {recognitionType: inRecoType, language: inLanguage });        //Passing list of args to the plugin
};

NuanceSpeechKitPlugin.prototype.stopRecognition = function (successCallback, failureCallback) {
    return PhoneGap.exec(successCallback,    //Success callback from the plugin
	                    failureCallback,     //Error callback from the plugin
	                    'com.nuance.speechkit.phonegap.PhoneGapSpeechPlugin',  //The plugin
	                    'stopRecognition',              //Tell plugin, which action we want to perform
	                    null);        //Passing list of args to the plugin
};


NuanceSpeechKitPlugin.prototype.getResults = function (successCallback, failureCallback) {
    return PhoneGap.exec(successCallback,    //Success callback from the plugin
	      failureCallback,     //Error callback from the plugin
	      'com.nuance.speechkit.phonegap.PhoneGapSpeechPlugin',  //The plugin
	      'getRecoResult',              //Tell plugin, which action we want to perform
	      null);        //Passing list of args to the plugin
};

NuanceSpeechKitPlugin.prototype.playTTS = function (inText, inLanguage, inVoice, successCallback, failureCallback) {
    return PhoneGap.exec(successCallback,    //Success callback from the plugin
	      failureCallback,     //Error callback from the plugin
	      'com.nuance.speechkit.phonegap.PhoneGapSpeechPlugin',  //The plugin
	      'startTTS',              //Tell plugin, which action we want to perform
	      {text : inText, language : inLanguage, voice: inVoice});        //Passing list of args to the plugin
};

NuanceSpeechKitPlugin.prototype.stopTTS = function (successCallback, failureCallback) {
    return PhoneGap.exec(successCallback,    //Success callback from the plugin
	      failureCallback,     //Error callback from the plugin
	      'com.nuance.speechkit.phonegap.PhoneGapSpeechPlugin',  //The plugin
	      'stopTTS',              //Tell plugin, which action we want to perform
	      null);        //Passing list of args to the plugin
};



/*
NuanceSpeechKitPlugin.prototype.initialize = function(credentialClassName, serverName, port, sslEnabled, successCallback, failureCallback) {
 return PhoneGap.exec( successCallback,    								//Success callback from the plugin
		 			   failureCallback,     							//Error callback from the plugin
		 			   'NuanceSpeechKitPlugin',  						//The plugin
		 			   'init',              							//Tell plugin, which action we want to perform
		 			   [credentialClassName, serverName, port, sslEnabled]);  //Passing list of args to the plugin
};

NuanceSpeechKitPlugin.prototype.cleanup = function(successCallback, failureCallback) {
	 return PhoneGap.exec(    successCallback,    //Success callback from the plugin
	      failureCallback,     //Error callback from the plugin
	      'NuanceSpeechKitPlugin',  //The plugin
	      'cleanup',              //Tell plugin, which action we want to perform
	      []);        //Passing list of args to the plugin
};

NuanceSpeechKitPlugin.prototype.startRecognition = function(recoType, language, successCallback, failureCallback) {
		 return PhoneGap.exec(    successCallback,    //Success callback from the plugin
		      failureCallback,     //Error callback from the plugin
		      'NuanceSpeechKitPlugin',  //The plugin
		      'startReco',              //Tell plugin, which action we want to perform
		      [recoType, language]);        //Passing list of args to the plugin
};

NuanceSpeechKitPlugin.prototype.stopRecognition = function(successCallback, failureCallback) {
	 return PhoneGap.exec(    successCallback,    //Success callback from the plugin
	      failureCallback,     //Error callback from the plugin
	      'NuanceSpeechKitPlugin',  //The plugin
	      'stopReco',              //Tell plugin, which action we want to perform
	      []);        //Passing list of args to the plugin
};

NuanceSpeechKitPlugin.prototype.getResults = function(successCallback, failureCallback) {
	 return PhoneGap.exec(    successCallback,    //Success callback from the plugin
	      failureCallback,     //Error callback from the plugin
	      'NuanceSpeechKitPlugin',  //The plugin
	      'getRecoResult',              //Tell plugin, which action we want to perform
	      []);        //Passing list of args to the plugin
};

NuanceSpeechKitPlugin.prototype.playTTS = function(text, language, voice, successCallback, failureCallback) {
	 return PhoneGap.exec(    successCallback,    //Success callback from the plugin
	      failureCallback,     //Error callback from the plugin
	      'NuanceSpeechKitPlugin',  //The plugin
	      'playTTS',              //Tell plugin, which action we want to perform
	      [text, language, voice]);        //Passing list of args to the plugin
};

NuanceSpeechKitPlugin.prototype.stopTTS = function(successCallback, failureCallback) {
	 return PhoneGap.exec(    successCallback,    //Success callback from the plugin
	      failureCallback,     //Error callback from the plugin
	      'NuanceSpeechKitPlugin',  //The plugin
	      'stopTTS',              //Tell plugin, which action we want to perform
	      []);        //Passing list of args to the plugin
};

NuanceSpeechKitPlugin.prototype.queryNextEvent = function(successCallback, failureCallback) {
	 return PhoneGap.exec(    successCallback,    //Success callback from the plugin
	      failureCallback,     //Error callback from the plugin
	      'NuanceSpeechKitPlugin',  //The plugin
	      'queryNextEvent',              //Tell plugin, which action we want to perform
	      []);        //Passing list of args to the plugin
};
*/

//cordova.addConstructor(function() {
//    cordova.addPlugin('NuanceSpeechKitPlugin', new NuanceSpeechKitPlugin());
//});