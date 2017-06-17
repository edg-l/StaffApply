using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using Newtonsoft.Json;
using System.Net.Mail;

namespace StaffApply
{
    [ApiVersion(2, 1)]
    public class StaffApply : TerrariaPlugin
    {
        #region Info
        public override string Name { get { return "StaffApply"; } }
        public override string Author { get { return "Ryozuki"; } }
        public override string Description { get { return "A plugin to let users apply to a staff rank directly on terraria"; } }
        public override Version Version { get { return new Version(1, 2, 1); } }
        #endregion

        public ConfigFile Config = new ConfigFile();

        public StaffApply(Main game) : base(game)
        {

        }

        #region Initialize
        public override void Initialize()
        {
            ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
        }
        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
            }
        }

        public string getRanks()
        {
            return String.Join(" ", Config.AppRanks);
        }
        
        void OnInitialize(EventArgs args)
        {
            LoadConfig();

            Directory.CreateDirectory("StaffApplications");

            Commands.ChatCommands.Add(new Command("staffapply.use", staffapply, "apply")
            {
                HelpText = "Usage: /apply (" + getRanks() + ") // Apply for one of these ranks"
            });
            Commands.ChatCommands.Add(new Command("staffapply.use", answer, "answer")
            {
                HelpText = "Usage: /answer <your answer> //Write down your application with this, can be used multiple times."
            });
            Commands.ChatCommands.Add(new Command("staffapply.use", sendApply, "sendapplication")
            {
                HelpText = "Usage: /sendapplication // Sends the staff application"
            });
        }

        private void OnReload()
        {
            LoadConfig();
        }

        void staffapply(CommandArgs e)
        {
            if (e.Parameters.Count == 0 || e.Parameters.Count > 1)
            {
                e.Player.SendErrorMessage("Invalid syntax! Proper syntax: {0}apply rank", Commands.Specifier);
                return;
            }

            string args = String.Join(" ", e.Parameters.ToArray());

            if (args == "help")
            {
                e.Player.SendInfoMessage("/apply (" + getRanks() + ") // Apply for one of these ranks");
                e.Player.SendInfoMessage("/answer <your answer> // Write down your application with this, can be used multiple times.");
                e.Player.SendInfoMessage("/sendapplication // Sends the staff application");
            }

            if (Config.AppRanks.Contains<string>(args) == false && args != "help")
            {
                e.Player.SendErrorMessage("Application for {0}".SFormat(args) + " doesn't exists, try /apply (" + getRanks() + ")");
                return;
            }

            foreach (string rank in Config.AppRanks)
            {
                if (args == rank)
                {
                    if (File.Exists("StaffApplications/{0}.txt".SFormat(e.Player.Name)))
                    {
                        e.Player.SendErrorMessage("You are already applying, {0}answer more or send your application with {0}sendapplication".SFormat(Commands.Specifier));
                    }
                    else if (File.Exists("StaffApplications/{0}-finished.txt".SFormat(e.Player.Name)))
                    {
                        e.Player.SendErrorMessage("You already applied");
                    }
                    else
                    {
                        File.Create(String.Format("StaffApplications/{0}.txt", e.Player.Name)).Close();

                        e.Player.SendInfoMessage("You are applying to {0} rank...".SFormat(rank));
                        e.Player.SendInfoMessage("Please answer this questions on your application: ");
                        foreach (string question in Config.Questions)
                        {
                            e.Player.SendSuccessMessage(question);
                        }
                        e.Player.SendInfoMessage("Use /answer to write down your application. You can use it multiple times. Minimum words to accept your application: {0}".SFormat(Config.minWords));
                        e.Player.SendInfoMessage("Use /sendapplication to send it");
                        using (StreamWriter fs = new StreamWriter("StaffApplications/{0}.txt".SFormat(e.Player.Name), true))
                        {
                            fs.WriteLine(rank.ToUpper() + " application of {0}".SFormat(e.Player.Name));
                            fs.WriteLine("---------------------------------------------");
                            fs.WriteLine("--------------[Questions]---------------");
                            foreach (string question in Config.Questions)
                            {
                                fs.WriteLine(question);
                            }
                            fs.WriteLine("---------------------------------------------");
                            fs.WriteLine("---------------[Answers]----------------");
                        }
                    }
                }
            }
        }

        void answer(CommandArgs e)
        {
            if (e.Parameters.Count == 0)
            {
                e.Player.SendErrorMessage("Invalid syntax! Proper syntax: {0}answer <your answer>", Commands.Specifier);
                return;
            }

            string args = String.Join(" ", e.Parameters.ToArray());


            if (File.Exists("StaffApplications/{0}.txt".SFormat(e.Player.Name)))
            {
                using (StreamWriter fs = new StreamWriter("StaffApplications/{0}.txt".SFormat(e.Player.Name), true))
                {
                    fs.WriteLine(args);
                }
                e.Player.SendSuccessMessage("Answer sent, you can send more answers");
            }
            else
            {
                e.Player.SendErrorMessage("You have to {0}apply before executing this command", Commands.Specifier);
            }
        }

        void sendApply(CommandArgs e)
        {
            string args = String.Join(" ", e.Parameters.ToArray());

            if (File.Exists("StaffApplications/{0}.txt".SFormat(e.Player.Name)))
            {
                FileInfo f = new FileInfo("StaffApplications/{0}.txt".SFormat(e.Player.Name));
                if(f.Length < Config.minWords)
                {
                    e.Player.SendErrorMessage("Your application is to short, answer more");
                }
                else
                {
                    File.Move("StaffApplications/{0}.txt".SFormat(e.Player.Name), "StaffApplications/{0}-finished.txt".SFormat(e.Player.Name));
                    if (Config.EnableMail)
                    {
                        sendAppMail(e);
                    }
                    else
                    {
                        e.Player.SendSuccessMessage("Staff application succesfully sent");
                    }
                }
            }
            else if(File.Exists("StaffApplications/{0}-finished.txt".SFormat(e.Player.Name)))
            {
                e.Player.SendErrorMessage("You already applied");
            }
            else
            {
                e.Player.SendErrorMessage("You have to {0}apply before executing this command", Commands.Specifier);
            }
        }

        void sendAppMail(CommandArgs e)
        {
            try
            {
                if (File.Exists("StaffApplications/{0}-finished.txt".SFormat(e.Player.Name)))
                {
                    string body = File.ReadAllText("StaffApplications/{0}-finished.txt".SFormat(e.Player.Name));

                    var smtp = new SmtpClient
                    {
                        Host = "smtp.gmail.com",
                        Port = 587,
                        EnableSsl = true,
                        DeliveryMethod = SmtpDeliveryMethod.Network,
                        Credentials = new System.Net.NetworkCredential(Config.AdminMail, Config.AdminMailPassword),
                        Timeout = 20000
                    };

                    var message = new MailMessage(Config.AdminMail, Config.AdminMail);
                    var ServerName = "Terraria Server";
                    if (!string.IsNullOrEmpty(TShock.Config.ServerName))
                    {
                        ServerName = TShock.Config.ServerName;
                    }
                    message.Subject = e.Player.Name + " Application for " + ServerName;
                    message.Body = body;
                    e.Player.SendInfoMessage("Sending, wait...");
                    smtp.Send(message);
                    e.Player.SendSuccessMessage("Staff application succesfully sent");
                }
            }
            catch(SmtpException err)
            {
                e.Player.SendErrorMessage("Mail not configured properly, check console. Be sure to enable lesssecureapps on your gmail configuration.");
                TShock.Log.ConsoleError(err.ToString());
            }
                
            
        }

        #region Load Config
        private void LoadConfig()
        {
            string path = Path.Combine(TShock.SavePath, "StaffApply.json");
            Config = ConfigFile.Read(path);
        }
        #endregion
    }
}
