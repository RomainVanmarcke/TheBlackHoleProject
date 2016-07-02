using Constellation;
using Constellation.Package;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackHole
{
    public class Program : PackageBase
    {
        static void Main(string[] args)
        {
            PackageHost.Start<Program>(args);
        }

        //Abonnement aux données de l'accelerometre
        [StateObjectLink("BlackConnector", "accelerometer")]
        private StateObjectNotifier Acc { get; set; }

        // Abonnements aux state objects info
        [StateObjectLink("ROMAIN-MSI", "HWMonitor", "/intelcpu/0/temperature/0")]
        private StateObjectNotifier tempCPU { get; set; }

        [StateObjectLink("ROMAIN-MSI", "HWMonitor", "/intelcpu/0/load/0")]
        private StateObjectNotifier loadCPU { get; set; }

        [StateObjectLink("ROMAIN-MSI", "HWMonitor", "/ram/load/0")]
        private StateObjectNotifier loadRAM { get; set; }

        [StateObjectLink("ROMAIN-MSI", "ForecastIO", "Lille")]
        private StateObjectNotifier meteo { get; set; }

        [StateObjectLink("ROMAIN-MSI", "DayInfo", "NameDay")]
        private StateObjectNotifier nameday { get; set; }

        [StateObjectLink("ROMAIN-MSI", "DayInfo", "SunInfo")]
        private StateObjectNotifier suninfo { get; set; }


        //creation de variables stockant les resultats de reconnaissance vocale
        public class RecognitionResult
        {
            public string arg1 { get; set; }
            public string arg2 { get; set; }
            public string arg3 { get; set; }
            public string arg4 { get; set; }
        }

        RecognitionResult ratpTrafficArg = new RecognitionResult();
        RecognitionResult ratpPlanningArg = new RecognitionResult();
        RecognitionResult googleTrafficArg = new RecognitionResult();
        RecognitionResult pushbulletArg = new RecognitionResult();
        RecognitionResult settingsArg = new RecognitionResult();

        bool HWM;
        bool DI;
        bool FIO;
        public override void OnStart()
        {
            //Initialisations Etat du menu et settings
            string menu = "home";
            HWM = false;
            DI = true;
            FIO = true;


            //Menu
            this.Acc.ValueChanged += (s, e) =>
            {
                if (Acc.DynamicValue.State == true)
                {
                    //valeurs de l'accelerometre
                    double x = (Acc.DynamicValue.X);
                    double y = (Acc.DynamicValue.Y);
                    double z = (Acc.DynamicValue.Z);
                    double xabs = Math.Abs(x);
                    double yabs = Math.Abs(y);
                    double zabs = Math.Abs(z);

                    // Mouvement retourne
                    if (xabs < 5 && yabs < 2 && z < -8)
                    {
                        switch (menu)
                        {
                            case "home":
                                PackageHost.PushStateObject("TextToSpeech", new { text = "Raiglage" });
                                Thread.Sleep(1000);
                                PackageHost.PushStateObject("NeedRecognition", new { Reason = "settings" });
                                break;
                            default:
                                PackageHost.PushStateObject("TextToSpeech", new { text = "Accueil" });
                                menu = "home";
                                break;
                        }
                    }
                    // Mouvement mise a plat
                    else if (xabs < 5 && yabs < 2 && z > 8)
                    {
                        switch (menu)
                        {
                            case "home":
                                PackageHost.PushStateObject("TextToSpeech", new { text = "Voici les infos, " });
                                Info(HWM, FIO, DI);
                                break;
                            default:
                                break;
                        }
                    }
                    // Mouvement incline gauche
                    else if (x > 5 && y < 6 && zabs < 4)
                    {
                        switch (menu)
                        {
                            case "home":
                                PackageHost.PushStateObject("TextToSpeech", new { text = "Requete" });
                                menu = "Request";
                                break;
                            case "Request":
                                PackageHost.PushStateObject("TextToSpeech", new { text = "menu R A T P" });
                                menu = "ratp";
                                break;
                            case "ratp":
                                PackageHost.PushStateObject("TextToSpeech", new { text = "Trafic R A T P, type" });
                                Thread.Sleep(2000);
                                PackageHost.PushStateObject("NeedRecognition", new { Reason = "ratpTraffic1" });
                                menu = "Home";
                                break;
                            default:
                                break;
                        }
                    }
                    // Mouvement incline droit
                    else if (x < (-5) && y < 6 && zabs < 4)
                    {
                        switch (menu)
                        {
                            case "home":
                                PackageHost.PushStateObject("TextToSpeech", new { text = "Peush Boulette" });
                                Thread.Sleep(1000);
                                PackageHost.PushStateObject("NeedRecognition", new { Reason = "pushbullet" });
                                break;
                            case "Request":
                                PackageHost.PushStateObject("TextToSpeech", new { text = "gougueule trafic, daipart" });
                                Thread.Sleep(2500);
                                PackageHost.PushStateObject("NeedRecognition", new { Reason = "googleTraffic1" });
                                menu = "Home";
                                break;
                            case "ratp":
                                PackageHost.PushStateObject("TextToSpeech", new { text = "planning R A T P, type" });
                                Thread.Sleep(2000);
                                PackageHost.PushStateObject("NeedRecognition", new { Reason = "ratpPlanning1" });
                                menu = "Home";
                                break;
                            default:
                                break;
                        }
                    }
                    //else
                    //{

                    //}
                }
            };
        }
        //Fin du menu


        //Stocke le resultat de la reconaissance vocale
        [MessageCallback(IsHidden = true)]
        void UseRecognition(string reason, string result)
        {
            switch (reason)
            {
                case "settings":
                    settingsArg.arg1 = result;
                    Settings();
                    break;
                case "ratpTraffic1":
                    ratpTrafficArg.arg1 = result;
                    PackageHost.PushStateObject("TextToSpeech", new { text = "ligne" });
                    Thread.Sleep(1000);
                    PackageHost.PushStateObject("NeedRecognition", new { Reason = "ratpTraffic2" });
                    break;
                case "ratpTraffic2":
                    ratpTrafficArg.arg2 = Converter(result);
                    RatpGetTraffic();
                    break;
                case "ratpPlanning1":
                    ratpPlanningArg.arg1 = result;
                    PackageHost.PushStateObject("TextToSpeech", new { text = "ligne" });
                    Thread.Sleep(1000);
                    PackageHost.PushStateObject("NeedRecognition", new { Reason = "ratpPlanning2" });
                    break;
                case "ratpPlanning2":
                    ratpPlanningArg.arg2 = Converter(result);
                    PackageHost.PushStateObject("TextToSpeech", new { text = "station" });
                    Thread.Sleep(1000);
                    PackageHost.PushStateObject("NeedRecognition", new { Reason = "ratpPlanning3" });
                    break;
                case "ratpPlanning3":
                    ratpPlanningArg.arg3 = result;
                    PackageHost.PushStateObject("TextToSpeech", new { text = "destination" });
                    Thread.Sleep(1000);
                    PackageHost.PushStateObject("NeedRecognition", new { Reason = "ratpPlanning4" });
                    break;
                case "ratpPlanning4":
                    ratpPlanningArg.arg4 = result;
                    RatpGetSchedule();
                    break;
                case "pushbullet":
                    pushbulletArg.arg1 = result;
                    PushBullet();
                    break;
                case "googleTraffic1":
                    googleTrafficArg.arg1 = result;
                    PackageHost.PushStateObject("TextToSpeech", new { text = "destination" });
                    Thread.Sleep(1000);
                    PackageHost.PushStateObject("NeedRecognition", new { Reason = "googleTraffic2" });
                    break;
                case "googleTraffic2":
                    googleTrafficArg.arg2 = result;
                    GoogleTraffic();
                    break;
                default:
                    break;
            }
        }

        string Converter(string result)
        {
            if (result.Contains("un"))
            {
                return "1";
            }
            else if (result.Contains("deux"))
            {
                return "2";
            }
            else if (result.Contains("trois"))
            {
                return "3";
            }
            else if (result.Contains("quatre"))
            {
                return "4";
            }
            else if (result.Contains("cinq"))
            {
                return "5";
            }
            else if (result.Contains("six"))
            {
                return "6";
            }
            else if (result.Contains("sept"))
            {
                return "7";
            }
            else if (result.Contains("huit"))
            {
                return "8";
            }
            else if (result.Contains("neuf"))
            {
                return "9";
            }
            else if (result.Contains("dix"))
            {
                return "10";
            }
            else if (result.Contains("onze"))
            {
                return "11";
            }
            else
            {
                return "12";
            }
        }

        void Settings()
        {
            string text = settingsArg.arg1;
            text.ToLower();
            DI = false;
            FIO = false;
            HWM = false;
            if (text.Contains("info") && text.Contains("jour"))
            {
                DI = true;
            }
            if (text.Contains("info") && text.Contains("pc"))
            {
                HWM = true;
            }
            if (text.Contains("forecast"))
            {
                FIO = true;
            }
        }

        //Envoie les infos à l'application via StateObject
        void Info(bool hwm, bool fio, bool di)
        {
            string annonce = "";
            if (hwm)
            {
                annonce += Requete("HWMonitor");
            }
            if (fio)
            {
                annonce += Requete("ForecastIO");
            }
            if (di)
            {
                annonce += Requete("DayInfo");
            }
            if (annonce == "")
            {
                annonce = "aucun package choisit";
            }
            PackageHost.PushStateObject("TextToSpeech", new { text = annonce });
        }

        //Met en forme la phrase pour chaque package
        private string Requete(string pack)
        {
            string text = "";
            switch (pack)
            {
                case "ForecastIO":
                    string resume = tempsbdd();
                    text = $"il fait {meteo.DynamicValue.currently.temperature}° à {meteo.Value.Name} , {resume}. ";
                    break;
                case "HWMonitor":
                    text = $"la tempairature du processeur est de {tempCPU.DynamicValue.Value}°, son utilisation est de {Math.Round(System.Convert.ToDouble(loadCPU.DynamicValue.Value), 2)} {loadCPU.DynamicValue.Unit}. ";
                    break;
                case "DayInfo":
                    text = dayInfo();
                    break;
            }
            return text;
        }

        //Gere la mise en forme particuliere de dayInfo
        private string dayInfo()
        {
            string fete = "";
            if (nameday.DynamicValue.Contains("Ste"))
            {
                fete = nameday.DynamicValue.Remove(0, 4);
                return $"Aujourd'hui c'est la sainte {fete}, le soleil se lève à {suninfo.DynamicValue.Sunrise} et se couche à {suninfo.DynamicValue.Sunset}. ";
            }
            else
            {
                fete = nameday.DynamicValue.Remove(0, 3);
                return $"Aujourd'hui c'est la saint {fete}, le soleil se lève à {suninfo.DynamicValue.Sunrise} et se couche à {suninfo.DynamicValue.Sunset}. ";
            }
        }

        //traductions Anglais -> Fracais de la meteo
        private string tempsbdd()
        {
            string s = meteo.DynamicValue.currently.summary;
            string c = "";
            if (s.Contains("Mostly Cloudy"))
            {
                c = "Mostly Cloudy";
                return $"Le temps est plutôt nuageux {tempsbdd2(c)}";
            }
            else if (s.Contains("Overcast"))
            {
                c = "Overcast";
                return $"Le temps est couvert {tempsbdd2(c)}";
            }
            else if (s.Contains("Drizzle"))
            {
                c = "Drizzle";
                return $"il y a une légère bruine {tempsbdd2(c)}";
            }
            else if (s.Contains("Foggy"))
            {
                c = "Foggy";
                return $"Il y a du brouillard à couper au couteau {tempsbdd2(c)}";
            }
            else if (s.Contains("Breezy"))
            {
                c = "Breezy";
                return $"Il y a beaucoup de vent {tempsbdd2(c)}";
            }
            else if (s.Contains("Clear"))
            {
                c = "Clear";
                return $"Le ciel est dégagé {tempsbdd2(c)}";
            }
            else if (s.Contains("Partly Cloudy"))
            {
                c = "Partly Cloudy";
                return $"Le ciel est partiellement couvert {tempsbdd2(c)}";
            }
            else if (s.Contains("Light Rain"))
            {
                c = "Light Rain";
                return "Il pleut légèrement";
            }
            else
            {
                return meteo.DynamicValue.currently.summary;
            }
        }
        private string tempsbdd2(string chaine)
        {
            string s = meteo.DynamicValue.currently.summary;
            if (s.Contains("Mostly Cloudy") && chaine != "Mostly Cloudy")
            {
                return "Et le temps est plutôt nuageux. ";
            }
            else if (s.Contains("Overcast") && chaine != "Overcast")
            {
                return "Et le temps est couvert. ";
            }
            else if (s.Contains("Drizzle") && chaine != "Drizzle")
            {
                return "Et il y a une légère bruine. ";
            }
            else if (s.Contains("Foggy") && chaine != "Foggy")
            {
                return "Et il y a du brouillard à couper au couteau. ";
            }
            else if (s.Contains("Breezy") && chaine != "Breezy")
            {
                return "Et il y a beaucoup de vent. ";
            }
            else if (s.Contains("Clear") && chaine != "Clear")
            {
                return $" Le ciel est dégagé";
            }
            else if (s.Contains("Partly Cloudy") && chaine != "Partly Cloudy")
            {
                return "Et le ciel est partiellement couvert. ";
            }
            else if (s.Contains("Light Rain") && chaine != "Light Rain")
            {
                return " Et il pleut légèrement. ";
            }
            else
            {
                return "";
            }
        }

        void RatpGetSchedule()
        {
            string annonce = "les prochaines arrivees: ";
            string s;
            MessageScope.Create("Ratp").OnSagaResponse(result =>
            {
                for (int i = 0; i < 4; i++)
                {
                    s = result.Data[i].message;
                    if (s.Contains("mn"))
                    {
                        s = $"{s[0]}{s[1]} minutes";
                    };
                    annonce = $"{annonce} {s}, ";
                }
            }).GetProxy().GetSchedule(new { type = ratpPlanningArg.arg1, line = ratpPlanningArg.arg2, station = ratpPlanningArg.arg3, direction = ratpPlanningArg.arg4 });
            PackageHost.PushStateObject("TextToSpeech", new { text = annonce });
        }

        void RatpGetTraffic()
        {
            string annonce = "Je n'ai pas reçu de réponse";
            PackageHost.WriteInfo($"J'ai exécuté la fonction RatpGetTraffic avec comme type: { ratpTrafficArg.arg1} et comme ligne: {ratpTrafficArg.arg2}");
            MessageScope.Create("Ratp").OnSagaResponse(result =>
            {
                PackageHost.WriteInfo($"le resultat de RATP traffic est {result}");
                annonce = result;
                PackageHost.PushStateObject("TextToSpeech", new { text = annonce });
            }).GetProxy().GetTraffic(new { type = ratpTrafficArg.arg1, line = ratpTrafficArg.arg2 });
            //}).GetProxy().GetTraffic(new { type = "metro", line = "1" });

        }

        void PushBullet()
        {
            //MessageScope.Create("PushBullet").GetProxy().SendPush(new { Iden = "CONFIG", Title = "BlackBullet", Message = pushbulletArg.arg1 }); // NE FONCTIONNE PAS AVEC UN IDEN ???
            MessageScope.Create("PushBullet").GetProxy().SendPush(new { Title = "BlackBullet", Message = pushbulletArg.arg1 });

        }

        void GoogleTraffic()
        {
            string annonce = "";
            PackageHost.WriteInfo($"J'ai exécuté la fonction GoogleTraffic avec comme départ: {googleTrafficArg.arg1} et comme arrivée: {googleTrafficArg.arg2}");
            MessageScope.Create("GoogleTraffic").OnSagaResponse(result =>
            {
                annonce = $"Pour aller de {googleTrafficArg.arg1} a {googleTrafficArg.arg2}, il faut voyager {result.Data[0].Name}, le temps de trajet avec traffic sera de {result.Data[0].InfoTraffic}, la distance est de : {result.Data[0].DistanceString}";
            }).GetProxy().GetRoutes(new { from = googleTrafficArg.arg1, to = googleTrafficArg.arg2 });
            PackageHost.PushStateObject("TextToSpeech", new { text = annonce });
        }
    }
}
