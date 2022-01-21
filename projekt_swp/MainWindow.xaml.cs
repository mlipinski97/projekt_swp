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
        static List<String> bookTitles = new List<String>();
        static Grammar titlesGrammar;
        static Grammar numbersGrammar;
        static Boolean advancedUser = false;

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
                cnn.Close();
                cnn.Open();
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
                command.Dispose();
                return returnDate;
            }

            public void returnBook()
            {
                cnn.Close();
                cnn.Open();
                SqlCommand updateCommand;
                SqlDataAdapter adapter = new SqlDataAdapter();
                String sqlString;
                sqlString = "UPDATE borrows SET actual_return_time = @actualReturnTime WHERE bookID = @bookId AND actual_return_time IS NULL";
                updateCommand = cnn.CreateCommand();
                updateCommand.CommandText = sqlString;
                updateCommand.Parameters.AddWithValue("@actualReturnTime", DateTime.Now.ToString());
                updateCommand.Parameters.AddWithValue("@bookId", bookId);
                adapter.UpdateCommand = updateCommand;
                adapter.UpdateCommand.ExecuteNonQuery();
                updateCommand.Dispose();
                Console.WriteLine("update complete");
            }
            public List<Book> getBooksLike(String bookName)
            {
                cnn.Close();
                cnn.Open();
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
                cnn.Close();
                cnn.Open();
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

            public void addNewPesel(String pesel)
            {
                cnn.Close();
                cnn.Open();
                SqlCommand command;
                SqlDataAdapter adapter = new SqlDataAdapter();
                String sqlString;
                sqlString = "insert into users(pesel) values(@nrPesel)";
                command = cnn.CreateCommand();
                command.CommandText = sqlString;
                command.Parameters.AddWithValue("@nrPesel", pesel);
                adapter.InsertCommand = command;
                adapter.InsertCommand.ExecuteNonQuery();
                command.Dispose();
                Console.WriteLine("insert complete");
            }

            public void fetchAllTitiles()
            {
                cnn.Close();
                cnn.Open();
                SqlCommand command;
                SqlDataReader dataReader;
                String sqlString;
                sqlString = "select title from books";
                command = cnn.CreateCommand();
                command.CommandText = sqlString;
                dataReader = command.ExecuteReader();
                while (dataReader.Read())
                {
                    bookTitles.Add(dataReader.GetString(0));
                }
                Console.WriteLine("zaladowano wszystkie tytuly");
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
            sqlUtils = new SqlUtils();
            sqlUtils.fetchAllTitiles();
            makeTitleList();
            InitializeComponent();
            ss = new SpeechSynthesizer();
            ss.SetOutputToDefaultAudioDevice();
            ss.SpeakAsync("Witam w bibliotece cyfrowej. Chciałbyś oddać czy wypożyczyć książkę?");
            CultureInfo ci = new CultureInfo("pl-PL");
            sre = new SpeechRecognitionEngine(ci);
            sre.SetInputToDefaultAudioDevice();
            sre.SpeechRecognized += Sre_ChooseAction;
            Microsoft.Speech.Recognition.Grammar grammar = new Grammar(".\\Grammars\\MainLibraryGrammar.xml");
            grammar.Enabled = true;
            sre.LoadGrammar(grammar);
            sre.RecognizeAsync(RecognizeMode.Multiple);

        }

        public void makeNumberList(int maxNumber)
        {
            Choices numbers = new Choices();
            for (int i = 1; i <= maxNumber; i++)
            {
                numbers.Add(i.ToString());
            }
            numbersGrammar = new Grammar(new GrammarBuilder(numbers));
            Console.WriteLine("Stworzono gramatyke numerow do liczby: " + maxNumber);
        }

        public void makeTitleList()
        {
            List<String> allTitles = new List<String>();
            bookTitles = bookTitles.Take(2000).ToList();
            foreach (String title in bookTitles)
            {
                String[] wordsInTile = title.Trim().Split(' ', '"');
                String currentWord = "";
                foreach (String s in wordsInTile)
                {
                    currentWord += s.Trim() + " ";
                    allTitles.Add(currentWord.Trim());
                }
            }
            titlesGrammar = new Grammar(new GrammarBuilder(new Choices(allTitles.ToArray())));
            Console.WriteLine("Stworzono gramatyke tytulow");
        }
        private void Sre_ChooseAction(object sender, SpeechRecognizedEventArgs e)
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
                    string pesel = "";
                    pesel += e.Result.Semantics["first"].Value.ToString();
                    pesel += e.Result.Semantics["second"].Value.ToString();
                    pesel += e.Result.Semantics["third"].Value.ToString();
                    pesel += e.Result.Semantics["fourth"].Value.ToString();
                    pesel += e.Result.Semantics["fifth"].Value.ToString();
                    pesel += e.Result.Semantics["sixth"].Value.ToString();
                    pesel += e.Result.Semantics["seventh"].Value.ToString();
                    pesel += e.Result.Semantics["eighth"].Value.ToString();
                    pesel += e.Result.Semantics["ninth"].Value.ToString();
                    pesel += e.Result.Semantics["tenth"].Value.ToString();
                    pesel += e.Result.Semantics["eleventh"].Value.ToString();
                    Pesel = pesel;
                    if (pesel.Length == 11)
                    {
                        Console.WriteLine("Czy PESEL jest poprawny? " + pesel);
                        ss.SpeakAsync("Czy PESEL jest poprawny?");
                        changeGrammar(Sre_ChooseAction, Sre_AskIfPeselIsCorrect, ".\\Grammars\\YesNoGrammar.xml");
                    }
                    else
                    {
                        Console.WriteLine("PESEL jest niepoprawny " + pesel);
                        ss.SpeakAsync("PESEL jest niepoprawny!");
                        Pesel = "";
                        handleWypozyczyc();
                    }
                }
                if (chosenAction.Equals("none"))
                {
                    //chyba niepotrzebne, w zaleznosci jak postanowimy zrobic niezrozumiala odpowiedz
                }

            }
        }

        private void setupBookListView()
        {
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
        }


        private void Sre_AskToUseSavedPesel(object sender, SpeechRecognizedEventArgs e)
        {
            String result = e.Result.Text;
            float confidence = e.Result.Confidence;
            if (confidence >= 0.6)
            {
                string choice = e.Result.Semantics["yesno"].Value.ToString();
                if (choice.Equals("yes"))
                {
                   ss.SpeakAsync("Którą książkę chciałbyś wypożyczyć?");
                    Console.WriteLine("Którą książkę chciałbyś wypożyczyć?");
                   changeGrammar(Sre_AskToUseSavedPesel, Sre_AskForBookName, titlesGrammar);
                    //tutaj powinna pojawić się zczytywanie ksiazki od uzytkownika
                }
                if (choice.Equals("no"))
                {
                    Console.WriteLine("proszę podać swój PESEL");
                    ss.Speak("proszę podać swój PESEL");
                    changeGrammar(Sre_AskToUseSavedPesel, Sre_AskForPesel, ".\\Grammars\\PESELGrammar.xml");
                }
            }
            else
            {
                Console.WriteLine("Prosze powtorzyc");
                ss.SpeakAsync("Proszę powtórzyć");
            }
        }

        private void Sre_AskForBookName(object sender, SpeechRecognizedEventArgs e)
        {
            String result = e.Result.Text;
            float confidence = e.Result.Confidence;
            if (confidence >= 0.6)
            {
                bookName = e.Result.Text;
                Console.WriteLine(bookName);   
                books = sqlUtils.getBooksLike(bookName);
                makeNumberList(books.Count());
                setupBookListView();
                foreach (Book book in books)
                {
                    this.bookListView.Items.Add(book);
                    Console.WriteLine(book.ToString());
                }
                Console.WriteLine("Numer książki do wypożyczenia");
                ss.SpeakAsync("Numer książki do wypożyczenia?");
                changeGrammar(Sre_AskForBookName, Sre_AskForBookNumber, numbersGrammar);
            }
            else
            {
                Console.WriteLine("Prosze powtorzyc");
                ss.SpeakAsync("Proszę powtórzyć");
            }
        }

        private void Sre_AskForBookNumber(object sender, SpeechRecognizedEventArgs e)
        {
            String result = e.Result.Text;
            float confidence = e.Result.Confidence;
            if (confidence >= 0.6)
            {
                String bookNumber = e.Result.Text;
                sqlUtils.borrowBook(bookNumber, Pesel);
                Console.WriteLine(bookNumber);

                Console.WriteLine("wypozyczono ksiazke z numerem: " + bookNumber + " Czy chciałbyś zrobić coś jeszcze?");
                ss.SpeakAsync("wypozyczono ksiazke z numerem: " + bookNumber + ". Czy chciałbyś zrobić coś jeszcze?");
                changeGrammar(Sre_AskForBookNumber, Sre_AskIfAnythingElse, ".\\Grammars\\YesNoGrammar.xml");
            }
            else
            {
                Console.WriteLine("Prosze powtorzyc");
                ss.SpeakAsync("Proszę powtórzyć");
            }
        }

        private void handleWypozyczyc()
        {
            if (!Pesel.Equals("") )
            {
                Console.WriteLine("Czy chcesz uzyc tego samego numeru pesel co poprzednio?");
                ss.SpeakAsync("Czy chcesz użyć tego samego numeru PESEL co poprzednio?");
                changeGrammar(Sre_ChooseAction, Sre_AskToUseSavedPesel, ".\\Grammars\\YesNoGrammar.xml");
            }
            else
            {
                Console.WriteLine("proszę podać swój PESEL");
                ss.SpeakAsync("proszę podać swój PESEL");
                changeGrammar(Sre_ChooseAction, Sre_AskForPesel, ".\\Grammars\\PESELGrammar.xml");
            }

        }

        private void Sre_AskForPesel(object sender, SpeechRecognizedEventArgs e)
        {
            String result = e.Result.Text;
            float confidence = e.Result.Confidence;
            if (confidence >= 0.6)
            {
                string pesel = "";
                pesel += e.Result.Semantics["first"].Value.ToString();
                pesel += e.Result.Semantics["second"].Value.ToString();
                pesel += e.Result.Semantics["third"].Value.ToString();
                pesel += e.Result.Semantics["fourth"].Value.ToString();
                pesel += e.Result.Semantics["fifth"].Value.ToString();
                pesel += e.Result.Semantics["sixth"].Value.ToString();
                pesel += e.Result.Semantics["seventh"].Value.ToString();
                pesel += e.Result.Semantics["eighth"].Value.ToString();
                pesel += e.Result.Semantics["ninth"].Value.ToString();
                pesel += e.Result.Semantics["tenth"].Value.ToString();
                pesel += e.Result.Semantics["eleventh"].Value.ToString();
                Pesel = pesel;
                Console.WriteLine("Czy PESEL jest poprawny? " + pesel);
                ss.SpeakAsync("Czy PESEL jest poprawny?");
                changeGrammar(Sre_AskForPesel, Sre_AskIfPeselIsCorrect, ".\\Grammars\\YesNoGrammar.xml");
            }
            else
            {
                Console.WriteLine("Prosze powtorzyc");
                ss.SpeakAsync("Proszę powtórzyć");
            }
        }

        private void Sre_AskIfPeselIsCorrect(object sender, SpeechRecognizedEventArgs e)
        {
            String result = e.Result.Text;
            float confidence = e.Result.Confidence;
            if (confidence >= 0.6)
            {
                string choice = e.Result.Semantics["yesno"].Value.ToString();
                if (choice.Equals("yes"))
                {
                    try
                    {
                        sqlUtils.addNewPesel(Pesel);
                    }catch (Exception)
                    {
                        Console.WriteLine("taki pesel juz jest wiec odrzucono insert :)");
                    }
                    ss.SpeakAsync("Którą książkę chciałbyś wypożyczyć?");
                    Console.WriteLine("Którą książkę chciałbyś wypożyczyć?"); 
                    changeGrammar(Sre_AskIfPeselIsCorrect, Sre_AskForBookName, titlesGrammar);
                }
                if (choice.Equals("no"))
                {
                    Console.WriteLine("prosze powtorzyc PESEL");
                    ss.Speak("Proszę powtórzyć PESEL!");
                    changeGrammar(Sre_AskIfPeselIsCorrect, Sre_AskForPesel, ".\\Grammars\\PESELGrammar.xml");
                }
            }
            else
            {
                Console.WriteLine("Prosze powtorzyc");
                ss.SpeakAsync("Proszę powtórzyć");
            }
        }

        public void handleOddac()
        {
            ss.Speak("Proszę umieścić książkę w miejscu oznaczonym na oddawanie książek.");
            if (isOverdue())
            {
               Console.WriteLine("ksiazka wymaga zaplacenia oplaty za przetrzymanie");
               Console.WriteLine("Platnosc karta czy gotowka?");
               ss.Speak("Platnosc karta czy gotowka?");
               changeGrammar(Sre_ChooseAction, Sre_AskForPaymentMethod, ".\\Grammars\\PaymentGrammar.xml");
            } else
            {
               Console.WriteLine("ksiazka nie wymaga zaplacenia oplaty za przetrzymanie");
               sqlUtils.returnBook();
               ss.Speak("Czy chciałbyś zrobić coś jeszcze?");
                changeGrammar(Sre_ChooseAction, Sre_AskIfAnythingElse, ".\\Grammars\\YesNoGrammar.xml");
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

        private void changeGrammar(EventHandler<SpeechRecognizedEventArgs> methodNameToSubstract,
            EventHandler<SpeechRecognizedEventArgs> methodNameToAdd,
            Microsoft.Speech.Recognition.Grammar newGrammar)
        {
            sre.UnloadAllGrammars();
            newGrammar.Enabled = true;
            sre.LoadGrammar(newGrammar);
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
                    Console.WriteLine("Chciałbyś oddać czy wypożyczyć książkę?");
                    ss.Speak("Chciałbyś oddać czy wypożyczyć książkę?");
                    changeGrammar(Sre_AskIfAnythingElse, Sre_ChooseAction, ".\\Grammars\\MainLibraryGrammar.xml");
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

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            bookId = bookIdTextBox.Text;
        }
    }
}
