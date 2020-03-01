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

            LoadSampleData();
            SimulateResults();
        }

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
            Rounds.Add(new Round(1, new InitialOrderStrategy(Teams.ToList()), Round.Zero, m => RankingSnapshot.None));
            Rounds.Add(new Round(2, new KoStrategy(), Rounds[0],
                m => new RankingSnapshot(m, new List<int> { 1, 2, 3 })));
            var round3Pairings = new List<Tuple<int, int>>() {
                new Tuple<int, int>(1, 4),
                new Tuple<int, int>(2, 3),
                new Tuple<int, int>(5, 12),
                new Tuple<int, int>(6, 11),
                new Tuple<int, int>(7, 10),
                new Tuple<int, int>(8, 9),
                new Tuple<int, int>(13, 16),
                new Tuple<int, int>(14, 15)
            };
            Rounds.Add(new Round(3, new RankingStrategy(round3Pairings), Rounds[1],
                m => new RankingSnapshot(m, new List<int> { 3, 5 })));

            var round4Pairings = new List<Tuple<int, int>>() {
                new Tuple<int, int>(1, 2),
                new Tuple<int, int>(3, 8),
                new Tuple<int, int>(4, 7),
                new Tuple<int, int>(5, 6),
                new Tuple<int, int>(9, 14),
                new Tuple<int, int>(10, 13),
                new Tuple<int, int>(11, 12),
                new Tuple<int, int>(15, 16)
            };
            Rounds.Add(new Round(4, new RankingStrategy(round4Pairings), Rounds[2],
                m => new RankingSnapshot(m, new List<int>())));

            // TODO use correct pairings
            Rounds.Add(new Round(5, new KoStrategy(), Rounds[3],
                m => new RankingSnapshot(m, new List<int>())));
        }

        public void LoadData()
        {
            // TODO correct one?
            var dialog = new OpenFileDialog();
            dialog.InitialDirectory = Settings.SavePath;

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
                // TODO improve ID behavior
                team.Id = Teams.Count;
            }

            Log.Info($" - {Teams.Count} teams loaded.");

            var orderedTeams = this.Teams.OrderByDescending(t => t.PreSeasonPonits).ToList();

            for (int i = 0; i < 4; i++)
            {
                orderedTeams[i].Seed = i + 1;
            }

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

                this.SaveScore(matchup);
                matchup.Publish();

                matchupCounter++;
            }

            Log.Info($" - {matchupCounter} matchups loaded.");
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
            Log.Debug($"Updating score ({matchup.Id}): {matchup.Home.Name} - {matchup.Away.Name} {matchup.HomeGoal} : {matchup.AwayGoal}");

            matchup.RaiseOnMatchPlayedEvent();

            this.UpdateScores();
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

                    this.SaveScore(matchup);
                    matchup.Publish();
                }
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

            UpdateRankings();
        }

        private void ClearScores()
        {
            Log.Info("Clearing scores...");
            Teams.ToList().ForEach(t => t.ClearRankingInfo());
        }

        [Obsolete("Saving not done via serializing anymore.")]
        private void SaveBySerializing()
        {
            var knownTypes = new Type[] { typeof(Team), typeof(Round) };

            var ms = new MemoryStream();
            var ser = new DataContractJsonSerializer(typeof(ViewModel), knownTypes);
            ser.WriteObject(ms, this);
            byte[] json = ms.ToArray();
            ms.Close();

            if (!Directory.Exists(Settings.SavePath))
            {
                Directory.CreateDirectory(Settings.SavePath);
            }

            File.WriteAllText(Path.Combine(Settings.SavePath, $"jlz-standing-{ DateTime.Now.ToString("yyyyMMdd-HHmmss")}.json"), Encoding.UTF8.GetString(json, 0, json.Length));
        }

        private void UpdateRankings()
        {
            // No ranking for round 1
            for (int i = 1; i < Rounds.Count; i++)
            {
                Log.Info($"Updating ranking for round {i}.");
                Rounds[i].UpdateRanking(Rounds.Where(r => r.Number < i + 2).SelectMany(x => x.Matchups));
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