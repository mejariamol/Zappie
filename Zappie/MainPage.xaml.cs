using SQLite.Net;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Zappie
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private static string path = Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\mydb.db";

        SQLiteConnection conn = new SQLiteConnection(new SQLite.Net.Platform.WinRT.SQLitePlatformWinRT(), path);

        private string getWord(string lastword)
        {
            

            List<DbItem> results = conn.Query<DbItem>("select word from entries where word like '"+ lastword[lastword.Length-1]+"%' and used=0");

            if (results.Count == 0)
            {
                conn.Execute("update entries set used=0 where word like '" + lastword[lastword.Length - 1] + "%'");
                results = conn.Query<DbItem>("select word from entries where word like '" + lastword[lastword.Length - 1] + "%' and used=0");
            }

            Random random = new Random();
            int i = random.Next(results.Count);

            conn.Execute("update entries set used=1 where word like '?'", results[i].word);

            this.lastWord = results[i].word;
            
            return results[i].word;
        }

        private void tick(string lastword)
        {
            conn.Execute("update entries set used=1 where word=" + lastword);
        }

        private ObservableCollection<Word> Chat;

        private static int ZAPPIE = 101;
        private static int USER = 102;

        private int USER_SCORE = 0;

        private int whoseTurn = ZAPPIE;

        DispatcherTimer timer;
        private int counter = 20;

        private string lastWord = "s";
        

        public MainPage()
        {
            this.InitializeComponent();

            Chat = new ObservableCollection<Word>();
            Chat.Add(new Word { body = "Hi! I am Zappie. Lets play the Word Game. I will start with any random word, and you will have to say a word which begins with the last letter of my word. And game goes on like this. You will get 20 sec to say a word which will fetch you +2 points, if failed then -1 point. Are you ready? Lets start...", isZappie = true, timestamp= DateTime.Now.ToString("h:mm tt") });
            myListView.ItemsSource = Chat;

        }

        private void Timer_Tick(object sender, object e)
        {
            if(counter > 0)
            {
                counter--;
                if (counter < 10)
                {
                    StatusBar.Background = new SolidColorBrush(Colors.Red);
                    TimerBox.Content = string.Format("00:0{0}", counter);
                }
                else
                {
                    StatusBar.Background = new SolidColorBrush(Colors.Green);
                    TimerBox.Content = string.Format("00:{0}", counter);
                }
            }
            else
            {
                timer.Stop();
                if(whoseTurn == ZAPPIE)
                {
                    USER_SCORE++;
                    UserScore.Text = "Score: " + USER_SCORE;
                    whoseTurn = USER;
                }
                else
                {
                    USER_SCORE--;
                    UserScore.Text = "Score: " + USER_SCORE;
                    whoseTurn = ZAPPIE;
                   // whoseTurnBox.Text = "Zappie's Turn";
                    
                    YoZappie();
                }

            }
        }

        private void YoZappie()
        {
            Chat.Add(new Word { body = getWord(lastWord), isZappie = true, timestamp = DateTime.Now.ToString("h:mm tt") });
            whoseTurnBox.Text = "" + lastWord.ToUpper()[lastWord.Length-1];
            selectedIndex = myListView.Items.Count - 1;
            myListView.SelectedIndex = selectedIndex;
            myListView.UpdateLayout();
            myListView.ScrollIntoView(myListView.SelectedItem);
            //whoseTurnBox.Text = "Your Turn";
            whoseTurn = USER;
            
            counter = 20;
            timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 1);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void TimerBox_Click(object sender, RoutedEventArgs e)
        {            
            Chat.Add(new Word { body = getWord("s"), isZappie = true, timestamp = DateTime.Now.ToString("h:mm tt") });
            whoseTurnBox.Text = ""+lastWord.ToUpper()[lastWord.Length - 1];
            selectedIndex = myListView.Items.Count - 1;
            myListView.SelectedIndex = selectedIndex;
            myListView.UpdateLayout();
            myListView.ScrollIntoView(myListView.SelectedItem);
            // whoseTurnBox.Text = "Your Turn";
            whoseTurn = USER;
            TimerBox.IsEnabled = false;

            timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 1);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //Windows.UI.ViewManagement.InputPane.GetForCurrentView().TryHide();
            if (whoseTurn == USER)
            {
                string input = sendBox.Text.Trim();
                if (input.Length > 0 && input.ToLower()[0] == lastWord[lastWord.Length - 1])
                {
                    Chat.Add(new Word { body = input, isZappie = false, timestamp = DateTime.Now.ToString("h:mm tt") });
                    selectedIndex = myListView.Items.Count - 1;
                    myListView.SelectedIndex = selectedIndex;
                    myListView.UpdateLayout();
                    myListView.ScrollIntoView(myListView.SelectedItem);
                    timer.Stop();
                    sendBox.Text = "";

                    ZappieCheck(input);
                }
            }
        }

        private int selectedIndex;

        private void ZappieCheck(string input)
        {
            List<DbItemFull> results = conn.Query<DbItemFull>("select * from entries where word like '"+ input +"'");

            if(results.Count > 0)
            {
                if (results[0].used == 1)
                {
                    Chat.Add(new Word { body = "Repeated Word Not Allowed!", isZappie = true, timestamp = DateTime.Now.ToString("h:mm tt") });
                    selectedIndex = myListView.Items.Count - 1;
                    myListView.SelectedIndex = selectedIndex;
                    myListView.UpdateLayout();
                    myListView.ScrollIntoView(myListView.SelectedItem);
                    timer.Start();
                }
                else
                {
                    lastWord = input;
                    if (counter > 9)
                    {
                        USER_SCORE += 2;
                    }
                    else
                    {
                        USER_SCORE += 1;
                    }
                    UserScore.Text = "Score: " + USER_SCORE;
                    whoseTurnBox.Text = "" + lastWord.ToUpper()[lastWord.Length - 1];
                    counter = 20;
                    YoZappie();
                }
            }
            else
            {
                Chat.Add(new Word { body = "No Such Word Exist!", isZappie = true, timestamp = DateTime.Now.ToString("h:mm tt") });
                selectedIndex = myListView.Items.Count - 1;
                myListView.SelectedIndex = selectedIndex;
                myListView.UpdateLayout();
                myListView.ScrollIntoView(myListView.SelectedItem);
                timer.Start();
                
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if(selectedIndex > 0)
            {
                Word item = myListView.SelectedItem as Word;
                if(conn == null)
                {
                    conn = new SQLiteConnection(new SQLite.Net.Platform.WinRT.SQLitePlatformWinRT(), path);
                }
                List<DbItemFull> results = new List<DbItemFull>();
                results = conn.Query<DbItemFull>("select * from entries where word like '" + item.body + "'");

                if(results.Count > 0)
                {
                    Chat.Add(new Word { body = item.body + "\n" + results[0].definition, isZappie = true, timestamp = DateTime.Now.ToString("h:mm tt") });
                    selectedIndex = myListView.Items.Count - 1;
                    myListView.SelectedIndex = selectedIndex;
                    myListView.UpdateLayout();
                    myListView.ScrollIntoView(myListView.SelectedItem);
                }
            }
        }

        private void sendBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if(e.Key == Windows.System.VirtualKey.Enter)
            {
                //Windows.UI.ViewManagement.InputPane.GetForCurrentView().TryHide();
                if (whoseTurn == USER)
                {
                    string input = sendBox.Text.Trim();
                    if (input.Length > 0 && input.ToLower()[0] == lastWord[lastWord.Length - 1])
                    {
                        Chat.Add(new Word { body = input, isZappie = false, timestamp = DateTime.Now.ToString("h:mm tt") });
                        selectedIndex = myListView.Items.Count - 1;
                        myListView.SelectedIndex = selectedIndex;
                        myListView.UpdateLayout();
                        myListView.ScrollIntoView(myListView.SelectedItem);
                        timer.Stop();
                        sendBox.Text = "";

                        ZappieCheck(input);
                    }
                }
            }
        }
    }
}
