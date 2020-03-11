using System;
using System.Net.Http;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
namespace RemoteDownload
{

    class Program
    {
        static readonly HttpClient client = new HttpClient();
        private static void PrintResult(string s)
        {
            ConsoleWriteLineWithColor(s, ConsoleColor.Cyan);
            Console.WriteLine();
        }
        private static void PrintURL(string s)
        {
            ConsoleWriteLineWithColor(s, ConsoleColor.Yellow);
        }
        private static void PrintResponse(string s)
        {
            ConsoleWriteLineWithColor(s, ConsoleColor.Blue);
        }
        private static void ConsoleWriteLineWithColor(object value, ConsoleColor color)
        {
            var lastColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(value);
            Console.ForegroundColor = lastColor;
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1307:指定 StringComparison", Justification = "<挂起>")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:请不要将文本作为本地化参数传递", Justification = "<挂起>")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1305:指定 IFormatProvider", Justification = "<挂起>")]
        static void PostUri(string uri)
        {
            using var payload = new StringContent(uri);

            var postStr = $"https://{Setting.MainSetting.host}/api/record";
            if (!string.IsNullOrEmpty(Setting.MainSetting.key))
            {
                postStr += $"?key={Setting.MainSetting.key}";
            }
            var postURI = new Uri(postStr);
            PrintURL(postURI.ToString());
            var response = client.PostAsync(postURI, payload).Result.Content.ReadAsStringAsync().Result;
            //{"id":262,"requestUrl":"https://github.com/Kikyou1997/LaniakeaProxy/archive/master.zip","fileUrl":"","taskID":"9976c8fd823d4e7ab8ff33890ffa6367"}
            
            if (response.Contains("Wrong"))
            {
                ConsoleWriteLineWithColor(response,ConsoleColor.Red);
                Console.WriteLine();
                return;
            }
            PrintResponse(response);
            var id = ((JObject)JsonConvert.DeserializeObject(response))["id"];
            PrintResult(id.ToString());
            string fileUrl;
            while (true)
            {
                var getURL = new Uri($"https://{Setting.MainSetting.host}/api/record/{id}");
                PrintURL(getURL.ToString());
                var res = client.GetAsync(getURL).Result.Content.ReadAsStringAsync().Result;

                //{"id":267,"requestUrl":"https://github.com/Kikyou1997/LaniakeaProxy/archive/master.zip","fileUrl":"","taskID":"4f0ebb6346434998af2964e4a01407c6"}
                PrintResponse(res);
                var jo = ((JObject)JsonConvert.DeserializeObject(res))["fileUrl"].ToString();
                if (string.IsNullOrEmpty(jo))
                {
                    PrintResult("fileUrl is null");
                    Thread.Sleep(1000);
                }
                else
                {
                    fileUrl = jo.ToString();
                    break;
                }
            }
            PrintResult($"fileurl is {fileUrl}");


            var filename = fileUrl.Split("/")[^1];//last

            if (string.IsNullOrEmpty(filename))
            {
                filename = "file";
            }

            var desktopDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

            var filepath = Path.Combine(desktopDirectory, filename);


            while (File.Exists(filepath))
            {
                Console.WriteLine("file is already exists");
                ConsoleWriteLineWithColor(filepath, ConsoleColor.Red);
                Console.WriteLine("please input new file name");
                Console.WriteLine("the file will be created in the desktop");
                filename = Console.ReadLine();
                filepath = Path.Combine(desktopDirectory, filename);
            }

            Console.WriteLine("Downloading to");
            PrintResult(filepath);

            var bs = client.GetAsync(new Uri(fileUrl)).Result.Content.ReadAsByteArrayAsync().Result;

            File.WriteAllBytes(filepath, bs);

            if (Setting.MainSetting.AutoClearConsole)
            {
                Console.Clear();
            }

            Console.WriteLine("Download success");
            PrintResult(uri);
            Console.WriteLine("Download to");
            PrintResult(filepath);
            Console.WriteLine("high speed url");
            PrintResult(fileUrl);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1307:指定 StringComparison", Justification = "<挂起>")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:请不要将文本作为本地化参数传递", Justification = "<挂起>")]
        static void Main()
        {
            Setting.Load(ref Setting.MainSetting);
            while (true)
            {
                Console.WriteLine("Just Input URI :)");
                var str = Console.ReadLine();
                var pa = str.Split(" ");
                if (pa.Length > 1)
                {
                    switch (pa[0])
                    {
                        case "key":
                            Setting.MainSetting.key = pa[1];
                            Console.WriteLine($"Setting key {pa[1]} success");
                            Setting.Save(Setting.MainSetting);
                            break;
                    }

                }
                else if (pa.Length == 1)
                {
                    if (!str.Contains("http"))
                    {
                        Console.WriteLine("URI should contains \"http\"");
                    }
                    else
                    {
                        PostUri(str);
                    }
                }
            }
        }
    }
    class Setting
    {
        public static Setting MainSetting = new Setting();
        public string key = "";
        public string host = "githubproxy.backend.117503445.top";
        public bool AutoClearConsole = true;
        public static void Load(ref Setting setting)
        {
            string json;
            try
            {
                json = File.ReadAllText("setting.json");
            }
            catch (FileNotFoundException)
            {
                return;
            }

            setting = JsonConvert.DeserializeObject<Setting>(json);
        }
        public static void Save(Setting setting)
        {
            var json = JsonConvert.SerializeObject(setting);
            File.WriteAllText("setting.json", json);
        }
    }
}
