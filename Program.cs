using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Threading;
using static System.Net.Mime.MediaTypeNames;
using static System.Net.WebRequestMethods;

namespace minecraft_restarter
{
    internal static class Program
    {

//todo: text reader, greeting when someone joins, random jokes


        private static string lastLine = "";
        public static List<string> jokesList = new List<string>();
        private static DateTime timeOfLastRestart = DateTime.UtcNow;
        private static DateTime timeOfLastJoke = DateTime.UtcNow;
        public static void Main()
        {

            Process ServerProc = new Process();
            ServerStart(ServerProc);

            Type type = typeof(Jokes);
            PopulateJokeList(jokesList, type);

            while (true)
            {

                bool serverUp = IsServer_up();

                //if (DetectOutOfMemoryEvent())
                //{
                //    ServerRestartSequence(ServerProc);
                //}

                if (CheckMemoryUse(ServerProc) / 1024 > 7000)
                {
                    ServerRestartSequence(ServerProc);
                }

                if (ServerUptime().TotalHours > 24 && IsServerEmpty(ServerProc))
                {
                    ServerRestartSequence(ServerProc);
                }

                if (!IsServerEmpty(ServerProc) && (TimeSinceLastJoke().TotalMinutes > 120))
                {
                    returnJoke(ServerProc);
                }

                if (serverUp)
                {
                    Console.WriteLine("server up since " + timeOfLastRestart + "UTC");
                    Console.WriteLine("Memory used " + (CheckMemoryUse(ServerProc) / 1024) + "MB");
                    Console.WriteLine("Uptime is " + (ServerUptime()));
                    AskForCommands(ServerProc);
                }
                else
                {
                    Console.WriteLine("server down, restarting server");
                    Process.Start(@"minecraft_restarter.exe");
                    Environment.Exit(0);
                }

            }
        }

        private static void PopulateJokeList(List<string> jokesList, Type type)
        {
            foreach (var field in type.GetFields())
            {
                var val = field.GetValue(null);
                jokesList.Add(val.ToString());
            }
        }

        private static void ServerRestartSequence(Process ServerProc)
        {
            ServerStop(ServerProc);
            Process.Start(@"minecraft_restarter.exe");
            Environment.Exit(0);
        }

        private static TimeSpan ServerUptime()
        {
            TimeSpan uptime = DateTime.UtcNow.Subtract(timeOfLastRestart);
            return uptime;
        }

        private static TimeSpan TimeSinceLastJoke()
        {
            TimeSpan uptime = DateTime.UtcNow.Subtract(timeOfLastJoke);
            return uptime;
        }

        private static void AskForCommands(Process ServerProc)
        {
            try
            {
                Console.WriteLine("write commands (like stop or list) in the next 10 seconds");
                string command = Reader.ReadLine(15000);
                if (command == "stop")
                {
                    ServerStop(ServerProc);
                    Environment.Exit(0);
                }
                if (command == "restart")
                {
                    ServerStop(ServerProc);
                    Process.Start(@"minecraft_restarter.exe");
                    Environment.Exit(0);
                }
                if (command == "joke")
                {
                    returnJoke(ServerProc);
                }
                else
                {
                    ServerCommandInputWriter(ServerProc, command);
                }
            }
            catch (TimeoutException)
            {
            }
        }

        private static void ServerStart(Process ServerProc)
        {

            string ServerFile;
            string ServerPath;

            ServerFile = "server.jar";
            ServerPath = @"C:\MC\The Paper World\";

            var startInfo = new ProcessStartInfo("java", "-Xmx6G -Xms6G -jar " + ServerFile + " nogui");
            startInfo.WorkingDirectory = ServerPath;
            startInfo.RedirectStandardError = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardInput = true;
            startInfo.UseShellExecute = false; // Necessary for Standard Stream Redirection
            startInfo.CreateNoWindow = true; // You can do either this or open it with "javaw" instead of "java"

            ServerProc.EnableRaisingEvents = true;
            ServerProc.StartInfo = startInfo;
            ServerProc.OutputDataReceived += new DataReceivedEventHandler(ServerOutputDataReceived);
            ServerProc.ErrorDataReceived += new DataReceivedEventHandler(ServerErrorDataReceived);
            ServerProc.Start();

            ServerProc.BeginOutputReadLine();

            timeOfLastRestart = DateTime.UtcNow;
            Thread.Sleep(15000);

        }
        static void ServerErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine("Error: {0}", e.Data);
        }

