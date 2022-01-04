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
using System.IO;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Speech.Synthesis;
using Microsoft.Speech.Recognition;
using System.Globalization;

namespace projekt_swp
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static Microsoft.Speech.Synthesis.SpeechSynthesizer ss;
        static SpeechRecognitionEngine sre;
        static String Pesel = "";
        public MainWindow()
        {
            InitializeComponent();
            ss = new Microsoft.Speech.Synthesis.SpeechSynthesizer();
            ss.SetOutputToDefaultAudioDevice();
            ss.Speak("Witam w bibliotece cyfrowej. Chciałbyś oddać czy wypożyczyć książkę?");
            CultureInfo ci = new CultureInfo("pl-PL");
            sre = new SpeechRecognitionEngine(ci);
            sre.SetInputToDefaultAudioDevice();
            sre.SpeechRecognized += Sre_SpeechRecognized;
            Microsoft.Speech.Recognition.Grammar grammar = new Microsoft.Speech.Recognition.Grammar(".\\Grammars\\MainLibraryGrammar.xml");
            grammar.Enabled = true;
            sre.LoadGrammar(grammar);
            sre.RecognizeAsync(RecognizeMode.Multiple);
        }
        //chcialbym zaznaczyc ze robimy to jak zwierzeta w jednej klasie ale chyba nie mamy wyboru
        private void Sre_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            float confidence = e.Result.Confidence;
            if (confidence <= 0.6)
            {
                Console.WriteLine("zbyt mała pewnosc (pewnosc równa: " + confidence + ")");

            }
            else
            {
                string chosenAction = e.Result.Semantics["action"].Value.ToString();
                if (chosenAction.Equals("oddac"))
                {
                    handleOddac();
                }
                if (chosenAction.Equals("wypozyczyc"))
                {
                    handleWypozyczyc();
                }
                if (chosenAction.Equals("none"))
                {
                    //chyba niepotrzebne, w zaleznosci jak postanowimy zrobic niezrozumiala odpowiedz
                }

            }
        }

        private void handleWypozyczyc()
        {
            ss.Speak("Którą książkę chciałbyś wypożyczyć?");
            //wczytanie gramatyki odnosnie ksiazek. @Johny do decyzji jak robimy ksiazki, czy lista czy voice-to-text i LIKE do bazy
            if (checkIfBookAvailable())
            {
                if (Pesel.Equals(""))
                {
                    ss.Speak("Proszę podać swój numer PESEL");
                    //wczytanie gramatyki numeru pesel i wysuchanie peselu od uzytkownika. sprawdzenie czy sie zgadza regexem
                }
                else
                {
                    ss.Speak("Czy użyć wcześniej podanego numeru PESEL?");
                    //wczytanie gramatyki tak-nie
                }
            }
            else
            {
                ss.Speak("Książka którą próbujesz wypożyczyć jest obecnie niedostępna.");
                ss.Speak("Czy chciałbyś zrobić coś jeszcze?");
                //TODO zaimplementowac ladowanie gramatyki tak-nie wraz z pętlą przekierowującą na pierwsze pytanie (czy chcesz oddac/wypo)
            }
        }

       
        private bool checkIfBookAvailable()
        {
            //mock sprawdzania czy ksiazka jest dostepna w bazie danych
            return checkIfOverdue();
        }

        public void handleOddac()
        {
            ss.Speak("Proszę umieścić książkę w miejscu oznaczonym na oddawanie książek.");
            if (checkIfOverdue())
            {
               Console.WriteLine("ksiazka wymaga zaplacenia oplaty za przetrzymanie");
               askForPaymentMethod();
            } else
            {
               Console.WriteLine("ksiazka nie wymaga zaplacenia oplaty za przetrzymanie");
               ss.Speak("Czy chciałbyś zrobić coś jeszcze?");
               //TODO zaimplementowac ladowanie gramatyki tak-nie wraz z pętlą przekierowującą na pierwsze pytanie (czy chcesz oddac/wypo)
            }
        }

        private void askForPaymentMethod()
        {
            throw new NotImplementedException();
            //TODO wczytywanie gramatyki PaymentGrammar.xml (gramatyki platnosci: gotowka/karta)
        }

        //mockowanie sprawdzania czy ksiazka przetrzymana, normalnie powinno być sprawdzane z bazy danych.
        //50% szansy ze przetrzymana 50% ze nie przetrzymana
        private Boolean checkIfOverdue()
        {
            Random random = new Random();
            if (random.Next(0, 2) == 1)
            {
                return true;
            }
            return false;
        }

        //obie klasy sa do voice-to-text od microsoftu
        public static async Task RecognizeSpeechAsync()
        {
            // Creates an instance of a speech config with specified subscription key and service region.
            // Replace with your own subscription key // and service region (e.g., "westus").
            var config = SpeechConfig.FromSubscription("<TUTAJ WKLEJAMY KOD SUBSKRYBCJI Z AZURE", "westeurope");

            using (var recognizer = new SpeechRecognizer(config))
            {
                Console.WriteLine("Say something...");

                // Starts speech recognition, and returns after a single utterance is recognized. The end of a
                // single utterance is determined by listening for silence at the end or until a maximum of 15
                // seconds of audio is processed.  The task returns the recognition text as result. 
                // Note: Since RecognizeOnceAsync() returns only a single utterance, it is suitable only for single
                // shot recognition like command or query. 
                // For long-running multi-utterance recognition, use StartContinuousRecognitionAsync() instead.
                var result = await recognizer.RecognizeOnceAsync();

                // Checks result.
                if (result.Reason == ResultReason.RecognizedSpeech)
                {
                    Console.WriteLine($"We recognized: {result.Text}");
                }
                else if (result.Reason == ResultReason.NoMatch)
                {
                    Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                }
                else if (result.Reason == ResultReason.Canceled)
                {
                    var cancellation = CancellationDetails.FromResult(result);
                    Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

                    if (cancellation.Reason == CancellationReason.Error)
                    {
                        Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                        Console.WriteLine($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
                        Console.WriteLine($"CANCELED: Did you update the subscription info?");
                    }
                }
            }
        }

        static async Task ReadLineFromMic()
        {
            await RecognizeSpeechAsync();
            Console.WriteLine("Please press <Return> to continue.");
            Console.ReadLine();
        }
    }
}
