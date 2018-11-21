using System;
using System.ServiceModel;
using EnspireWebClient.AssayService;
using EnspireWebClient.DataService;
using System.Threading;
using System.Reflection;

namespace EnspireWebClient
{

    class Program
    {
        static void Main(string[] args)
        {
            Client client = new Client();
            string command;
            if (args.Length == 0) command = "help";
            else command = args[0];
            ValidAssayProtocol[] protocols = client.dataproxy.GetValidProtocols(0);

            try
            {
                if (command == "help")
                {
                    Console.Write(@"
available commands:  
help - get this help message
list - list available protocols
run <name> - run protocol <name>, wait until it finishes (currently OnExportDone callback)
runasync <name> - run protocol <name>, return immediately
listen - listen for callbacks until ctrl-C
probe - listen and call get_CurrentState() every second
");
                }
                else if (command == "list")
                {
                    ListProtocols(protocols);
                }
                else if (command == "runasync")
                {
                    client.assayproxy.StartAssay(FindPid(protocols, args));
                }
                else if (command == "run")
                {
                    client.assayproxy.StartAssay(FindPid(protocols, args));
                    client.doneEvent.WaitOne();
                }
                else if (command == "probe")
                {
                    while (Console.KeyAvailable == false)
                    {
                        Console.WriteLine("state is {0}.", client.assayproxy.get_CurrentState().ToString());
                        Thread.Sleep(1000);
                        //ConsoleKeyInfo info = Console.ReadKey();
                        //Console.WriteLine(info.Key.ToString());
                        //if (info.Key == ConsoleKey.Escape) break;
                    }
                }
                else if (command == "listen")
                {
                    Console.CancelKeyPress += (sender, eArgs) =>
                    {
                        client.quitEvent.Set();
                        eArgs.Cancel = true;
                    };
                    client.quitEvent.WaitOne();
                }
            }
            catch (Exception ex)
            {
                if (ex.Message == "unknown protocol")
                {
                    Console.WriteLine("Please choose a protocol to start:\n");
                    ListProtocols(protocols);
                }
                else
                {
                    Console.WriteLine(ex.Message);
                }
            }

        }

        static void ListProtocols(ValidAssayProtocol[] protocols)
        {
            foreach (ValidAssayProtocol protocol in protocols)
            {
                Console.WriteLine(protocol.ProtocolName);
            }
        }

        static int FindPid(ValidAssayProtocol[] protocols, string[] args)
        {
            int pid = -1;
            if (args.Length > 1)
            {
                foreach (ValidAssayProtocol protocol in protocols)
                {
                    if (protocol.ProtocolName == args[1])
                    {
                        pid = protocol.AssayProtocolID;
                        break;
                    }
                }
            }
            if (pid == -1) throw new Exception("unknown protocol");
            return pid;
        }
    }

    public partial class Client : EnSpireAssayServiceCallback
    {
        public ManualResetEvent quitEvent = new ManualResetEvent(false);
        public ManualResetEvent doneEvent = new ManualResetEvent(false);
        public EnSpireAssayServiceClient assayproxy = null;
        public EnSpireDataServiceClient dataproxy = null;

        public Client()
        {
            assayproxy = new EnSpireAssayServiceClient(new InstanceContext(this), "EnSpireAssayTcpEndpoint");
            assayproxy.SubscribeEvents();
            dataproxy = new EnSpireDataServiceClient("EnSpireDataTcpEndpoint");
        }

        public void OnApertureRead(string apertureid)
        {
            Console.WriteLine("OnApertureRead");
        }

        public void OnAssayContinued(int assayId)
        {
            Console.WriteLine("OnAssayContinued");
        }

        public void OnAssayDone(int assayid)
        {
            Console.WriteLine("OnAssayDone");
        }

        public void OnAssayError(string Source, int Number, string Description, int Choices, ErrorAction Action)
        {
            Console.WriteLine("OnAssayError");
        }

        public void OnAssayExportDone(int assayid)
        {
            Console.WriteLine("OnAssayExportDone");
        }

        public void OnAssayPaused(int assayid)
        {
            Console.WriteLine("OnAssayPaused");
        }

        public void OnAssaySaved(int assayid)
        {
            Console.WriteLine("OnAssaySaved");
        }

        public void OnAssayStarted(int assayid)
        {
            Console.WriteLine("OnAssayStarted");
        }

        public void OnCurrentStateChanged(CurrentState newstate)
        {
            //Console.WriteLine("OnCurrentStateChanged");
        }

        public void OnError(string source, int number, string description, int choices, ErrorAction erroraction)
        {
            Console.WriteLine("OnError");
        }

        public void OnExcFilterBarcodesRead()
        {
            Console.WriteLine("OnExcFilterBarcodesRead");
        }

        public void OnExportDone(int assayid, int plateid, string exportfilenames)
        {
            Console.WriteLine("OnExportDone: {0}", exportfilenames);
            doneEvent.Set();
        }

        public void OnInitDone(bool isinstrumentconnected)
        {
            Console.WriteLine("OnInitDone");
        }

        public void OnInstrumentServerConnected(bool connected)
        {
            Console.WriteLine("OnInstrumentServerConnected");
        }

        public void OnInstrumentStateChanged(int newstate)
        {
            //Console.WriteLine("OnInstrumentStateChanged");
        }

        public void OnPlateBarcodeRead(string barcode, bool acknowledge)
        {
            Console.WriteLine("OnPlateBarcodeRead");
        }

        public void OnPlateLoaded(string barcode, int height, int maxheight)
        {
            Console.WriteLine("OnPlateLoaded");
        }

        public void OnPlateUnloaded()
        {
            Console.WriteLine("OnPlateUnloaded");
        }

        public void OnShakeBegin(int shaketime)
        {
            Console.WriteLine("OnShakeBegin");
        }

        public void OnShakeEnd()
        {
            Console.WriteLine("OnShakeEnd");
        }

        public void OnShutdown()
        {
            Console.WriteLine("OnShutdown");
        }

        public void OnTemperatureMeasured(int insidetemperature, int outsidetemperature, int humidity)
        {
            Console.WriteLine("OnTemperatureMeasured");
        }

        public void OnTemperatureUpdated(int inside, int outside, int humidity, int uppersensor, int lowersensor, int pwmplatewarmer, int pwmxywarmer, int pwmplatecooler, int insidesensor)
        {
            Console.WriteLine("Temperature is {0} C", (inside/100) - 273.15);
        }

        public void OnTimerElapsed(int ticks)
        {
            Console.WriteLine("OnTimerElapsed");
        }

        public void OnWaitBegin(int waittime)
        {
            Console.WriteLine("OnWaitBegin");
        }

        public void OnWaitEnd()
        {
            Console.WriteLine("OnWaitEnd");
        }
    }
}


