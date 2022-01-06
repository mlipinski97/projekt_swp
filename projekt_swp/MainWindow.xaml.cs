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
using System.Windows.Forms;

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
        static String bookName = "";
        SqlUtils sqlUtils;
        static List<Book> books = new List<Book>();

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

            public void returnBook()
            {
                SqlCommand command;
                SqlDataAdapter adapter = new SqlDataAdapter();
                String sqlString;
                sqlString = "UPDATE borrows SET actual_return_time = @actualReturnTime WHERE bookID = @bookId AND actual_return_time IS NULL";
                command = cnn.CreateCommand();
                command.CommandText = sqlString;
                command.Parameters.AddWithValue("@actualReturnTime", DateTime.Now.ToString());
                command.Parameters.AddWithValue("@bookId", bookId);
                adapter.UpdateCommand = command;
                adapter.UpdateCommand.ExecuteNonQuery();
                command.Dispose();
                Console.WriteLine("update complete");
            }
            public List<Book> getBooksLike(String bookName)
            {
                SqlCommand command;
                SqlDataReader dataReader;
                String sqlString;
                sqlString = "select books.bookID, title, author, average_rating, num_pages, language_code from books " +
                    "left join borrows on borrows.bookID = books.bookID where lower(title) " +
                    "like lower(@bookName) and books.bookID not in (select bookID from borrows where actual_return_time is null)";
                command = cnn.CreateCommand();
                command.CommandText = sqlString;
                command.Parameters.AddWithValue("@bookName", "%" + bookName + "%");
                dataReader = command.ExecuteReader();
                List<Book> books = new List<Book>();
                int innerIdCounter = 1;
                while (dataReader.Read())
                {
                    books.Add(new Book(innerIdCounter, dataReader.GetInt32(0), dataReader.GetString(1), dataReader.GetString(2), 
                        dataReader.GetDecimal(3), dataReader.GetInt32(4), dataReader.GetString(5)));
                    innerIdCounter++;
                }               
                return books;
                
            }
            public void borrowBook(String innerBookId, String nrPesel)
            {
                SqlCommand command;
                SqlDataAdapter adapter = new SqlDataAdapter();
                String sqlString;
                sqlString = "insert into borrows(pesel, bookID, borrow_time, return_time) values(@nrPesel, @bookId, @borrowTime, @returnTime)";
                command = cnn.CreateCommand();
                command.CommandText = sqlString;
                command.Parameters.AddWithValue("@nrPesel", nrPesel);
                command.Parameters.AddWithValue("@bookId", books.ElementAt(int.Parse(innerBookId) - 1).id);
                command.Parameters.AddWithValue("@borrowTime", DateTime.Now.ToString());
                command.Parameters.AddWithValue("@returnTime", DateTime.Now.AddDays(30).ToString());
                adapter.InsertCommand = command;
                adapter.InsertCommand.ExecuteNonQuery();
                command.Dispose();
                Console.WriteLine("insert complete");
            }
        }

        class Book
        {
            public int innerId { get; set; }
            public int id { get; set; }
            public String title { get; set; }
            public String author { get; set; }
            public Decimal average_rating { get; set; }
            public int num_pages { get; set; }
            public String language_code { get; set; }

            public Book(int innerId, int id, String title, String author, Decimal average_rating, int num_pages, String language_code)
            {
                this.innerId = innerId;
                this.id = id;
                this.title = title;
                this.author = author;
                this.average_rating = average_rating;
                this.num_pages = num_pages;
                this.language_code = language_code;
            }
            override public String ToString()
            {
                return this.id.ToString() + " " + title + " " + author
                    + " " + average_rating.ToString() + " " + num_pages.ToString() + " " + language_code;
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
            ss.SpeakAsync("Którą książkę chciałbyś wypożyczyć?");
            bookName = bookNameTextBox.Text;
            //wczytanie gramatyki odnosnie ksiazek. @Johny do decyzji jak robimy ksiazki, czy lista czy voice-to-text i LIKE do bazy
            books = sqlUtils.getBooksLike(bookNameTextBox.Text);
            bookListView.Items.Clear();
            var gridView = new GridView();
            bookListView.View = gridView;
            gridView.Columns.Add(new GridViewColumn
            {
                Header = "Id",
                DisplayMemberBinding = new System.Windows.Data.Binding("innerId")
            });
            gridView.Columns.Add(new GridViewColumn
            {
                Header = "title",
                DisplayMemberBinding = new System.Windows.Data.Binding("title")
            });
            gridView.Columns.Add(new GridViewColumn
            {
                Header = "author",
                DisplayMemberBinding = new System.Windows.Data.Binding("author")
            });
            gridView.Columns.Add(new GridViewColumn
            {
                Header = "average rating",
                DisplayMemberBinding = new System.Windows.Data.Binding("average_rating")
            });
            gridView.Columns.Add(new GridViewColumn
            {
                Header = "number of pages",
                DisplayMemberBinding = new System.Windows.Data.Binding("num_pages")
            });
            gridView.Columns.Add(new GridViewColumn
            {
                Header = "language code",
                DisplayMemberBinding = new System.Windows.Data.Binding("language_code")
            });
            foreach (Book book in books)
            {
                this.bookListView.Items.Add(book);
                Console.WriteLine(book.ToString());
            }

            ss.Speak("Podaj numer książki którą chcesz wypożyczyć");

            /*if (true)
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
            }*/
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
               sqlUtils.returnBook();
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
                sqlUtils.returnBook();
                Console.WriteLine("Czy chcesz zrobić coś jeszcze");
                ss.Speak("Czy chcesz zrobić coś jeszcze?");
                changeGrammar(Sre_AskForPaymentMethod, Sre_AskIfAnythingElse, ".\\Grammars\\YesNoGrammar.xml");
            }
            else
            {
                Console.WriteLine("Prosze powtorzyc");
                ss.Speak("Proszę powtórzyć");
            }
         
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            sqlUtils.borrowBook(bookInnerId.Text, "12345678910");
        }
    }
}
