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

namespace projekt_swp
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Main(null);
        }
        async static Task FromMic(SpeechConfig speechConfig)
        {
            AudioConfig audioConfig = AudioConfig.FromDefaultMicrophoneInput();
            SpeechRecognizer recognizer = new SpeechRecognizer(speechConfig, audioConfig);

            Console.WriteLine("Speak into your microphone.");
            var result = await recognizer.RecognizeOnceAsync();
            Console.WriteLine($"RECOGNIZED: Text={result.Text}");
        }

        async static Task Main(string[] args)
        {
            var speechConfig = SpeechConfig.FromSubscription("a", "westeurope");
            await FromMic(speechConfig);
        }
    }
}
