using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows.Input;

namespace JlzQualiTool
{
    using log4net;
    using Microsoft.Win32;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Json;
    using System.Windows;
    using System.Windows.Threading;
    using System.Xml.Serialization;

    [DataContract]
    public class ViewModel //: INotifyPropertyChanged
    {
        private static readonly string TimeMatchupSeparator = "---";
        private static ILog Log = log4net.LogManager.GetLogger(typeof(ViewModel));

        public ViewModel()
        {
            // TODO check about using CollectionViewSource instead for data grid binding
            // cf https://stackoverflow.com/questions/19112922/sort-observablecollectionstring-through-c-sharp
            this.Teams = new ObservableCollection<Team>();
            this.Rounds = new ObservableCollection<Round>();

            LoadData(Path.Combine(Settings.SavePath, "Bonstetten201x-14-round4.data"));

            //LoadSampleData();
            //SimulateResults();
        }

        public Configuration Configuration { get; set; }

        public ICommand LoadCommand => new CommandHandler(this.LoadData, true);

        public IEnumerable<Matchup> Matchups => Rounds.SelectMany(x => x.Matchups);

        public IEnumerable<Matchup> PlayedMatchups => Matchups.Where(m => m.IsPlayed);

        [DataMember]
        public ObservableCollection<Round> Rounds { get; }

        public ICommand SaveCommand => new CommandHandler(this.SaveData, true);

        public ICommand SaveScoreCommand => new ParameterCommandHandler(this.SaveScore, true);

        public ICommand SimulateResultsCommand => new CommandHandler(this.SimulateResults, true);

        [DataMember]
        public ObservableCollection<Team> Teams { get; set; }

        public ICommand UpdateScoresCommand => new CommandHandler(this.UpdateScores, true);

