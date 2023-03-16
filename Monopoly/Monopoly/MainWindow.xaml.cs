using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Runtime.Remoting.Metadata;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using System.Runtime.Remoting.Messaging;

namespace Monopoly
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 


    public abstract class Square
    {
        public string name;
        public abstract void Action(Player player);
    }

    public class MoneySquare : Square
    {
        int type;
        public int amount;
        public MoneySquare(string name, int type, int amount)
        {
            this.name = name;
            this.type = type;
            this.amount = amount;
        }
        public override void Action(Player player)
        {
            if (type == 1)
            {
                player.Bal += this.amount;
            }
            else player.Bal -= this.amount;
        }
    }

    public class GoToJail : Square
    {

        public GoToJail(string name)
        {
            this.name = name;
        }
        public override void Action(Player player)
        {
            player.InJail = true;

        }
    }

    public class NoActionSquare : Square
    {

        public NoActionSquare(string name)
        {
            this.name = name;
        }
        public override void Action(Player player) { }
    }

    public class Chance : Square {

        private static readonly Dictionary<int, int> StationDic = new Dictionary<int, int> { { 7, 15 }, { 22, 25 }, { 36, 5 } };
        private static readonly Dictionary<int, int> UtilityDic = new Dictionary<int, int> { { 7, 12 }, { 22, 28 }, { 36, 12 } };

        public Chance(string Name) {
            this.name = Name;
        }

        private static void Get_chance(Player player)
        {
            string card = System.IO.File.ReadAllLines(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\Init\\" + "Chance.txt")[Player.dice.Next(0, System.IO.File.ReadAllLines(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\Init\\" + "Community Chest.txt").Length)];
            string[] data = card.Split(',');
            Random_Square_Action(data, player, true);
        }

        private static void Get_Community(Player player)
        {
            string card = System.IO.File.ReadAllLines(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\Init\\" + "Community Chest.txt")[Player.dice.Next(0, System.IO.File.ReadAllLines(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\Init\\" + "Community Chest.txt").Length)];
            string[] data = card.Split(',');
            Random_Square_Action(data, player, false);
        }


        private static void Random_Square_Action(string[] data, Player player, bool type)
        {
            // advance to a place: 0
            // collect money: 1
            // take money: 2
            // conditional advance: 3
            // move backwards 4
            // GOJF 5
            // GTJ 6
            // Houses 7
            MainWindow var = Window.GetWindow(App.Current.MainWindow) as MainWindow;
            int ind = Array.IndexOf(var.board.players, player);
            Image temp1 = (Image)(var.rootCanvas).Children[ind + 1];
            MessageBox.Show(data[0]);
            switch (data[1])
            {
                case "0":
                    player.Position = int.Parse(data[2]);
                    var.move_player(temp1, player.Position);
                    var.board.squares[player.Position].Action(player);
                    break;

                case "1":
                    player.Bal += int.Parse(data[2]);

                    break;

                case "2":
                    player.Bal -= int.Parse(data[2]);
                    break;

                case "3":
                    if (player.Position == 36) var.board.squares[0].Action(player);
                    if (data[2] == "Station") player.Position = StationDic[player.Position];
                    else player.Position = UtilityDic[player.Position];
                    var.move_player(temp1, player.Position);
                    var.board.squares[player.Position].Action(player);
                    break;

                case "4":
                    player.Position -= int.Parse(data[2]);
                    var.move_player(temp1, player.Position);
                    var.board.squares[player.Position].Action(player);
                    break;

                case "5":
                    player.GetOutOfJail += 1;
                    break;

                case "6":
                    player.InJail = true;
                    player.Position = 30;
                    var.move_player(temp1, player.Position);
                    break;

                case "7":

                    break;
            }
            var.ChanceScreen(data[0], type);
        }
        public override void Action(Player player)
        {
            if (this.name == "Chance") Get_chance(player);

            else Get_Community(player);
        }
    }

    public class BuyableSquare : Square
    {
        public ushort price;
        public Player owner;
        public ushort morgage;
        bool IsMorgaged = false;

        public void Buy(Player player, int price)
        {
            this.owner = player;
            player.Bal -= price;
        }

        public override void Action(Player player) { }
    }

    public class Station : BuyableSquare
    { // 2,Kings Cross Station,200,25,50,100,200
        public ushort[] rents;
        public Station(string name, ushort price, ushort[] rents)
        {
            this.name = name;
            this.price = price;
            this.rents = rents;
            this.morgage = (ushort)(this.price / 2);
        }

        public override void Action(Player player)
        {
            MainWindow var = Window.GetWindow(App.Current.MainWindow) as MainWindow;
            if (this.owner == null)
            {
                var.BuyScreen(player, this);

            }
            else
            {
                int renttopay = CalcRent(player, var.board);
                ((TextBlock)((ScrollViewer)(var.rootCanvas).Children[var.PlayerCount + 11]).Content).Text += player.Name + " paid £" + renttopay.ToString() + " rent to " + this.owner.Name + "\n";
                var.board.history += player.Name + " paid £" + renttopay.ToString() + " rent to " + this.owner.Name + "\n";
                player.Bal = player.Bal - renttopay;
                this.owner.Bal += renttopay;
            }
        }

        private int CalcRent(Player player, Board board)
        {
            int counter = 0;
            for (int i = 0; i < 4; i++)
            {
                int index = 10 * i + 5;
                if (((Station)board.squares[index]).owner == player)
                {
                    counter++;
                }
            }
            return this.rents[counter];
        }
    };

    public class Company : BuyableSquare
    {
        public Company(string name, ushort price)
        {
            this.name = name;
            this.price = price;
            this.morgage = (ushort)(price / 2);
        }

        private int CalcRentCoefficent(Player player, Board board)
        {
            int counter = 0;
            if (((Company)board.squares[12]).owner == player)
            {
                counter++;
            }
            if (((Company)board.squares[28]).owner == player)
            {
                counter++;
            }

            return ((6 * counter) - 2);

        }

        public void Action(Player player, int dice)
        {
            MainWindow var = Window.GetWindow(App.Current.MainWindow) as MainWindow;

            if (owner == null)
            {
                var.BuyScreen(player, this);
            }
            else
            {
                int rentCo = CalcRentCoefficent(this.owner, var.board);
                player.Bal -= rentCo * dice;
                this.owner.Bal += rentCo * dice;
                ((TextBlock)((ScrollViewer)(var.rootCanvas).Children[var.PlayerCount + 11]).Content).Text += player.Name + " paid £" + (rentCo * dice).ToString() + " rent to " + this.owner.Name + "\n";
                var.board.history += player.Name + " paid £" + (rentCo * dice).ToString() + " rent to " + this.owner.Name + "\n";
            }
        }
    }

    public class Property : BuyableSquare
    {
        public char colour;
        public ushort[] rents;
        public ushort houses;
        public ushort housescost;
        public ushort hotelcost;

        public Property(ushort price, char colour, string name, ushort[] rents, ushort houses) // 1, Old Kent Road, b, 60, 2, 10, 30, 90, 160, 250
        {
            this.owner = null;
            this.houses = 0;
            this.rents = rents;
            this.name = name;
            this.price = price;
            this.colour = colour;
            this.morgage = (ushort)(price / 2);
            this.housescost = houses;
        }
        public override void Action(Player player)
        {
            MainWindow var = Window.GetWindow(App.Current.MainWindow) as MainWindow;
            if (this.owner == null)
            {
                var.BuyScreen(player, this);
            }
            else
            {
                ((TextBlock)((ScrollViewer)(var.rootCanvas).Children[var.PlayerCount + 11]).Content).Text += player.Name + " paid £" + this.rents[this.houses].ToString() + " rent to " + this.owner.Name + "\n";
                var.board.history += player.Name + " paid £" + this.rents[this.houses].ToString() + " rent to " + this.owner.Name + "\n";
                player.Bal = player.Bal - this.rents[this.houses];
                this.owner.Bal += this.rents[this.houses];
            }
        }

    }

    public class Board
    {
        public Player[] players;
        public Square[] squares = new Square[40];
        private int i = 0;
        public int Houses = 32;
        public int Hotels = 12;
        public bool Companyroll = false;
        public string history;
        public Board(int players, string[] names, ImageSource[] images)
        {
            this.players = new Player[players];

            for (int i = 0; i < players; i++)
            {
                this.players[i] = new Player(names[i], images[i]);
            }
            int counter = 0;
            foreach (string line in System.IO.File.ReadLines(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\Init\\" + "Squares_info_init.txt"))
            {

                string[] data = line.Split(',');
                switch (data[0])
                {
                    case "0": // go, tax, super tax
                        squares[counter] = new MoneySquare(data[1], int.Parse(data[2]), int.Parse(data[3]));
                        break;

                    case "1": // property eg 1,Old Kent Road,b,60,2,10,30,90,160,250
                        ushort[] rents = new ushort[6];
                        for (int i = 0; i < data.Length - 5; i++)
                        {
                            rents[i] = ushort.Parse(data[4 + i]);
                        }
                        squares[counter] = new Property(ushort.Parse(data[3]), char.Parse(data[2]), data[1], rents, ushort.Parse(data[10]));
                        break;

                    case "2": // station
                        ushort[] rents1 = new ushort[4];
                        for (int i = 0; i < data.Length - 3; i++)
                        {
                            rents1[i] = ushort.Parse(data[3 + i]);
                        }
                        squares[counter] = new Station(data[1], ushort.Parse(data[2]), rents1);
                        break;

                    case "3": // company
                        squares[counter] = new Company(data[1], ushort.Parse(data[2]));
                        break;

                    case "4": // Comunity chest
                        squares[counter] = new Chance(data[1]);
                        break;

                    case "5": // chance - maybe merge chance and community or not idk we'll see 
                        squares[counter] = new Chance(data[1]);
                        break;

                    case "7": // no action
                        squares[counter] = new NoActionSquare(data[1]);
                        break;
                    case "8": // go to jail - should this even have its own class like idk 
                        squares[counter] = new GoToJail(data[1]);
                        break;

                }
                counter++;
            }// it is midnight and i need to get up so yeah im off see you.
        }
        public Player GetCurrentPlayer()
        {
            return this.players[i];
        }

        public void incPLayer()
        {
            i++;
            if (i == players.Length)
            {
                i = 0;
            }
        }

        public List<BuyableSquare> GetProperties(char type, Player player) {

            List<BuyableSquare> properties = new List<BuyableSquare>();

            foreach (BuyableSquare i in squares.OfType<BuyableSquare>()) {
                if (i.owner == player) {
                    properties.Add(i);
                }
            }

            properties = FilterProperties(type, properties);

            return properties;
        }

        public List<BuyableSquare> FilterProperties(char type, List<BuyableSquare> properties) {
            if (type == 'a')
            {
                return properties;
            }
            else if (type == 's')
            {
                return (List<BuyableSquare>)properties.OfType<Station>();
            }

            else if (type == 'c') {
                return (List<BuyableSquare>)properties.OfType<Company>();
            }

            else {
                List<BuyableSquare> temp = new List<BuyableSquare>();
                foreach (Property i in properties.OfType<Property>()) {
                    if (i.colour == type) temp.Add(i);
                }
                return temp;
            }
        }
    }

    public class Player
    {
        static public Random dice = new Random();
        public int Bal;
        public ImageSource Piece;
        public int Position;
        public bool InJail;
        public int JailRounds = 0;
        public int GetOutOfJail;
        public string Name;
        public Player(string name, ImageSource piece)
        {
            this.Name = name;
            this.Bal = 1500;
            this.Piece = piece;
            this.Position = 0;
            this.InJail = false;
            this.GetOutOfJail = 0;
        }

        public static int[] Roll()
        {
            int roll1 = dice.Next(1, 7);
            int roll2 = dice.Next(1, 7);
            return new int[] { roll1, roll2, roll1 + roll2 };
        }
    }
    public partial class MainWindow : Window
    {
        public Canvas rootCanvas = new Canvas();
        List<BitmapImage> pieces;
        public int PlayerCount;
        string path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public Board board;
        ushort counter = 1;
        BitmapImage[] dice = new BitmapImage[7];
        SoundPlayer play = new SoundPlayer();
        SoundPlayer music = new SoundPlayer();
        Image board_image = new Image();
        ushort doublesCount = 0;
        Dictionary<char, Brush> colours = new Dictionary<char, Brush>();

        public MainWindow()
        {
            InitializeComponent();

            Viewbox main = new Viewbox();
            main.StretchDirection = StretchDirection.Both;
            main.Stretch = Stretch.Fill;
            rootCanvas.Height = root.Height;
            rootCanvas.Width = root.Width;
            rootCanvas.Background = (Brush)new BrushConverter().ConvertFromString("#4B4B4B");
            main.Child = rootCanvas;
            root.Content = main;
            board_image.Source = new BitmapImage(new Uri(path + "\\Assets\\" + "board.jpg"));
            board_image.Height = root.Height;
            board_image.Width = root.Height;
            dice[0] = new BitmapImage(new Uri(path + "\\Assets\\" + "Dice.png"));

            for (int i = 1; i < 7; i++)
            {
                dice[i] = new BitmapImage(new Uri(path + "\\Assets\\" + i.ToString() + ".png"));
            }
            play.SoundLocation = path + "\\Assets\\shake_dice.wav";

            foreach (string line in System.IO.File.ReadLines(path + "\\Init\\" + "Prop_hex.txt"))
            {
                string[] data = line.Split(',');
                colours.Add(char.Parse(data[0]), (Brush)new BrushConverter().ConvertFromString(data[1]));
            }

            Menu();
        }
        public char SmartReverseLookup(Dictionary<char, Brush> me, Brush value)
        {
            return me.First(a => a.Value.Equals(value)).Key;
        }
        public void Menu()
        {
            rootCanvas.Children.Clear();
            Image background = new Image();
            background.Source = new BitmapImage(new Uri(path + "\\Assets\\background.jpg"));
            background.Width = 800;
            background.Height = 300;
            rootCanvas.Children.Add(background);
            Canvas.SetLeft(background, 0);
            Canvas.SetTop(background, 0);

            Image Rating = new Image();
            Rating.Source = new BitmapImage(new Uri(path + "\\Assets\\Rating.png"));
            Rating.Width = 29;
            Rating.Height = 40;
            rootCanvas.Children.Add(Rating);
            Canvas.SetLeft(Rating, 0);
            Canvas.SetTop(Rating, 0);

            TextBlock property = new TextBlock();
            property.Text = "Property Trading Game from Parker Brothers";
            property.FontSize = 7;
            rootCanvas.Children.Add(property);
            Canvas.SetLeft(property, 213);
            Canvas.SetTop(property, 30);
            property = null;
            GC.Collect();

            Image Logo = new Image();
            Logo.Source = new BitmapImage(new Uri(path + "\\Assets\\Logo.png"));
            Logo.Width = 382;
            Logo.Height = 102;
            rootCanvas.Children.Add(Logo);
            Canvas.SetLeft(Logo, 209);
            Canvas.SetTop(Logo, 10);

            Brush buttonbackground = (Brush)new BrushConverter().ConvertFromString("#3FC0FF");

            Button Play = new Button();
            Play.Width = 140;
            Play.Height = 37;
            Play.Content = "PLAY";
            Play.Background = buttonbackground;
            Play.FontSize = 20;
            Play.FontWeight = FontWeights.Bold;
            Play.Click += new RoutedEventHandler(playerselect);
            rootCanvas.Children.Add(Play);
            Canvas.SetLeft(Play, 330);
            Canvas.SetTop(Play, 147);

            Button Help = new Button();
            Help.Width = 96;
            Help.Height = 37;
            Help.Content = "HELP";
            Help.Background = buttonbackground;
            Help.FontSize = 20;
            Help.FontWeight = FontWeights.Bold;
            Help.Click += (sender, e) =>
            {
                System.Diagnostics.Process.Start("https://instructions.hasbro.com/en-gb/instruction/monopoly-classic-game");
            };
            rootCanvas.Children.Add(Help);
            Canvas.SetLeft(Help, 209);
            Canvas.SetTop(Help, 207);

            Button Load = new Button();
            Load.Width = 96;
            Load.Height = 37;
            Load.Content = "LOAD";
            Load.Background = buttonbackground;
            Load.FontSize = 20;
            Load.FontWeight = FontWeights.Bold;
            rootCanvas.Children.Add(Load);
            Canvas.SetLeft(Load, 352);
            Canvas.SetTop(Load, 207);

            Button Exit = new Button();
            Exit.Width = 96;
            Exit.Height = 37;
            Exit.Content = "EXIT";
            Exit.Background = buttonbackground;
            Exit.FontSize = 20;
            Exit.FontWeight = FontWeights.Bold;
            Exit.Click += (sender, e) =>
            {
                System.Environment.Exit(0);
            };
            rootCanvas.Children.Add(Exit);
            Canvas.SetLeft(Exit, 495);
            Canvas.SetTop(Exit, 207);
            // Bottom of the menu page

            TextBlock text1 = new TextBlock();
            text1.FontSize = 20;
            text1.Text = "Not Licenced By:";
            text1.FontWeight = FontWeights.Bold;
            text1.Foreground = Brushes.White;
            rootCanvas.Children.Add(text1);
            Canvas.SetLeft(text1, 2);
            Canvas.SetTop(text1, 300);

            Image Hasbro = new Image();
            Hasbro.Source = new BitmapImage(new Uri(path + "\\Assets\\Hasbro.png"));
            Hasbro.Width = 101;
            Hasbro.Height = 56;
            rootCanvas.Children.Add(Hasbro);
            Canvas.SetLeft(Hasbro, 50);
            Canvas.SetTop(Hasbro, 337);

            Image EA = new Image();
            EA.Source = new BitmapImage(new Uri(path + "\\Assets\\EA.png"));
            EA.Width = 82;
            EA.Height = 82;
            rootCanvas.Children.Add(EA);
            Canvas.SetLeft(EA, 239);
            Canvas.SetTop(EA, 330);

            Image CMG = new Image();
            CMG.Source = new BitmapImage(new Uri(path + "\\Assets\\Cool.png"));
            CMG.Width = 250;
            CMG.Height = 34;
            rootCanvas.Children.Add(CMG);
            Canvas.SetLeft(CMG, 392);
            Canvas.SetTop(CMG, 348);

            Image In7 = new Image();
            In7.Source = new BitmapImage(new Uri(path + "\\Assets\\7.png"));
            In7.Width = 73;
            In7.Height = 70.5;
            rootCanvas.Children.Add(In7);
            Canvas.SetLeft(In7, 700);
            Canvas.SetTop(In7, 336);

            TextBlock text2 = new TextBlock();
            text2.FontSize = 8;
            text2.Width = 786;
            text2.FontSize = 8;
            text2.Text = "The MONOPOLY name and logo, the distinctive design of the game board, the four corner squares, the MR. MONOPOLY name and character, as well as each of the distinctive elements of the board and playing\npieces are trademarks of Hasbro for its property trading game and equipment. © 2023 Hasbro. All Rights Reserved. ";
            text2.Foreground = Brushes.White;
            rootCanvas.Children.Add(text2);
            Canvas.SetLeft(text2, 2);
            Canvas.SetTop(text2, 423);
        }

        public void playerselect(object sender, RoutedEventArgs e)
        {
            rootCanvas.Children.Clear();
            Image background = new Image();
            background.Source = new BitmapImage(new Uri(path + "\\Assets\\background.jpg"));
            background.Width = 1196.47;
            background.Height = 450;
            rootCanvas.Children.Add(background);
            Canvas.SetLeft(background, -198.24);
            Canvas.SetTop(background, 0);
            pieces = new List<BitmapImage>();
            TextBox[] playernames = new TextBox[6];
            Canvas[] SelectCanvas = new Canvas[6];
            string[] strings = { "Car.png", "Iron.png", "Dog.png ", "Ship.png", "Hat.png", "Thimble.png" };
            for (int i = 0; i < 6; i++)
            {
                SelectCanvas[i] = new Canvas();
                SelectCanvas[i].Width = 105;
                SelectCanvas[i].Height = 160;
                SelectCanvas[i].Background = (Brush)new BrushConverter().ConvertFromString("#595959");
                rootCanvas.Children.Add(SelectCanvas[i]);
                Canvas.SetTop(SelectCanvas[i], 107);
                Canvas.SetLeft(SelectCanvas[i], 60 + i * 115);

                playernames[i] = new TextBox();
                playernames[i].Height = 25;
                playernames[i].Cursor = Cursors.IBeam;
                playernames[i].Width = 105;
                playernames[i].Foreground = Brushes.White;
                playernames[i].TextAlignment = TextAlignment.Center;
                playernames[i].VerticalContentAlignment = VerticalAlignment.Center;

                playernames[i].Background = (Brush)new BrushConverter().ConvertFromString("#4B4B4B");
                SelectCanvas[i].Children.Add(playernames[i]);
                Canvas.SetTop(playernames[i], -25);
                Canvas.SetLeft(playernames[i], 0);



                Button button1 = new Button();
                button1.Background = null;
                button1.Foreground = Brushes.White;
                button1.Click += new RoutedEventHandler(PlayerSelectButton);
                button1.Tag = -2;
                button1.Content = "<";
                button1.BorderThickness = new Thickness(0);
                button1.FontSize = 20;
                SelectCanvas[i].Children.Add(button1);
                Canvas.SetTop(button1, 66);
                Canvas.SetLeft(button1, 0);

                Image temp = new Image();
                temp.Height = 62;
                temp.Width = 69; // nice
                SelectCanvas[i].Children.Add(temp);
                Canvas.SetTop(temp, 49);
                Canvas.SetLeft(temp, 18);

                Button button2 = new Button();
                button2.Background = null;
                button2.Foreground = Brushes.White;
                button2.Tag = -1;
                button2.Content = ">";
                button2.Click += new RoutedEventHandler(PlayerSelectButton);
                button2.BorderThickness = new Thickness(0);
                button2.FontSize = 20;
                SelectCanvas[i].Children.Add(button2);
                Canvas.SetTop(button2, 66);
                Canvas.SetLeft(button2, 90);

                Button button3 = new Button();
                button3.Width = 105;
                button3.Click += new RoutedEventHandler(PlayerSelectButton);
                button3.Tag = i + 1;
                button3.Content = "Add";
                button3.FontSize = 16;
                button3.VerticalContentAlignment = VerticalAlignment.Center;
                button3.HorizontalContentAlignment = HorizontalAlignment.Center;
                SelectCanvas[i].Children.Add(button3);
                Canvas.SetTop(button3, 170);
                Canvas.SetLeft(button3, 0);

                pieces.Add(new BitmapImage(new Uri(path + "\\Assets\\" + strings[i])));

                if (i < 2)
                {
                    playernames[i].Text = "Player " + (i + 1).ToString();
                    temp.Source = pieces[0];
                    pieces.Remove(pieces[0]);
                    button3.Content = "Remove";
                }
                else
                {
                    playernames[i].IsReadOnly = true;
                    playernames[i].Text = "Empty";
                    button1.Visibility = Visibility.Hidden;
                    button2.Visibility = Visibility.Hidden;

                    if (i != 2) { button3.Visibility = Visibility.Hidden; }
                }

            }
            PlayerCount = 2;

            Button Ready = new Button();
            Ready.Content = "READY";
            Ready.Width = 115;
            Ready.FontSize = 20;
            Ready.Tag = -3;
            Ready.Click += new RoutedEventHandler(PlayerSelectButton);
            Ready.Background = Brushes.Red;
            Ready.Foreground = Brushes.White;
            rootCanvas.Children.Add(Ready);
            Canvas.SetTop(Ready, 388);
            Canvas.SetLeft(Ready, 358);

        }

        private void PlayerSelectButton(object sender, RoutedEventArgs e)
        {
            int tag = (int)((Button)sender).Tag;
            UIElementCollection children = ((Canvas)((Button)sender).Parent).Children;
            if (tag > 0) // either add or remove
            {
                if ((string)((Button)children[4]).Content == "Add")
                {
                    ((TextBox)children[0]).Text = "Player " + tag.ToString();
                    ((TextBox)children[0]).IsReadOnly = false;
                    ((Button)children[1]).Visibility = Visibility.Visible;
                    ((Button)children[3]).Visibility = Visibility.Visible;
                    ((Image)children[2]).Source = pieces.Last();
                    pieces.Remove(pieces.Last());
                    ((Button)sender).Content = "Remove";
                    PlayerCount += 1;
                    if (tag != 6)
                    {
                        ((Button)((Canvas)((Canvas)((Canvas)((Button)sender).Parent).Parent).Children[tag + 1]).Children[4]).Content = "Add";
                        ((Button)((Canvas)((Canvas)((Canvas)((Button)sender).Parent).Parent).Children[tag + 1]).Children[4]).Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    if (PlayerCount == 2) MessageBox.Show("2 players are required to play", "ERROR");
                    else
                    {
                        if (tag != PlayerCount)
                        {
                            UIElementCollection childrenNeigh = ((Canvas)((Canvas)((Canvas)((Button)sender).Parent).Parent).Children[tag + 1]).Children;

                            if (((TextBox)childrenNeigh[0]).Text.Split(' ')[0] == "Player") ((TextBox)children[0]).Text = "Player " + (tag).ToString();
                            else ((TextBox)children[0]).Text = ((TextBox)childrenNeigh[0]).Text;

                            if (e.Source == sender)
                            {
                                pieces.Add((BitmapImage)((Image)children[2]).Source);
                                ((Image)children[2]).Source = null;
                            }

                            BitmapImage temp = (BitmapImage)((Image)childrenNeigh[2]).Source;
                            ((Image)childrenNeigh[2]).Source = null;
                            ((Image)children[2]).Source = temp;
                            PlayerSelectButton((Button)childrenNeigh[4], e);
                        }
                        else
                        {
                            ((TextBox)children[0]).Text = "Empty";
                            ((TextBox)children[0]).IsReadOnly = true;
                            ((Button)children[1]).Visibility = Visibility.Hidden;
                            ((Button)children[3]).Visibility = Visibility.Hidden;
                            if (e.Source == sender)
                            {
                                pieces.Add((BitmapImage)((Image)children[2]).Source);
                                ((Image)children[2]).Source = null;
                            }
                            ((Button)children[4]).Content = "Add";
                            if (tag != 6)
                            {
                                ((Button)((Canvas)((Canvas)((Canvas)((Button)sender).Parent).Parent).Children[tag + 1]).Children[4]).Visibility = Visibility.Hidden;
                            }
                        }

                        if (e.Source == sender)
                        {
                            PlayerCount -= 1;
                        }
                    }
                }
            }

            else if (tag == -1)
            { // right arrow
                pieces.Insert(0, (BitmapImage)((Image)children[2]).Source);
                ((Image)children[2]).Source = pieces.Last();
                pieces.Remove(pieces.Last());
            }

            else if (tag == -2)
            {// left arrow
                if (pieces.Count != 0)
                {
                    BitmapImage temp = (BitmapImage)((Image)children[2]).Source;
                    ((Image)children[2]).Source = pieces.First();
                    pieces.Remove(pieces.First());
                    pieces.Add(temp);
                }
            }

            else
            {
                string[] names = new string[PlayerCount];
                ImageSource[] icons = new ImageSource[PlayerCount];
                for (int i = 1; i < PlayerCount + 1; i++)
                {
                    names[i - 1] = ((TextBox)((Canvas)children[i]).Children[0]).Text;
                    icons[i - 1] = ((Image)((Canvas)children[i]).Children[2]).Source;
                }
                board = new Board(PlayerCount, names, icons);
                pieces.Clear();
                pieces = null;
                GC.Collect();
                highestdice();
            }
        }

        public void highestdice()
        {
            Image background = (Image)rootCanvas.Children[0];
            rootCanvas.Children.Clear();
            rootCanvas.Children.Add(background);

            Button roll = new Button();
            roll.Width = 121;
            roll.Height = 34;
            roll.FontSize = 16;
            roll.BorderThickness = new Thickness(0);
            roll.Content = "Roll Dice";
            roll.Foreground = Brushes.White;
            roll.Background = Brushes.DeepSkyBlue;
            roll.Click += new RoutedEventHandler(Diceroll);
            rootCanvas.Children.Add(roll);
            Canvas.SetTop(roll, 16);
            Canvas.SetLeft(roll, 340);

            for (int i = 0; i < 2; i++)
            {
                Image diceroll = new Image();
                diceroll.Width = 92;
                diceroll.Height = 92;
                diceroll.Source = dice[0];
                rootCanvas.Children.Add(diceroll);
                Canvas.SetTop(diceroll, 155);
                Canvas.SetLeft(diceroll, 298 + 113 * i);
            }

            for (int i = 0; i < PlayerCount; i++)
            {
                Canvas player = new Canvas();
                player.Width = 66;
                player.Height = 92;
                player.Background = Brushes.White;
                rootCanvas.Children.Add(player);
                if (i == 0) Canvas.SetTop(player, 345 - 18);

                else Canvas.SetTop(player, 345);
                Canvas.SetLeft(player, PlayerCount * -37.5 + 404.5 + i * 69);

                TextBlock score = new TextBlock();
                score.Width = 66;
                score.TextAlignment = TextAlignment.Center;
                score.FontSize = 10;
                player.Children.Add(score);
                Canvas.SetTop(score, -2);

                TextBlock name = new TextBlock();
                name.Text = board.players[i].Name;
                name.Width = 66;
                name.TextAlignment = TextAlignment.Center;
                name.FontSize = 14;
                player.Children.Add(name);
                Canvas.SetTop(name, 10);

                Image piece = new Image();
                piece.Width = 66;
                piece.Height = 54;
                piece.HorizontalAlignment = HorizontalAlignment.Center;
                piece.Source = board.players[i].Piece;
                player.Children.Add(piece);
                Canvas.SetTop(piece, 33);
            }
        }

        private async void Diceroll(object sender, RoutedEventArgs e)
        {
            ((Button)sender).IsEnabled = false;
            //Clear previous roll image
            ((Image)((Canvas)((Button)sender).Parent).Children[2]).Source = dice[0];
            //Roll dice two as well
            ((Image)((Canvas)((Button)sender).Parent).Children[3]).Source = dice[0];
            //Dice sound
            play.Play();
            await Task.Delay(500);

            //Roll the dice and update display
            int[] info = Player.Roll();
            ((Image)((Canvas)((Button)sender).Parent).Children[2]).Source = dice[info[0]];
            ((Image)((Canvas)((Button)sender).Parent).Children[3]).Source = dice[info[1]];
            ((Button)sender).IsEnabled = true;
            // move the next player icon up

            Canvas.SetTop(((Canvas)((Canvas)((Button)sender).Parent).Children[counter + 3]), 345);
            ((TextBlock)((Canvas)((Canvas)((Button)sender).Parent).Children[counter + 3]).Children[0]).Text = info[2].ToString();
            if (counter != PlayerCount)
            {
                Canvas.SetTop(((Canvas)((Canvas)((Button)sender).Parent).Children[counter + 4]), 345 - 18);
            }

            else
            { // if it is the last player then start the game in order (for now ignore duplicates)
                ((Button)sender).Visibility = Visibility.Hidden;
                int[] scores = new int[PlayerCount];
                for (int i = 0; i < PlayerCount; i++)
                {
                    scores[i] = int.Parse(((TextBlock)((Canvas)((Canvas)((Button)sender).Parent).Children[i + 4]).Children[0]).Text);
                }
                int temp;
                Player temp1;

                for (int j = 0; j <= scores.Length - 2; j++) // bubble sort because i cba to implement something more efficent
                {
                    for (int i = 0; i <= scores.Length - 2; i++)
                    {
                        if (scores[i] < scores[i + 1])
                        {
                            temp = scores[i + 1];
                            scores[i + 1] = scores[i];
                            scores[i] = temp;

                            temp1 = board.players[i + 1];
                            board.players[i + 1] = board.players[i];
                            board.players[i] = temp1;
                        }
                    }
                }
                GameScreen(sender, e);
                // sort the scores and switch the board players array so that the players are in the right order (if scores are tied the player who is earliest in the array goes first bc lazy and like cba)
            }
            counter++;
        }

        public void GameScreen(object sender, RoutedEventArgs e)
        {
            rootCanvas.Children.Clear();
            rootCanvas.Children.Add(board_image);
            // place players on board
            foreach (Player player in board.players)
            {
                Image temp = new Image();
                temp.Width = 17;
                temp.Height = 17;
                temp.Source = player.Piece;
                rootCanvas.Children.Add(temp);
                move_player(temp, player.Position);
            }

            Player currentPlayer = board.GetCurrentPlayer();
            UIElement[] playerinfo = new UIElement[5]; // Text blocks to display the players information

            for (int i = 0; i < 4; i++) // we only want 4 text blocks the 5th element is reserved for the players icon
            {
                TextBlock temp = new TextBlock();
                temp.Foreground = Brushes.White;
                temp.FontSize = 18;

                switch (i)
                {
                    case 0:
                        temp.Text = currentPlayer.Name;
                        break;
                    case 1:
                        temp.Text = "Balance: " + currentPlayer.Bal.ToString();
                        break;
                    case 2:
                        temp.Text = "Position: " + currentPlayer.Position.ToString();
                        break;
                    case 3:
                        temp.Text = "Jail free: " + currentPlayer.GetOutOfJail.ToString();
                        break;
                }
                rootCanvas.Children.Add(temp);
                Canvas.SetTop(temp, 10 + 30 * i);
                Canvas.SetLeft(temp, 465);
                playerinfo[i] = temp;

            }

            Image playerpiece = new Image();
            playerpiece.Width = 69;
            playerpiece.Height = 69;
            playerpiece.Source = currentPlayer.Piece;
            playerinfo[4] = playerpiece;
            rootCanvas.Children.Add(playerpiece);
            Canvas.SetTop(playerpiece, 20);
            Canvas.SetLeft(playerpiece, 710);

            Button roll = new Button();
            roll.Content = "Roll Dice";
            roll.Click += new RoutedEventHandler(RollDice);
            roll.FontSize = 25;
            roll.Width = 116;
            roll.Height = 42;
            rootCanvas.Children.Add(roll);
            Canvas.SetTop(roll, 150);
            Canvas.SetLeft(roll, 645);

            for (int i = 0; i < 2; i++)
            {
                Image diceimg = new Image();
                diceimg.Width = 40;
                diceimg.Height = 40;
                diceimg.Source = dice[0];
                rootCanvas.Children.Add(diceimg);
                Canvas.SetTop(diceimg, 150);
                Canvas.SetLeft(diceimg, 495 + i * 50);
            }

            Button trade = new Button();
            trade.Click += new RoutedEventHandler(TradeScreen);
            trade.Content = "Trade";
            trade.FontSize = 25;
            trade.Width = 116;
            trade.Height = 42;
            rootCanvas.Children.Add(trade);
            Canvas.SetTop(trade, 210);
            Canvas.SetLeft(trade, 490);

            Button properties = new Button();
            properties.Content = "Properties";
            properties.FontSize = 25;
            properties.Width = 116;
            properties.Height = 42;
            properties.Click += PropertyScreen;
            rootCanvas.Children.Add(properties);
            Canvas.SetTop(properties, 210);
            Canvas.SetLeft(properties, 645);

            ScrollViewer scroll = new ScrollViewer();
            scroll.Width = 320;
            scroll.Height = 110;

            TextBlock history = new TextBlock();
            history.Text += board.history + currentPlayer.Name + "'s turn\n";
            history.FontSize = 10;
            history.Background = Brushes.White;
            history.Foreground = Brushes.Black;
            history.TextAlignment = TextAlignment.Center;
            scroll.Content = history;
            rootCanvas.Children.Add(scroll);
            Canvas.SetTop(scroll, 295);
            Canvas.SetLeft(scroll, 456);

            Button EndTurn = new Button();
            EndTurn.Content = "End Turn";
            EndTurn.IsEnabled = false;
            EndTurn.FontSize = 25;
            EndTurn.Width = 125;
            EndTurn.Height = 35;
            EndTurn.Click += new RoutedEventHandler(End_Turn);

            rootCanvas.Children.Add(EndTurn);
            Canvas.SetTop(EndTurn, 411);
            Canvas.SetLeft(EndTurn, 475);

            Button Menu = new Button();
            Menu.Content = "Main Menu";
            Menu.FontSize = 25;
            Menu.Width = 130;
            Menu.Height = 35;
            rootCanvas.Children.Add(Menu);
            Canvas.SetTop(Menu, 411);
            Canvas.SetLeft(Menu, 640);

            // pay fine
            Button fine = new Button();
            fine.Content = "Pay £50 fine";
            rootCanvas.Children.Add(fine);
            fine.Visibility = Visibility.Hidden;
            fine.Click += (sender1, e1) => {
                Player player = board.GetCurrentPlayer();
                player.Bal -= 50;
                player.InJail = false;
                player.Position = 10;
                ((TextBlock)playerinfo[1]).Text = "Balance: " + player.Bal.ToString();
                ((Button)sender).Visibility = Visibility.Hidden;
                history.Text += player.Name + "paid £50 to get out of jail\n";
                board.history += player.Name + "paid £50 to get out of jail\n";

            };
            Canvas.SetLeft(fine, 710);
            Canvas.SetTop(fine, 89);

            // get out of jail free
            Button getout = new Button();
            getout.Visibility = Visibility.Hidden;
            getout.Content = "Use get of jail card";
            getout.Click += (sender1, e1) => {
                Player player = board.GetCurrentPlayer();
                player.GetOutOfJail--;
                player.InJail = false;
                player.Position = 10;
                ((TextBlock)playerinfo[3]).Text = "Jail free: " + currentPlayer.GetOutOfJail.ToString();
                ((Button)sender).Visibility = Visibility.Hidden;
            };
            rootCanvas.Children.Add(getout);
            Canvas.SetLeft(getout, 675);
            Canvas.SetTop(getout, 120);
        }

        public async void RollDice(object sender, RoutedEventArgs e)
        {
            Player current = board.GetCurrentPlayer();
            ((Button)sender).IsEnabled = false;
            ((Image)((Canvas)((Button)sender).Parent).Children[PlayerCount + 7]).Source = dice[0];
            ((Image)((Canvas)((Button)sender).Parent).Children[PlayerCount + 8]).Source = dice[0];

            play.Play();
            await Task.Delay(500);

            //Roll the dice and update display
            int[] info = Player.Roll();
            ((Image)((Canvas)((Button)sender).Parent).Children[PlayerCount + 7]).Source = dice[info[0]];
            ((Image)((Canvas)((Button)sender).Parent).Children[PlayerCount + 8]).Source = dice[info[1]];

            ((TextBlock)((ScrollViewer)((Canvas)((Button)sender).Parent).Children[PlayerCount + 11]).Content).Text += current.Name + " rolled a " + info[0].ToString() + " and a " + info[1].ToString() + "\n";
            board.history += current.Name + " rolled a " + info[0].ToString() + " and a " + info[1].ToString() + "\n";

            if (info[0] == info[1])
            {
                ((Button)sender).IsEnabled = true;
                ((TextBlock)((ScrollViewer)((Canvas)((Button)sender).Parent).Children[PlayerCount + 11]).Content).Text += current.Name + " rolled doubles!" + "\n";
                board.history += current.Name + " rolled doubles!" + "\n";
                doublesCount++;
                if (current.InJail == true)
                {
                    current.InJail = false;
                    doublesCount = 0;
                    current.JailRounds = 0;
                    current.Position = 10;
                    ((Button)((Canvas)((Button)sender).Parent).Children[PlayerCount + 12]).IsEnabled = true;
                    ((Button)sender).IsEnabled = false;
                }

                else if (doublesCount == 3)
                {
                    ((TextBlock)((ScrollViewer)((Canvas)((Button)sender).Parent).Children[PlayerCount + 11]).Content).Text += current.Name + " rolled 3 doubles in a row and goes to jail!" + "\n";
                    board.history += current.Name + " rolled 3 doubles in a row and goes to jail!" + "\n";
                    current.InJail = true;
                    current.Position = 30;
                    End_Turn(sender, e);

                    int index = Array.IndexOf(board.players, current);
                    Image temp = ((Image)((Canvas)((Button)sender).Parent).Children[index + 1]);
                    move_player(temp, current.Position);
                    return;
                }
            }

            else ((Button)((Canvas)((Button)sender).Parent).Children[PlayerCount + 12]).IsEnabled = true;

            if (current.InJail == false && board.Companyroll == false)
            {
                int index = Array.IndexOf(board.players, current);

                Image temp = ((Image)((Canvas)((Button)sender).Parent).Children[index + 1]);
                current.Position += info[2];

                if (current.Position >= 40)
                {
                    current.Position -= 40;
                    board.squares[0].Action(current);
                    ((TextBlock)((ScrollViewer)(rootCanvas).Children[PlayerCount + 11]).Content).Text += current.Name + " passed go and collects £200" + "\n";
                    board.history += current.Name + " passed go and collects £200" + "\n";

                }

                ((TextBlock)((Canvas)((Button)sender).Parent).Children[PlayerCount + 3]).Text = "Position: " + current.Position.ToString();

                move_player(temp, current.Position);
                ((TextBlock)((ScrollViewer)(rootCanvas).Children[PlayerCount + 11]).Content).Text += current.Name + " landed on " + board.squares[current.Position].name + "\n";
                board.history += current.Name + " landed on " + board.squares[current.Position].name + "\n";
                if (board.squares[current.Position] is MoneySquare && current.Position != 0)
                {
                    ((TextBlock)((ScrollViewer)(rootCanvas).Children[PlayerCount + 11]).Content).Text += current.Name + " pays £" + ((MoneySquare)board.squares[current.Position]).amount.ToString() + "\n";
                    board.history += current.Name + " pays £" + ((MoneySquare)board.squares[current.Position]).amount.ToString() + "\n";

                }
                else if (board.squares[current.Position] is Company)
                {
                    if (((Company)board.squares[current.Position]).owner != null)
                    {
                        board.Companyroll = true;
                        ((Button)sender).IsEnabled = true;
                        ((Button)((Canvas)((Button)sender).Parent).Children[PlayerCount + 12]).IsEnabled = false;
                        ((TextBlock)((ScrollViewer)(rootCanvas).Children[PlayerCount + 11]).Content).Text += current.Name + " has to roll to calculate rent." + "\n";
                        board.history += current.Name + " has to roll to calculate rent." + "\n";
                        return;
                    }
                    else
                    {
                        ((Company)board.squares[current.Position]).Action(current, info[2]);
                        return;
                    }
                }

                board.squares[current.Position].Action(current);
                ((TextBlock)(rootCanvas).Children[PlayerCount + 2]).Text = "Balance: " + current.Bal.ToString();

            }

            else if (board.Companyroll)
            {
                ((Company)board.squares[current.Position]).Action(current, info[2]);
                board.Companyroll = false;
            }

            else
            {
                if (current.JailRounds == 3) {
                    current.InJail = false;
                    current.Position = 10;
                    current.JailRounds = 0;
                    current.Bal -= 50;
                    ((TextBlock)((ScrollViewer)((Canvas)((Button)sender).Parent).Children[PlayerCount + 11]).Content).Text += current.Name + " paid £50 fine." + "\n";
                    board.history += current.Name + " paid £50 fine." + "\n";

                }
                ((Button)((Canvas)((Button)sender).Parent).Children[PlayerCount + 14]).Visibility = Visibility.Hidden;

            }
        }

        public void End_Turn(object sender, RoutedEventArgs e)
        {
            doublesCount = 0;
            board.incPLayer();
            Player current = board.GetCurrentPlayer();
            ((Button)((Canvas)((Button)sender).Parent).Children[PlayerCount + 6]).IsEnabled = true;
            ((TextBlock)((ScrollViewer)((Canvas)((Button)sender).Parent).Children[PlayerCount + 11]).Content).Text += "\n" + current.Name + "'s turn\n";
            board.history += "\n" + current.Name + "'s turn\n";
            ((TextBlock)((Canvas)((Button)sender).Parent).Children[PlayerCount + 1]).Text = current.Name;
            ((TextBlock)((Canvas)((Button)sender).Parent).Children[PlayerCount + 2]).Text = "Balance: " + current.Bal.ToString();
            ((TextBlock)((Canvas)((Button)sender).Parent).Children[PlayerCount + 3]).Text = "Position: " + current.Position.ToString();
            ((TextBlock)((Canvas)((Button)sender).Parent).Children[PlayerCount + 4]).Text = "Jail free: " + current.GetOutOfJail.ToString();
            ((Image)((Canvas)((Button)sender).Parent).Children[PlayerCount + 5]).Source = current.Piece;
            ((Button)((Canvas)((Button)sender).Parent).Children[PlayerCount + 12]).IsEnabled = false;
            ((ScrollViewer)((Canvas)((Button)sender).Parent).Children[PlayerCount + 11]).ScrollToBottom();

            if (current.InJail)
            {
                current.JailRounds++;
                ((Button)rootCanvas.Children[PlayerCount + 14]).Visibility = Visibility.Visible;
                if (current.GetOutOfJail > 0)
                {
                    ((Button)rootCanvas.Children[PlayerCount + 15]).Visibility = Visibility.Visible;
                }
            }
        }

        public void move_player(Image temp, int position)
        {


            if (position == 0)
            {
                Canvas.SetTop(temp, 415);
                Canvas.SetLeft(temp, 412);
            }
            else if (position < 10)
            {
                Canvas.SetTop(temp, 422);
                Canvas.SetLeft(temp, 410 - 38 * (position % 10));
            }
            else if (position == 10)
            {
                Canvas.SetTop(temp, 433);
                Canvas.SetLeft(temp, 0);
            }

            else if (position < 20)
            {
                Canvas.SetTop(temp, 404 - 38 * (position % 10));
                Canvas.SetLeft(temp, 11);
            }

            else if (position == 20)
            {
                Canvas.SetTop(temp, 21);
                Canvas.SetLeft(temp, 21);
            }

            else if (position < 30)
            {
                Canvas.SetTop(temp, 14);
                Canvas.SetLeft(temp, 30 + 38 * (position % 10));
            }

            else if (position == 30)
            {
                Canvas.SetTop(temp, 405);
                Canvas.SetLeft(temp, 29);
            }

            else if (position < 40)
            {
                Canvas.SetTop(temp, 31 + 38 * (position % 10));
                Canvas.SetLeft(temp, 422);
            }
        }

        public void ChanceScreen(string message, bool type) {
            string typecard;
            if (type) typecard = "Chance";
            else typecard = "Community Chest";
            Rectangle test = new Rectangle();
            test.Opacity = 0.75;
            test.Fill = Brushes.Black;
            test.Height = rootCanvas.Height;
            test.Width = rootCanvas.Width;
            rootCanvas.Children.Add(test);

            Canvas Chance = new Canvas();
            Chance.Width = 380;
            Chance.Height = 190;
            Chance.Background = Brushes.White;
            rootCanvas.Children.Add(Chance);
            Canvas.SetTop(Chance, 70);
            Canvas.SetLeft(Chance, 210);

            TextBlock title = new TextBlock();
            title.Text = typecard;
            title.FontSize = 25;
            title.Width = Chance.Width;
            title.TextAlignment = TextAlignment.Center;
            Chance.Children.Add(title);
            Canvas.SetTop(title, 5);

            Image image = new Image();
            image.Source = new BitmapImage(new Uri(path + "\\Assets\\" + typecard + ".png"));
            if (type)
            {
                image.Width = 60;
                image.Height = 120;
                Canvas.SetLeft(image, 280);
            }
            else {
                image.Width = 120;
                image.Height = 120;
                Canvas.SetLeft(image, 240);
            }
            Chance.Children.Add(image);
            Canvas.SetTop(image, 36);


            Border messagelabel = new Border();
            messagelabel.Width = 175;
            messagelabel.Height = Chance.Height;

            TextBlock messagelabelcontent = new TextBlock();
            messagelabelcontent.Background = null;
            messagelabelcontent.FontSize = 16;
            messagelabelcontent.Text = message;
            messagelabelcontent.TextAlignment = TextAlignment.Center;
            messagelabelcontent.TextWrapping = TextWrapping.Wrap;
            messagelabelcontent.VerticalAlignment = VerticalAlignment.Center;
            messagelabelcontent.HorizontalAlignment = HorizontalAlignment.Center;
            messagelabel.Child = messagelabelcontent;
            Chance.Children.Add(messagelabel);
            Canvas.SetLeft(messagelabel, 10);

            TextBlock rights = new TextBlock();
            rights.Text = "©" + DateTime.Now.Year.ToString() + " not Hasbro. All rights reserved.";
            rights.Width = Chance.Width;
            rights.TextAlignment = TextAlignment.Center;
            Chance.Children.Add(rights);
            Canvas.SetBottom(rights, 5);

            Button Ok = new Button();
            Ok.Width = 146;
            Ok.Height = 48;
            Ok.Content = "OK";
            Ok.Click += (sender, e) =>
            {
                ((TextBlock)((ScrollViewer)(rootCanvas).Children[PlayerCount + 11]).Content).Text += board.GetCurrentPlayer().Name + " took a " + typecard + " card:" + "\n" + message;
                board.history += board.GetCurrentPlayer().Name + " took a " + typecard + " card:" + "\n" + message;
                rootCanvas.Children.RemoveRange(PlayerCount + 16, rootCanvas.Children.Count - (PlayerCount + 16));
            };
            rootCanvas.Children.Add(Ok);
            Ok.FontSize = 27;

            Canvas.SetTop(Ok, 377);
            Canvas.SetLeft(Ok, 332);
        }

        public void AuctionScreen(Canvas propertycard, BuyableSquare prop) {
            List<int> playersin = new List<int>();
            for (int i = 0; i < PlayerCount; i++) {
                playersin.Add(i);
            }
            int curentIndex = 0;
            int highestbid;
            Player current = board.players[0];
            Canvas[] players = new Canvas[PlayerCount];
            ScaleTransform scale = new ScaleTransform(propertycard.LayoutTransform.Value.M11 * 0.6, propertycard.LayoutTransform.Value.M22 * 0.6);
            propertycard.LayoutTransform = scale;
            propertycard.UpdateLayout();
            rootCanvas.Children.RemoveRange(PlayerCount + 17, rootCanvas.Children.Count - (PlayerCount + 17));

            for (int i = 0; i < PlayerCount; i++)
            {
                players[i] = new Canvas();
                players[i].Width = 66;
                players[i].Height = 108;
                players[i].Background = Brushes.White;
                rootCanvas.Children.Add(players[i]);
                if (i == 0) Canvas.SetTop(players[i], 345 - 18);

                else Canvas.SetTop(players[i], 345);
                Canvas.SetLeft(players[i], PlayerCount * -37.5 + 404.5 + i * 69);

                TextBlock name = new TextBlock();
                name.Text = board.players[i].Name;
                name.Width = 66;
                name.TextWrapping = TextWrapping.Wrap;
                name.TextAlignment = TextAlignment.Center;
                name.FontSize = 14;
                players[i].Children.Add(name);
                Canvas.SetTop(name, -2);

                TextBlock bal = new TextBlock();
                bal.FontSize = 14;
                bal.Width = 66;
                bal.TextAlignment = TextAlignment.Center;
                bal.Text = "£" + board.players[i].Bal.ToString();
                players[i].Children.Add(bal);
                Canvas.SetTop(bal, 20);

                Image piece = new Image();
                piece.Width = 66;
                piece.Height = 54;
                piece.HorizontalAlignment = HorizontalAlignment.Center;
                piece.Source = board.players[i].Piece;
                players[i].Children.Add(piece);
                Canvas.SetTop(piece, 45);
            }

            rootCanvas.Children.Add(propertycard);
            Canvas.SetLeft(propertycard, 10);
            Canvas.SetTop(propertycard, 60);

            Canvas BidCanvas = new Canvas();
            BidCanvas.Height = 150;
            BidCanvas.Width = 300;
            BidCanvas.Background = Brushes.Gray;
            rootCanvas.Children.Add(BidCanvas);
            Canvas.SetLeft(BidCanvas, 258);
            Canvas.SetTop(BidCanvas, 100);

            TextBlock Original = new TextBlock();
            Original.Foreground = Brushes.White;
            Original.Text = "Original Value: £" + prop.price.ToString();
            Original.FontSize = 20;
            rootCanvas.Children.Add(Original);
            Canvas.SetRight(Original, 20);
            Canvas.SetTop(Original, 20);

            TextBlock Highest = new TextBlock();
            Highest.Text = "Highest Bid: £0";
            Highest.Foreground = Brushes.White;
            Highest.FontSize = 20;
            rootCanvas.Children.Add(Highest);
            Canvas.SetRight(Highest, 20);
            Canvas.SetTop(Highest, 50);

            TextBlock Title = new TextBlock();
            Title.Text = "Place your bid:";
            Title.FontSize = 27;
            Title.Width = 300;
            Title.Foreground = Brushes.White;
            Title.TextAlignment = TextAlignment.Center;
            BidCanvas.Children.Add(Title);
            Canvas.SetTop(Title, 5);

            TextBlock Price = new TextBlock();
            Price.Width = 77;
            Price.Height = 33;
            Price.Background = Brushes.White;
            Price.Text = "£1";
            Price.TextAlignment = TextAlignment.Center;
            BidCanvas.Children.Add(Price);
            Canvas.SetTop(Price, 78);
            Canvas.SetLeft(Price, 43);

            RepeatButton dec = new RepeatButton();
            Image decImage = new Image();
            decImage.Source = new BitmapImage(new Uri(path + "\\Assets\\" + "dec.png"));
            decImage.Width = 20;
            decImage.Height = 22;
            dec.BorderThickness = new Thickness(0);
            dec.Background = null;
            dec.Content = decImage;
            dec.Click += (sender, e) =>
            {

                if (int.Parse(Price.Text.Substring(1)) == int.Parse(Highest.Text.Substring(14)) + 1)
                {
                    dec.IsEnabled = false;
                }
                else { Price.Text = "£" + (int.Parse(Price.Text.Substring(1)) - 1).ToString(); }
            };

            BidCanvas.Children.Add(dec);
            Canvas.SetTop(dec, 95);
            Canvas.SetLeft(dec, 130);

            RepeatButton inc = new RepeatButton();
            Image incImage = new Image();
            incImage.Source = new BitmapImage(new Uri(path + "\\Assets\\" + "inc.png"));
            incImage.Width = 20;
            incImage.Height = 22;
            inc.Background = null;
            inc.Content = incImage;
            inc.Click += (sender, e) => {
                Price.Text = "£" + (int.Parse(Price.Text.Substring(1)) + 1).ToString();
                if (int.Parse(Price.Text.Substring(1)) <= 1)
                {
                    dec.IsEnabled = false;
                }
                else {
                    dec.IsEnabled = true;
                }
                if (int.Parse(Price.Text.Substring(1)) == current.Bal - 1) {
                    inc.IsEnabled = false;
                }
            };

            inc.BorderThickness = new Thickness(0);
            BidCanvas.Children.Add(inc);
            Canvas.SetTop(inc, 65);
            Canvas.SetLeft(inc, 130);

            Button BidButton = new Button();
            BidButton.Width = 90;
            BidButton.Height = 40;
            BidButton.FontSize = 18;
            BidButton.Tag = 0;
            BidButton.Background = Brushes.White;
            BidButton.Click += (sender, e) =>
            {
                //change higest bid to new bid, change bid textblock, switch to next user that is in the auction
                highestbid = int.Parse(Price.Text.Substring(1));
                Canvas.SetTop(players[playersin[curentIndex]], 345);
                curentIndex++;
                if (curentIndex == playersin.Count) curentIndex = 0;
                Highest.Text = "Highest Bid: £" + highestbid.ToString();
                Canvas.SetTop(players[playersin[curentIndex]], 345 - 18);
                Price.Text = "£" + (highestbid + 1).ToString();

            };
            BidButton.Content = "BID";
            BidCanvas.Children.Add(BidButton);
            Canvas.SetTop(BidButton, 55);
            Canvas.SetLeft(BidButton, 175);

            Button Fold = new Button();
            Fold.Width = 90;
            Fold.Height = 40;
            Fold.Tag = 0;
            Fold.Click += (sender, e) =>
            {
                Canvas.SetTop(players[playersin[curentIndex]], 345);
                Rectangle temp = new Rectangle();
                temp.Height = players[curentIndex].Height;
                temp.Width = players[curentIndex].Width;
                temp.Fill = Brushes.Black;
                temp.Opacity = 0.6;
                players[playersin[curentIndex]].Children.Add(temp);
                playersin.RemoveAt(curentIndex);
                if (playersin.Count == 1) {
                    prop.Buy(board.players[playersin[0]], prop.price);
                    ((TextBlock)((ScrollViewer)(rootCanvas).Children[PlayerCount + 11]).Content).Text += board.players[playersin[0]].Name + " bought " + prop.name + "\n";
                    board.history += board.players[playersin[0]].Name + " bought " + prop.name + "\n";
                    ((TextBlock)(rootCanvas).Children[PlayerCount + 2]).Text = "Balance: " + board.players[playersin[0]].Bal.ToString();
                    rootCanvas.Children.RemoveRange(PlayerCount + 16, rootCanvas.Children.Count - (PlayerCount + 16));
                    return;
                }
                else if (curentIndex == playersin.Count) curentIndex = 0;

                Canvas.SetTop(players[playersin[curentIndex]], 345 - 18);

            };
            Fold.FontSize = 18;
            Fold.Background = Brushes.White;
            Fold.Content = "FOLD";
            Canvas.SetTop(Fold, 105);
            Canvas.SetLeft(Fold, 175);
            BidCanvas.Children.Add(Fold);

        }

        private Canvas CreateCard(BuyableSquare prop) {
            Canvas propertycard = new Canvas();
            propertycard.Width = 175;
            propertycard.Height = 235;
            propertycard.Background = Brushes.White;

            if (prop == null) {
                return propertycard;
            }

            Label name = new Label();
            name.Width = 175;

            TextBlock namecontent = new TextBlock();
            namecontent.Background = null;
            namecontent.FontSize = 16;
            namecontent.Text = prop.name.ToUpper();
            namecontent.TextAlignment = TextAlignment.Center;
            namecontent.TextWrapping = TextWrapping.Wrap;
            name.Content = namecontent;
            name.VerticalContentAlignment = VerticalAlignment.Center;
            name.HorizontalContentAlignment = HorizontalAlignment.Center;
            propertycard.Children.Add(name);
            if (prop is Property)
            {
                name.Height = 60;
                name.Background = colours[((Property)prop).colour];
                switch (((Property)prop).colour)
                {
                    case 'b':
                        namecontent.Foreground = Brushes.White; break;
                    case 'd':
                        namecontent.Foreground = Brushes.White; break;
                    default:
                        namecontent.Foreground = Brushes.Black; break;

                }

                TextBlock rent = new TextBlock();
                rent.Width = 175;
                rent.Text = "RENT £" + ((Property)prop).rents[0].ToString();
                rent.TextAlignment = TextAlignment.Center;
                rent.FontSize = 11;
                propertycard.Children.Add(rent);
                Canvas.SetTop(rent, 65);


                TextBlock rents = new TextBlock();
                rents.Text = "With 1 House\r\nWith 2 Houses\r\nWith 3 Houses\r\nWith 4 Houses";
                rents.FontSize = 11;
                propertycard.Children.Add(rents);
                Canvas.SetTop(rents, 85);
                Canvas.SetLeft(rents, 13);

                TextBlock rentsvalues = new TextBlock();
                rentsvalues.Text = "£" + ((Property)prop).rents[1].ToString() + "\r\n" + ((Property)prop).rents[2].ToString() + "\r\n" + ((Property)prop).rents[3].ToString() + "\r\n" + ((Property)prop).rents[4].ToString();
                rentsvalues.FontSize = 11;
                rentsvalues.TextAlignment = TextAlignment.Right;
                propertycard.Children.Add(rentsvalues);
                Canvas.SetTop(rentsvalues, 85);
                Canvas.SetRight(rentsvalues, 15);

                TextBlock hotel = new TextBlock();
                hotel.TextAlignment = TextAlignment.Center;
                hotel.Text = "With HOTEL £" + ((Property)prop).rents[5].ToString();
                hotel.Width = 175;
                propertycard.Children.Add(hotel);
                Canvas.SetTop(hotel, 146);

                TextBlock values = new TextBlock();
                values.Width = 175;
                values.TextAlignment = TextAlignment.Center;
                values.Text = "Mortgage Value " + prop.morgage.ToString() + "\r\nHouses cost £" + ((Property)prop).housescost.ToString() + " each.\r\nHotels, £" + ((Property)prop).housescost.ToString() + ". plus 4 houses";
                propertycard.Children.Add(values);
                Canvas.SetTop(values, 172);

                // add property code
            }

            else if (prop is Station)
            {
                Canvas.SetTop(name, 72);

                Image train_icon = new Image();
                train_icon.Width = 67;
                train_icon.Height = 54;
                train_icon.Source = new BitmapImage(new Uri(path + "\\Assets\\" + "train.png"));
                propertycard.Children.Add(train_icon);
                Canvas.SetTop(train_icon, 5);
                Canvas.SetLeft(train_icon, 58);

                TextBlock rents = new TextBlock();
                rents.Text = "Rent\nIf 2 railroads are owned\nIf 3 railroads are owned\nIf 4 railroads are owned";
                rents.FontSize = 11;
                propertycard.Children.Add(rents);
                Canvas.SetTop(rents, 133);
                Canvas.SetLeft(rents, 15.5);

                TextBlock rentsvalues = new TextBlock();
                rentsvalues.Text = "£" + ((Station)prop).rents[0].ToString() + "\r\n" + ((Station)prop).rents[1].ToString() + "\r\n" + ((Station)prop).rents[2].ToString() + "\r\n" + ((Station)prop).rents[3].ToString();
                rentsvalues.FontSize = 11;
                rentsvalues.TextAlignment = TextAlignment.Right;
                propertycard.Children.Add(rentsvalues);
                Canvas.SetTop(rentsvalues, 133);
                Canvas.SetRight(rentsvalues, 15);

                TextBlock mortgage = new TextBlock();
                mortgage.TextAlignment = TextAlignment.Center;
                mortgage.Text = "Mortgage Value £" + ((Station)prop).morgage.ToString();
                mortgage.Width = 175;
                propertycard.Children.Add(mortgage);
                Canvas.SetTop(mortgage, 200);
            }

            else
            {
                // company code
                Canvas.SetTop(name, 66);

                Image train_icon = new Image();
                train_icon.Width = 59;
                train_icon.Height = 73;
                if (prop.name == "Electric Company") train_icon.Source = new BitmapImage(new Uri(path + "\\Assets\\" + "Electric.png"));

                else train_icon.Source = new BitmapImage(new Uri(path + "\\Assets\\" + "Water.jpg"));
                propertycard.Children.Add(train_icon);
                Canvas.SetLeft(train_icon, 62);

                TextBlock rents = new TextBlock();
                rents.TextWrapping = TextWrapping.Wrap;
                rents.TextAlignment = TextAlignment.Center;
                rents.Width = 175;
                rents.Text = "If one \"Utility\" is owned,\nrent is 4 times amount\nshown on dice.\n\nIf both \"Utilites\" are owned,\nrent is 10 times amount\nshown on dice.";
                rents.FontSize = 8;
                propertycard.Children.Add(rents);
                Canvas.SetTop(rents, 110);

                TextBlock mortgage = new TextBlock();
                mortgage.TextAlignment = TextAlignment.Center;
                mortgage.Text = "Mortgage Value £" + ((Company)prop).morgage.ToString();
                mortgage.Width = 175;
                propertycard.Children.Add(mortgage);
                Canvas.SetTop(mortgage, 203);

            }
            return propertycard;
        }

        public void BuyScreen(Player player, BuyableSquare prop)
        {
            Rectangle test = new Rectangle();
            test.Opacity = 0.75;
            test.Fill = Brushes.Black;
            test.Height = rootCanvas.Height;
            test.Width = rootCanvas.Width;
            rootCanvas.Children.Add(test);

            Image Rich_Uncle_Pennybags = new Image();
            Rich_Uncle_Pennybags.Height = 247;
            Rich_Uncle_Pennybags.Width = 121;
            Rich_Uncle_Pennybags.Source = new BitmapImage(new Uri(path + "\\Assets\\" + "Pay me.png"));
            rootCanvas.Children.Add(Rich_Uncle_Pennybags);
            Canvas.SetTop(Rich_Uncle_Pennybags, 26);
            Canvas.SetLeft(Rich_Uncle_Pennybags, 243);

            Canvas propertycard = CreateCard(prop);
            rootCanvas.Children.Add(propertycard);
            Canvas.SetTop(propertycard, 22);
            Canvas.SetLeft(propertycard, 408);

            TextBlock Available = new TextBlock();
            Available.TextAlignment = TextAlignment.Center;
            Available.Text = "This property is AVAILABLE.\r\nDo you want to buy it for £" + prop.price.ToString() + "?";
            Available.Foreground = Brushes.White;
            Available.FontSize = 27;
            rootCanvas.Children.Add(Available);
            Canvas.SetTop(Available, 281);
            Canvas.SetLeft(Available, 227);

            Button Buy = new Button();
            Buy.Width = 146;
            Buy.Height = 48;
            Buy.Content = "BUY";
            Buy.Click += (sender, e) =>
            {
                if (player.Bal - prop.price < 0)
                {
                    MessageBox.Show("You do not have sufficent funds to buy this property, the auction will now proceed", "MONEY ERROR, BROKIE ALERT");
                    AuctionScreen(propertycard, prop);
                    return;
                }
                else
                {
                    prop.Buy(player, prop.price);
                    ((TextBlock)((ScrollViewer)(rootCanvas).Children[PlayerCount + 11]).Content).Text += player.Name + " bought " + prop.name + "\n";
                    board.history += player.Name + " bought " + prop.name + "\n"; ;
                    ((TextBlock)(rootCanvas).Children[PlayerCount + 2]).Text = "Balance: " + player.Bal.ToString();
                    rootCanvas.Children.RemoveRange(PlayerCount + 16, rootCanvas.Children.Count - (PlayerCount + 16));

                }

            };
            rootCanvas.Children.Add(Buy);
            Buy.FontSize = 27;
            Canvas.SetTop(Buy, 377);
            Canvas.SetLeft(Buy, 212);

            Button Auction = new Button();
            Auction.Width = 146;
            Auction.Height = 48;
            Auction.Content = "AUCTION";
            Auction.Click += (sender, e) => {
                AuctionScreen(propertycard, prop);
            };
            Auction.FontSize = 27;
            rootCanvas.Children.Add(Auction);
            Canvas.SetTop(Auction, 377);
            Canvas.SetLeft(Auction, 439);
        }

        public void PropertyScreen(object sender, RoutedEventArgs e) {
            rootCanvas.Children.Clear();
            List<BuyableSquare> Properties= new List<BuyableSquare>();
            TextBlock Bal;
            TextBlock NoHouses;
            TextBlock NoHotels;

            Canvas Prop = CreateCard(null);
            rootCanvas.Children.Add(Prop);
            Canvas.SetLeft(Prop, 375);
            Canvas.SetTop(Prop, 110);
            Canvas Buttons;
            int counter = 0;
            foreach (Brush i in colours.Values) {
                
                Rectangle temp = new Rectangle();
                temp.Stroke = i;
                temp.Width = 78;
                temp.Height = 23;
                temp.MouseEnter += (sender1, e1) => {
                    Cursor= Cursors.Hand;
                };
                temp.MouseLeave += (sender1, e1) => {
                    Cursor = Cursors.Arrow;
                };
                temp.MouseDown += (sender1, e1) => {
                    char key = SmartReverseLookup(colours, i);
                    Properties = board.GetProperties(key, board.GetCurrentPlayer());
                    if (Properties.Count >= 1)
                    {
                        Prop = CreateCard(Properties[0]);
                    }
                };
                rootCanvas.Children.Add(temp);
                Canvas.SetTop(temp, 203 + (11+23)*(counter/3));
                Canvas.SetLeft(temp,25+ (78+11)*(counter%3));
                counter++;
            }

            Button Station;
            Button Company;

            
            

            Canvas NextBack;
            Button Next;
            Button Back;

            string[] button_names = {"Mortgage", "Unmortgage", "Build", "Sell"};
            Canvas ManagementButtons;
            Button Morg;
            Button PayMorg;
            Button Build;
            Button Sell;

            Button Exit = new Button();
            Exit.Content = "X";
            Exit.FontSize = 30;
            Exit.Background = null;
            Exit.BorderThickness = new Thickness(0);
            Exit.Click += GameScreen;
            rootCanvas.Children.Add(Exit);
            Canvas.SetRight(Exit, 5);
            Canvas.SetTop(Exit, 5);

        }

        public void TradeScreen(object sender, RoutedEventArgs e) {
        }


        // To Do list: Chance screen, property screen/functionality, trade screen/functionality  
        // After working game: add save game functionality, link to database (maybe), add online multiplayer

    }
}
