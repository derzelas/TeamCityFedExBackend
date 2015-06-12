using System;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using System.Timers;
using HipchatApiV2;
using Timer = System.Timers.Timer;

namespace TeamCityHipChatService
{
    public partial class TeamCityHipChatService : ServiceBase
    {
        private HipchatClient _hipChatClient;
        private TeamCityEngine _teamCityEngine;
        private Timer _timer;
        private ProjectStatus _dev;
        private string _lastDevMessageId;

        public TeamCityHipChatService()
        {
            InitializeComponent();
            InitializeHipChatClient();
            InitializeTeamCityEngine();
            InitializeTimer();
            _lastDevMessageId = string.Empty;

        }

        void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            var dev = _teamCityEngine.GetProjectStatus("bt2");
            SendStatus(dev);

            var history = _hipChatClient.ViewRecentRoomHistory("Aidaws", _lastDevMessageId);
            var historyItem = history.Items.OrderByDescending(x => x.Date).FirstOrDefault(x => x.Message.StartsWith("@TeamCity status dev") && x.Id != _lastDevMessageId);
            if (historyItem != null)
            {
                _lastDevMessageId = historyItem.Id;
                dev = _teamCityEngine.GetProjectStatus("bt2");
                SendStatus(dev, true);
            }
        }

        private void SendStatus(ProjectStatus dev, bool force = false)
        {
            if (force || dev.Status != _dev.Status || dev.State != _dev.State)
            {
                _dev = dev;
                _hipChatClient.SendNotification("Aidaws",
                    string.Format("@Clients status dev {0},{1}", _dev.Status.ToString().ToLower(),
                        _dev.State.ToString().ToLower()));
            }
        }

        protected override void OnStart(string[] args)
        {
            _dev = _teamCityEngine.GetProjectStatus("bt2");
            _hipChatClient.SendNotification("Aidaws", string.Format("@Clients status dev {0},{1}", _dev.Status.ToString().ToLower(), _dev.State.ToString().ToLower()));
        }

        protected override void OnStop()
        {
            _hipChatClient.SendNotification("Aidaws", "TCHC service stopped.");
            _timer.Dispose();
        }

        private void InitializeTeamCityEngine()
        {
            _teamCityEngine = new TeamCityEngine("http://aidatest-pc:8090/");
        }

        private void InitializeHipChatClient()
        {
            _hipChatClient = new HipchatClient("LbwCda0h960ALdKiIu20XiQSmGYYVqhLEFhJVtUE");
            _hipChatClient.SendNotification("Aidaws", "TCHC service started.");
            var history = _hipChatClient.ViewRecentRoomHistory("Aidaws", _lastDevMessageId, maxResults:1000);
            var historyItem = history.Items.OrderByDescending(x => x.Date).First(x => x.Message == "TCHC service started.");
            _lastDevMessageId = historyItem.Id;
        }

        private void InitializeTimer()
        {
            _timer = new Timer();
            _timer.Elapsed += TimerElapsed;
            _timer.Interval = 5000;
            _timer.Enabled = true;
        }
    }
}


//LbwCda0h960ALdKiIu20XiQSmGYYVqhLEFhJVtUE