        public static void GlobalExceptionHandler(object sender, DispatcherUnhandledExceptionEventArgs args)
        {
            Log.Fatal("Unexpected exception occured.", args.Exception);
            MessageBox.Show(args.Exception.Message, Resources.UnexpectedException, MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        }

        public void InitializeMatchups()
        {
            // TODO ideally avoid passing view model directly.
            // TODO put ranking order into configuration
            Rounds.Add(new Round(Configuration.RoundInfos[0], this, m => RankingSnapshot.None));
            Rounds.Add(new Round(Configuration.RoundInfos[1], this, m => new RankingSnapshot(m, new List<int> { 1, 2, 3 })));
            Rounds.Add(new Round(Configuration.RoundInfos[2], this, m => new RankingSnapshot(m, new List<int> { 3, 5 })));
            Rounds.Add(new Round(Configuration.RoundInfos[3], this, m => new RankingSnapshot(m, new List<int>())));
            Rounds.Add(new Round(Configuration.RoundInfos[4], this, m => new RankingSnapshot(m, new List<int>())));
        }

        public void LoadData()
        {
            // TODO correct one?
            var dialog = new OpenFileDialog
            {
                InitialDirectory = Settings.SavePath
            };

            var showDialog = dialog.ShowDialog();

            if (showDialog.HasValue ? showDialog.Value : false)
            {
                LoadData(dialog.FileName);
            }
        }

        public void LoadData(string fileName)
        {
            this.Teams.Clear();

            Log.Info($"Loading data from file '{fileName}'.");

            var lines = File.ReadLines(fileName, Encoding.Default);
            var e = lines.GetEnumerator();
            while (e.MoveNext())
            {
                if (e.Current.Equals(TimeMatchupSeparator))
                {
                    // TODO check alternative: FromLine returns null as indicator.
                    break;
                }
                Team team = Team.FromLine(e.Current);
                this.Teams.Add(team);
            }

            // TODO right place to do that?
            Settings.TotalTeams = Teams.Count();

            Log.Info($" - {Teams.Count} teams loaded.");

            var orderedTeams = this.Teams.OrderByDescending(t => t.PreSeasonPonits).ToList();

            for (int i = 0; i < 4; i++)
            {
                orderedTeams[i].Seed = i + 1;
            }

            Configuration = LoadConfig(Teams.Count());

            this.Rounds.Clear();
            this.InitializeMatchups();

            int matchupCounter = 0;
            while (e.MoveNext())
            {
                // TODO improve exception handling
                var cells = e.Current.Split(',');
                if (cells.Count() < 2)
                {
                    // TODO alternative: exception.
                    break;
                }
                var matchId = int.Parse(cells[0].Trim());
                var score = cells[1].Trim().Split(':');
                if (score.Count() < 2)
                {
                    // TODO alternative: exception.
                    break;
                }
                var matchup = Matchups.Single(m => m.Id == matchId);
                matchup.HomeGoal = int.Parse(score[0]);
                matchup.AwayGoal = int.Parse(score[1]);

                this.SaveScore(matchup, false);
                matchup.Publish();

                matchupCounter++;

                if (matchupCounter % (Teams.Count() / 2) == 0)
                {
                    UpdateScores();
                }
            }

            if (matchupCounter % (Teams.Count() / 2) != 0)
            {
                UpdateScores();
            }

            Log.Info($" - {matchupCounter} matchups loaded.");

            // TODO consistency check!
        }

        public void LoadSampleData()
        {
            this.LoadData(@"../../../SampleData.txt");
        }

        public void SaveData()
        {
            if (!Directory.Exists(Settings.SavePath))
            {
                Directory.CreateDirectory(Settings.SavePath);
            }

            var path = Path.Combine(Settings.SavePath, @$"jlz-standing-{ DateTime.Now.ToString("yyyyMMdd-HHmmss")}.data");
            StreamWriter file = new StreamWriter(path);
            foreach (var team in Teams)
            {
                file.WriteLine($"{team.Name}, {team.PreSeasonPonits}, {team.SelfAssessmentPoints}");
            }

            file.WriteLine(TimeMatchupSeparator);

            foreach (var matchup in Matchups.Where(m => m.IsPlayed))
            {
                file.WriteLine($"{matchup.Id}, {matchup.HomeGoal}:{matchup.AwayGoal}");
            }

            file.Close();
        }

        public void SaveScore(Matchup matchup)
        {
            this.SaveScore(matchup, true);
        }

        public void SaveScore(Matchup matchup, bool updateScores)
        {
            Log.Debug($"Updating score ({matchup.Id}): {matchup.Home.Name} - {matchup.Away.Name} {matchup.HomeGoal} : {matchup.AwayGoal}");

            matchup.RaiseOnMatchPlayedEvent();

            if (updateScores)
            {
                this.UpdateScores();
            }
        }

        public void SimulateResults()
        {
            Log.Info("Simulating results...");
            var random = new Random(10);

            for (int i = 0; i < this.Rounds.Count; i++)
            {
                foreach (var matchup in this.Rounds[i].Matchups.Where(m => !m.IsPlayed && m.IsFixed))
                {
                    matchup.HomeGoal = random.Next(0, 10);
                    matchup.AwayGoal = random.Next(0, 10);

                    this.SaveScore(matchup, false);
                    matchup.Publish();
                }

                UpdateScores();
            }
        }

        public void UpdateScores()
        {
            ClearScores();

            for (int t = 0; t < this.Teams.Count; t++)
            {
                var team = this.Teams[t];

                var teamMatchups = PlayedMatchups.Where(m => m.Home == team || m.Away == team);

                foreach (var matchup in teamMatchups)
                {
                    team.Matches++;

                    if (matchup.Home == team)
                    {
                        team.GoalsScored += matchup.HomeGoal ?? 0;
                        team.GoalsReceived += matchup.AwayGoal ?? 0;

                        if (matchup.HomeGoal > matchup.AwayGoal)
                        {
                            team.Points += 2;
                        }
                        else if (matchup.HomeGoal == matchup.AwayGoal)
                        {
                            team.Points += 1;
                        }
                    }
                    else
                    {
                        team.GoalsScored += matchup.AwayGoal ?? 0;
                        team.GoalsReceived += matchup.HomeGoal ?? 0;

                        if (matchup.HomeGoal < matchup.AwayGoal)
                        {
                            team.Points += 2;
                        }
                        else if (matchup.HomeGoal == matchup.AwayGoal)
                        {
                            team.Points += 1;
                        }
                    }
                }

                team.Publish();
            }

            this.UpdateRankings();
        }

        private void ClearScores()
        {
            Log.Info("Clearing scores...");
            Teams.ToList().ForEach(t => t.ClearRankingInfo());
        }

        private Configuration LoadConfig(int totalTeams)
        {
            var configFile = @$"../../../config/config{totalTeams}.xml";

            XmlSerializer serializer = new XmlSerializer(typeof(Configuration));

            StreamReader reader = new StreamReader(configFile, Encoding.UTF8);
            var configuration = (Configuration)serializer.Deserialize(reader);
            reader.Close();
            return configuration;
        }

        private void UpdateRankings()
        {
            // No ranking for round 1
            for (int i = 1; i < Rounds.Count; i++)
            {
                var next = i + 1;
                if ((next == Rounds.Count && Rounds[i].HasStarted) || (Rounds[i].HasStarted && !Rounds[next].HasStarted))
                {
                    Rounds[i].UpdateRanking(Rounds.Where(r => r.Number < i + 2).SelectMany(x => x.Matchups.Where(m => m.IsPlayed)));
                }
            }
        }

        public class CommandHandler : ICommand
        {
            private readonly Action action;
            private readonly bool canExecute;

            public CommandHandler(Action action, bool canExecute)
            {
                this.action = action;
                this.canExecute = canExecute;
            }

            public bool CanExecute(object parameter)
            {
                return this.canExecute;
            }

            public void Execute(object parameter)
            {
                this.action();
            }

            public event EventHandler? CanExecuteChanged;
        }

        public class ParameterCommandHandler : ICommand
        {
            private readonly Action<Matchup> action;
            private readonly bool canExecute;

            public ParameterCommandHandler(Action<Matchup> action, bool canExecute)
            {
                this.action = action;
                this.canExecute = canExecute;
            }

            public bool CanExecute(object parameter)
            {
                return this.canExecute;
            }

            public void Execute(object parameter)
            {
                this.action((Matchup)parameter);
            }

            public event EventHandler? CanExecuteChanged;
        }
    }
}