        static void ServerOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine("{0}", e.Data);
            lastLine = e.Data;
        }

        private static void ServerStop(Process ServerProc)
        {
            StreamWriter myStreamWriter = ServerProc.StandardInput;
            String inputText;

#if RELEASE
            Countdown(myStreamWriter);
#endif
            inputText = "stop";
            myStreamWriter.WriteLine(inputText);
            myStreamWriter.Close();
            Thread.Sleep(10000); //Tam said it must be ten
            bool serverUp = IsServer_up();
            if (serverUp)
            {
                ServerProc.Kill();
            }
        }

        private static Boolean IsServerEmpty(Process ServerProc)
        {
            ServerCommandInputWriter(ServerProc, "list");
            string checkFor = "There are 0";
            bool b = lastLine.Contains(checkFor);
            return b;
        }

        private static void ServerCommandInputWriter(Process ServerProc, string command)
        {
            StreamWriter myStreamWriter = ServerProc.StandardInput;
            myStreamWriter.WriteLine(command);
        }

        private static void Countdown(StreamWriter myStreamWriter)
        {
            string inputText = "say server restart in 1 minute";
            myStreamWriter.WriteLine(inputText);
            Thread.Sleep(15000);
            inputText = "say 45 seconds left, lazy potato, get off before it restarts!";
            myStreamWriter.WriteLine(inputText);
            Thread.Sleep(15000);
            inputText = "say 30 seconds left, don't be a fool, flee while you still can";
            myStreamWriter.WriteLine(inputText);
            Thread.Sleep(15000);
            inputText = "say 15 seconds left, THIS IS NOT A DRILL";
            myStreamWriter.WriteLine(inputText);
            Thread.Sleep(5000);
            for (int a = 10; a >= 0; a--)
            {
                myStreamWriter.WriteLine("say server restart in {0}", a);
                Thread.Sleep(1000);
            }
        }

        public static long CheckMemoryUse(Process ServerProc)
        {
            Process proc = ServerProc;
            PerformanceCounter PC = new PerformanceCounter();
            PC.CategoryName = "Process";
            PC.CounterName = "Working Set - Private";
            PC.InstanceName = proc.ProcessName;
            long memsize = Convert.ToInt64(PC.NextValue()) / 1024;
            PC.Close();
            PC.Dispose();
            return memsize;
        }

        private static bool IsServer_up()
        {

            var query = "SELECT * "
                    + "FROM Win32_Process "
                    + "WHERE Name = 'java.exe' "
                    + "OR "
                    + "Name = 'javaw.exe'";

            // get associated processes
            List<Process> servers = null;
            using (var results = new ManagementObjectSearcher(query).Get())
                servers = results.Cast<ManagementObject>()
                                 .Select(mo => Process.GetProcessById((int)(uint)mo["ProcessId"]))
                                 .ToList();
            Console.WriteLine("number of java processes running: "+ servers.Count);
            if (servers.Count < 1)
            {
                return false;
            }

            return true;
        }

        public static bool DetectOutOfMemoryEvent()
        {

            String myEventType = null;
            // Associate the instance of 'EventLog' with local System Log.
            EventLog myEventLog = new EventLog("System", ".");
            int myOption = Convert.ToInt32(3);
            switch (myOption)
            {
                case 1:
                    myEventType = "Error";
                    break;
                case 2:
                    myEventType = "Information";
                    break;
                case 3:
                    myEventType = "Warning";
                    break;
                default: break;
            }

            EventLogEntryCollection myLogEntryCollection = myEventLog.Entries;
            int myCount = myLogEntryCollection.Count;
            // Iterate through all 'EventLogEntry' instances in 'EventLog'.
            for (int i = myCount - 1; i > -1; i--)
            {
                EventLogEntry myLogEntry = myLogEntryCollection[i];
                // Select the entry having desired EventType.
                if (myLogEntry.EntryType.ToString().Equals(myEventType))
                {
                    // Display Source of the event.
                    Console.WriteLine(myLogEntry.Source
                       + " was the source of last event of type "
                       + myLogEntry.EntryType);
                    if (myLogEntry.Source == "Resource-Exhaustion-Detector" && (timeOfLastRestart.AddHours(1) > DateTime.Now))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        class Reader
        {
            private static Thread inputThread;
            private static AutoResetEvent getInput, gotInput;
            private static string input;

            static Reader()
            {
                getInput = new AutoResetEvent(false);
                gotInput = new AutoResetEvent(false);
                inputThread = new Thread(reader);
                inputThread.IsBackground = true;
                inputThread.Start();
            }

            private static void reader()
            {
                while (true)
                {
                    getInput.WaitOne();
                    input = Console.ReadLine();
                    gotInput.Set();
                }
            }

            // omit the parameter to read a line without a timeout
            public static string ReadLine(int timeOutMillisecs = Timeout.Infinite)
            {
                getInput.Set();
                bool success = gotInput.WaitOne(timeOutMillisecs);
                if (success)
                    return input;
                else
                    throw new TimeoutException("User did not provide input within the timelimit.");
            }
        }

        public static void returnJoke(Process ServerProc)
        {

            Random randNum = new Random();
            int aRandomPos = randNum.Next(jokesList.Count);//Returns a nonnegative random number less than the specified maximum (firstNames.Count).

            string currName = "say " + jokesList[aRandomPos];
            ServerCommandInputWriter(ServerProc, currName);
            timeOfLastJoke = DateTime.UtcNow;
    }

        public static class Jokes
        {
            public const string string1 = "Why did Adele cross the road? To say hello from the other side.";
            public const string string2 = "What kind of concert only costs 45 cents? A 50 Cent concert featuring Nickelback.";
            public const string string3 = "What did the grape say when it got crushed? Nothing, it just let out a little wine.";
            public const string string4 = "I want to be cremated as it is my last hope for a smoking hot body.";
            public const string string5 = "Time flies like an arrow. Fruit flies like a banana.";
            public const string string6 = "To the guy who invented zero, thanks for nothing.";
            public const string string7 = "I had a crazy dream last night! I was swimming in an ocean of orange soda. Turns out it was just a Fanta sea.";
            public const string string8 = "A crazy wife says to her husband that moose are falling from the sky. The husband says, it’s reindeer.";
            public const string string9 = "Ladies, if he can’t appreciate your fruit jokes, you need to let that mango.";
            public const string string10 = "Geology rocks but Geography is where it’s at!";
            public const string string11 = "What was Forrest Gump’s email password? 1forrest1";
            public const string string12 = "Did you hear about the restaurant on the moon? I heard the food was good but it had no atmosphere.";
            public const string string13 = "Can February March? No, but April May.";
            public const string string14 = "Need an ark to save two of every animal? I noah guy.";
            public const string string15 = "I don’t trust stairs because they’re always up to something.";
            public const string string16 = "Smaller babies may be delivered by stork but the heavier ones need a crane.";
            public const string string17 = "My grandpa has the heart of the lion and a lifetime ban from the zoo.";
            public const string string18 = "Why was Dumbo sad? He felt irrelephant.";
            public const string string19 = "A man sued an airline company after it lost his luggage. Sadly, he lost his case.";
            public const string string20 = "I lost my mood ring and I don't know how to feel about it!";
            public const string string21 = "Yesterday, I accidentally swallowed some food coloring. The doctor says I’m okay, but I feel like I’ve dyed a little inside.";
            public const string string22 = "So what if I don’t know what apocalypse means? It’s not the end of the world!";
            public const string string23 = "My friend drove his expensive car into a tree and found out how his Mercedes bends.";
            public const string string24 = "Becoming a vegetarian is one big missed steak.";
            public const string string25 = "I was wondering why the ball was getting bigger. Then it hit me.";
            public const string string26 = "Some aquatic mammals at the zoo escaped. It was otter chaos!";
            public const string string27 = "Never trust an atom, they make up everything!";
            public const string string28 = "Waking up this morning was an eye-opening experience.";
            public const string string29 = "Long fairy tales have a tendency to dragon.";
            public const string string30 = "What do you use to cut a Roman Emperor's hair? Ceasers.";
            public const string string31 = "The Middle Ages were called the Dark Ages because there were too many knights.";
            public const string string32 = "My sister bet that I couldn’t build a car out of spaghetti. You should’ve seen her face when I drove pasta.";
            public const string string33 = "I made a pun about the wind but it blows.";
            public const string string34 = "Never discuss infinity with a mathematician, they can go on about it forever.";
            public const string string35 = "I knew a guy who collected candy canes, they were all in mint condition.";
            public const string string36 = "My wife tried to apply at the post office but they wouldn’t letter. They said only mails work here.";
            public const string string37 = "My friend’s bakery burned down last night. Now his business is toast.";
            public const string string38 = "Getting the ability to fly would be so uplifting.";
            public const string string39 = "It's hard to explain puns to kleptomaniacs because they always take things literally.";
            public const string string40 = "Two windmills are standing in a wind farm. One asks, “What’s your favorite kind of music?” The other says, “I’m a big metal fan.”";
            public const string string41 = "I can’t believe I got fired from the calendar factory. All I did was take a day off!";
            public const string string42 = "England doesn't have a kidney bank, but it does have a Liverpool.";
            public const string string43 = "What do you call the wife of a hippie? A Mississippi.";
            public const string string44 = "A cross-eyed teacher couldn’t control his pupils.";
            public const string string45 = "She had a photographic memory, but never developed it.";
            public const string string46 = "I wasn’t originally going to get a brain transplant, but then I changed my mind.";
            public const string string47 = "There was a kidnapping at school yesterday. Don’t worry, though - he woke up!";
            public const string string48 = "What do you get when you mix alcohol and literature? Tequila mockingbird.";
            public const string string49 = "What washes up on tiny beaches? Microwaves.";
            public const string string50 = "I hate how funerals are always at 9 a.m. I’m not really a mourning person.";
            public const string string51 = "What’s the difference between a poorly dressed man on a bicycle and a nicely dressed man on a tricycle? A tire.";
            public const string string52 = "The guy who invented the door knocker got a no-bell prize.";
            public const string string53 = "German sausage jokes are just the wurst.";
            public const string string54 = "What do you call an alligator in a vest? An investigator.";
            public const string string55 = "What do you call the ghost of a chicken? A poultry-geist.";
            public const string string56 = "How does Moses make coffee? Hebrews it.";
            public const string string57 = "The machine at the coin factory just suddenly stopped working, with no explanation. It doesn’t make any cents.";
            public const string string58 = "Sure, I drink brake fluid. But I can stop anytime!";
            public const string string59 = "What do you call a man with no arms and no legs stuffed in your mailbox? Bill.";
            public const string string60 = "Somebody stole all my lamps. I couldn’t be more de-lighted!";
            public const string string61 = "I bought a boat because it was for sail.";
            public const string string62 = "I'm reading a book about anti-gravity. It's impossible to put down!";
            public const string string63 = "How did the picture end up in jail? It was framed!";
            public const string string64 = "My ex-wife still misses me. But her aim is starting to improve!";
            public const string string65 = "Coffee has a rough time in our house. It gets mugged every single morning!";
            public const string string66 = "Why was the cookie sad? Because his mom was a wafer long!";
            public const string string67 = "What's the difference between a hippo and a zippo? One is really heavy and the other is a little lighter!";
            public const string string68 = "What did the sushi say to the bee? Wasabee!";
            public const string string69 = "Why was the baby ant confused? Because all his uncles were ants!";
            public const string string70 = "I just found out that I'm color blind. The news came completely out of the green!";
            public const string string71 = "Why didn't the cat go to the vet? He was feline fine!";
            public const string string72 = "Who is the penguin's favorite Aunt? Aunt-Arctica!";
            public const string string73 = "What should a lawyer always wear to a court? A good lawsuit!";
            public const string string74 = "The quickest way to make antifreeze? Just steal her blanket!";
            public const string string75 = "How do you make a good egg-roll? You push it down a hill!";
            public const string string76 = "Apple is designing a new automatic car. But they're having trouble installing Windows!";
            public const string string77 = "I've started sleeping in our fireplace. Now I sleep like a log!";
            public const string string78 = "That baseball player was such a bad sport. He stole third base and then just went home!";
            public const string string79 = "Did you hear about the guy who got hit in the head with a can of soda? He was lucky it was a soft drink!";
            public const string string80 = "The past, the present, and the future walk into a bar. It was tense!";
            public const string string81 = "You really shouldn't be intimidated by advanced math… it's easy as pi!";
            public const string string82 = "What did the hamburger name its baby? Patty!";
            public const string string83 = "One lung said to another, “we be-lung together!”";
            public const string string84 = "I asked a Frenchman if he played video games. He said Wii.";
            public const string string85 = "Why are frogs so happy? They eat whatever bugs them.";
            public const string string86 = "What did the duck say when she purchased new lipstick? Put it on my bill!";
            public const string string87 = "What does a clock do when it's hungry? It goes back for seconds.";
            public const string string88 = "My parents said I can't drink coffee anymore. Or else they'll ground me!";
            public const string string89 = "What did syrup to the waffle? I love you a waffle lot!";
            public const string string90 = "My wife refuses to go to a nude beach with me. I think she's just being clothes-minded!";
            public const string string91 = "Did you hear about that cheese factory that exploded in France? There was nothing left but de Brie!";
            public const string string92 = "I'm no cheetah, you're lion!";
            public const string string93 = "What did the ranch say when somebody opened the refrigerator? \"Hey, close the door! I'm dressing!\"";
            public const string string94 = "I wanted to take pictures of the fog this morning but I mist my chance. I guess I could dew it tomorrow!";
            public const string string95 = "My dad unfortunately passed away when we couldn't remember his blood type. His last words to us were, \"Be positive!\"";
            public const string string96 = "What do you call a girl with one leg that's shorter than the other? Ilene.";
            public const string string97 = "Towels can’t tell jokes. They have a dry sense of humor.";
            public const string string98 = "What did the buffalo say to his son? Bison.";
            public const string string99 = "Why should you never trust a train? They have loco motives.";
            public const string string100 = "A cabbage and celery walk into a bar and the cabbage gets served first because he was a head.";
            public const string string101 = "What’s America's favorite soda? Mini soda.";
        }
    }
}
