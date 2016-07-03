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
        //vitesse d'élocution pour le tts
        var myrate = 1;                 
        var reason = "";
        $scope.state = false;
        constellation.intializeClient("http://AdresseIPduServeur:8088", "CREDENTIALS CONSTELLATION", "BlackClient"); // A configurer selon votre installation de Constellation

        constellation.connect();

        // Lance la lecture de l'accéléromètre et envoie les données sous forme d'un SO
        $scope.runAcc = function () {
            $scope.state = true;

            // Fréquence de mise à jour des données de l'accéléromètre
            var options = {
                frequency: 500 // Relevé deux fois par secondes
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
        // Stoppe la lecture de l'accéléromètre
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
                // On s'abonne aux SO TextToSpeech et NeedRecognition du package BlackHole
                constellation.subscribeStateObjects("*", "BlackHole", "*", "*");
                // Initialisation des données de l'accéléromètre
                constellation.sendMessage({ Scope: 'Package', Args: ['BlackConnector'] }, 'SOModifier', ['accelerometer', { "State": $scope.state, "X": 0, "Y": 0, "Z": 0 }]);

            }
        });

        constellation.onUpdateStateObject(function (stateobject) {
            $scope.$apply(function () {
                // Cas SO mis à jour est TextToSpeech 
                if (stateobject.Name === "TextToSpeech") {
                    // Lance l'annonce du text par téléphone
                    textTo(stateobject.Value.text);
                }
                // Cas SO mis à jour est NeedRecognition 
                if (stateobject.Name === "NeedRecognition") {
                    reason = stateobject.Value.Reason;
                    // Lance la reconnaissance vocale
                    recognition.start();
                }
            })

        })
        // Fonction à appeler pour faire parler le device
        function textTo(message) {          
            TTS.speak({
                text: message,
                locale: 'fr-FR',
                rate: myrate
            });
        }
        // Fonction qui renvoie les resultats de la voice recognition dans le package BlackHole 
        RetourRecognitionResult = function (textReco) {         

            constellation.sendMessage({ Scope: 'Package', Args: ['BlackHole'] }, 'UseRecognition', [reason, textReco]);
        }
    }]);

// Fonction de reconnaissance vocale
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