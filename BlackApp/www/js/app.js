// Ionic Starter App

// angular.module is a global place for creating, registering and retrieving Angular modules
// 'starter' is the name of this angular module example (also set in a <body> attribute in index.html)
// the 2nd parameter is an array of 'requires'
angular.module('blackapp', ['ionic', 'ngCordova', 'ngConstellation'])

.run(function ($ionicPlatform) {
    $ionicPlatform.ready(function () {
        if (cordova.platformId === 'ios' && window.cordova && window.cordova.plugins.Keyboard) {
            // Hide the accessory bar by default (remove this to show the accessory bar above the keyboard
            // for form inputs)
            cordova.plugins.Keyboard.hideKeyboardAccessoryBar(true);

            // Don't remove this line unless you know what you are doing. It stops the viewport
            // from snapping when text inputs are focused. Ionic handles this internally for
            // a much nicer keyboard experience.
            cordova.plugins.Keyboard.disableScroll(true);
        }
        if (window.StatusBar) {
            StatusBar.styleDefault();
        }
    });
})

.controller('BlackCtrl', ['$scope', '$cordovaDeviceMotion', 'constellationConsumer', '$timeout',
    function ($scope, $cordovaDeviceMotion, constellation, $timeout) {
        var myrate = 1;                 //vitesse d'élocution pour le tts
        var reason = "";
        $scope.state = false;
        //constellation.intializeClient("http://192.168.43.32:8088", "21affda431649385c6ff45c10f7043b46d09d821", "BlackClient");
        constellation.intializeClient("http://192.168.0.12:8088", "21affda431649385c6ff45c10f7043b46d09d821", "BlackClient");

        constellation.connect();

        $scope.runAcc = function () {
            $scope.state = true;

            var options = {
                frequency: 500
            };
            $scope.watch = $cordovaDeviceMotion.watchAcceleration(options);
            $scope.watch.then(
                null,
                function (error) {
                },
                function (result) {
                    $scope.X = result.x;
                    $scope.Y = result.y;
                    $scope.Z = result.z;
                    constellation.sendMessage({ Scope: 'Package', Args: ['BlackConnector'] }, 'SOModifier', ['accelerometer', { "State": $scope.state, "X": $scope.X, "Y": $scope.Y, "Z": $scope.Z }]);
                });
        };

        $scope.stopAcc = function () {
            $scope.state = false;
            $scope.watch.clearWatch();
            $scope.X = 0;
            $scope.Y = 0;
            $scope.Z = 0;
            constellation.sendMessage({ Scope: 'Package', Args: ['BlackConnector'] }, 'SOModifier', ['accelerometer', { "State": $scope.state, "X": $scope.X, "Y": $scope.Y, "Z": $scope.Z }]);
        };

        constellation.onConnectionStateChanged(function (change) {
            if (change.newState === $.signalR.connectionState.connected) {
                constellation.requestSubscribeStateObjects("*", "BlackHole", "*", "*");
                constellation.sendMessage({ Scope: 'Package', Args: ['BlackConnector'] }, 'SOModifier', ['accelerometer', { "State": $scope.state, "X": 0, "Y": 0, "Z": 0 }]);

            }
        });

        constellation.onUpdateStateObject(function (stateobject) {
            $scope.$apply(function () {
                if (stateobject.Name === "TextToSpeech") {
                    textTo(stateobject.Value.text);
                }
                if (stateobject.Name === "NeedRecognition") {
                    reason = stateobject.Value.Reason;
                    recognition.start();
                }
            })

        })

        function textTo(message) {          //fonction à appeler pour faire parler le device
            TTS.speak({
                text: message,
                locale: 'fr-FR',
                rate: myrate
            });
        }

        RetourRecognitionResult = function (textReco) {         // Fonction qui renvoie les resultats de la voice recognition dans BlackHole 

            constellation.sendMessage({ Scope: 'Package', Args: ['BlackHole'] }, 'UseRecognition', [reason, textReco]);
            constellation.sendMessage({ Scope: 'Package', Args: ['BlackConnector'] }, 'SOModifier', ['RecognitionResult', { "Reason": reason, "Text": textReco }]); // SO pour vérifier la Voice Recognition

        }
    }]);

//var reason = "";
var recognition;
document.addEventListener('deviceready', onDeviceReady, false);

function onDeviceReady() {                                        // recognition.start() pour démarrer cette fonction (reconnaissance vocale)
    recognition = new SpeechRecognition();
    recognition.lang = 'fr-Fr';
    recognition.onresult = function (event) {
        if (event.results.length > 0) {
            var text = event.results[0][0].transcript;
        }
        RetourRecognitionResult(text);
    }
}