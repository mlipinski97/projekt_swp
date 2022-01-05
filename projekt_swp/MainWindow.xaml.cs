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
using System.Data.SqlClient;

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
        static String bookId = "";
        SqlUtils sqlUtils;

        class SqlUtils {
            static string connetionString = "Server=tcp:swp-bookstore-server.database.windows.net,1433;" +
                "Initial Catalog=swp-bookstore;" +
                "Persist Security Info=False;" +
                "User ID=swp;" +
                "Password=waciak123$;" +
                "MultipleActiveResultSets=False;" +
                "Encrypt=True;" +
                "TrustServerCertificate=False;" +
                "Connection Timeout=30;";
            static SqlConnection cnn = new SqlConnection(connetionString);
            public SqlUtils()
            {
                cnn.Open();
                Console.WriteLine("Connection Open!");
            }

            public void closeConnection()
            {
                cnn.Close();
                Console.WriteLine("Connection closed!");
            }
            public DateTime checkIfOverdueInDataBase()
            {
                SqlCommand command;
                SqlDataReader dataReader;
                String sqlString;
                DateTime returnDate = new DateTime();
                sqlString = "SELECT return_time from borrows WHERE bookID = @bookId AND actual_return_time IS NULL";
                command = cnn.CreateCommand();
                command.CommandText = sqlString;
                command.Parameters.AddWithValue("@bookId", bookId);
                dataReader = command.ExecuteReader();
                while (dataReader.Read())
                {
                    returnDate = dataReader.GetDateTime(0);
                }
                Console.WriteLine(returnDate.ToString());
                return returnDate;
            }
        }

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
            sqlUtils = new SqlUtils();
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
            return isOverdue();
        }

        public void handleOddac()
        {
            ss.Speak("Proszę umieścić książkę w miejscu oznaczonym na oddawanie książek.");
            if (isOverdue())
            {
               Console.WriteLine("ksiazka wymaga zaplacenia oplaty za przetrzymanie");
               Console.WriteLine("Platnosc karta czy gotowka?");
               ss.Speak("Platnosc karta czy gotowka?");
                changeGrammar(Sre_SpeechRecognized, Sre_AskForPaymentMethod, ".\\Grammars\\PaymentGrammar.xml");
            } else
            {
               Console.WriteLine("ksiazka nie wymaga zaplacenia oplaty za przetrzymanie");
               ss.Speak("Czy chciałbyś zrobić coś jeszcze?");
                changeGrammar(Sre_SpeechRecognized, Sre_AskIfAnythingElse, ".\\Grammars\\YesNoGrammar.xml");
            }
        }

        private void changeGrammar(EventHandler<SpeechRecognizedEventArgs> methodNameToSubstract, 
            EventHandler<SpeechRecognizedEventArgs> methodNameToAdd,
            String grammarPath)
        {
            sre.UnloadAllGrammars();
            Microsoft.Speech.Recognition.Grammar grammar = new Microsoft.Speech.Recognition.Grammar(grammarPath);
            grammar.Enabled = true;
            sre.LoadGrammar(grammar);
           
            sre.SpeechRecognized -= methodNameToSubstract;
            sre.SpeechRecognized += methodNameToAdd;
        }
        private void Sre_AskIfAnythingElse(object sender, SpeechRecognizedEventArgs e)
        {
            String result = e.Result.Text;
            float confidence = e.Result.Confidence;
            if (confidence >= 0.6)
            {
                string choice = e.Result.Semantics["yesno"].Value.ToString();
                if (choice.Equals("yes"))
                {
                    ss.Speak("Chciałbyś oddać czy wypożyczyć książkę?");
                    changeGrammar(Sre_AskIfAnythingElse, Sre_SpeechRecognized, ".\\Grammars\\MainLibraryGrammar.xml");
                }
                if (choice.Equals("no"))
                {
                    Console.WriteLine("Dziękuję, życzę miłego dnia");
                    ss.Speak("Dziękuję, życzę miłego dnia");
                }
             }
            else
            {
                Console.WriteLine("Prosze powtorzyc");
                ss.SpeakAsync("Proszę powtórzyć");
            }
        }
        private void Sre_AskForPaymentMethod(object sender, SpeechRecognizedEventArgs e)
        {
            String result = e.Result.Text;
            float confidence = e.Result.Confidence;
            if (confidence >= 0.6)
            {
                string chosenPayment = e.Result.Semantics["platnosc"].Value.ToString();
                if (chosenPayment.Equals("karta"))
                {
                    Console.WriteLine("wybrano karte");
                    ss.Speak("Wybrano kartę.");
                }
                if (chosenPayment.Equals("gotowka"))
                {
                    Console.WriteLine("wybrano gotówkę");
                    ss.Speak("Wybrano gotówkę.");
                }
                Console.WriteLine("Czy chcesz zrobić coś jeszcze");
                ss.Speak("Czy chcesz zrobić coś jeszcze?");
                changeGrammar(Sre_AskForPaymentMethod, Sre_AskIfAnythingElse, ".\\Grammars\\YesNoGrammar.xml");
            }
            else
            {
                Console.WriteLine("Prosze powtorzyc");
                ss.Speak("Proszę powtórzyć");
            }
            //TODO wczytywanie gramatyki PaymentGrammar.xml (gramatyki platnosci: gotowka/karta)
        }

        private Boolean isOverdue()
        {
            return sqlUtils.checkIfOverdueInDataBase() < DateTime.Now;
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

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            bookId = bookIdTextBox.Text;
        }

    }
}
