﻿using System;
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
    using System.Windows;
    using System.Windows.Threading;

    [DataContract]
    public class ViewModel //: INotifyPropertyChanged
    {
        private static readonly string TimeMatchupSeparator = "---";
        private static ILog Log = log4net.LogManager.GetLogger(typeof(ViewModel));

        public ICommand LoadCommand => new CommandHandler(this.LoadData, true);
        public IEnumerable<Matchup> PlayedMatchups => Matchups.Where(m => m.IsPlayed);

        [DataMember]
        public ObservableCollection<Round> Rounds { get; } = new ObservableCollection<Round>();

        public ICommand SaveCommand => new CommandHandler(this.SaveData, true);
        public ICommand SaveScoreCommand => new ParameterCommandHandler(this.SaveScore, true);
        public ICommand SimulateResultsCommand => new CommandHandler(this.SimulateResults, true);

        [DataMember]
        public ObservableCollection<Team> Teams { get; set; } = new ObservableCollection<Team>();

        public ICommand UpdateScoresCommand => new CommandHandler(this.UpdateScores, true);
        public IEnumerable<Matchup> Matchups => Rounds.SelectMany(x => x.Matchups);

        private string SaveFileName { set; get; } = @$"jlz-standing-{ DateTime.Now.ToString("yyyyMMdd-HHmmss")}.data";

        public ViewModel()
        {
            // TODO check about using CollectionViewSource instead for data grid binding cf https://stackoverflow.com/questions/19112922/sort-observablecollectionstring-through-c-sharp

            Options.Start(Environment.GetCommandLineArgs());
            if (Options.Current.File != null)
            {
                LoadData(Path.Combine(Options.Current.File));
            }
        }

        public static void GlobalExceptionHandler(object sender, DispatcherUnhandledExceptionEventArgs args)
        {
            Log.Fatal("Unexpected exception occured.", args.Exception);
            MessageBox.Show(args.Exception.Message, Resources.UnexpectedException, MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        }

        public void InitializeMatchups()
        {
            var configuration = Configuration.Current;
            Rounds.Add(new Round(configuration.RoundInfos[0], this, m => RankingSnapshot.None));
            Rounds.Add(new Round(configuration.RoundInfos[1], this, m => new RankingSnapshot(m, new List<int> { 1, 2, 3 })));
            Rounds.Add(new Round(configuration.RoundInfos[2], this, m => new RankingSnapshot(m, new List<int> { 3, 5 })));
            Rounds.Add(new Round(configuration.RoundInfos[3], this, m => new RankingSnapshot(m, new List<int>())));
            Rounds.Add(new Round(configuration.RoundInfos[4], this, m => new RankingSnapshot(m, new List<int>())));
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
            var id = 'A';
            while (e.MoveNext())
            {
                if (e.Current.Equals(TimeMatchupSeparator))
                {
                    // TODO check alternative: FromLine returns null as indicator.
                    break;
                }
                Team team = Team.FromLine(e.Current, id);
                this.Teams.Add(team);
                id++;
                if (id == 'J')
                {
                    id++;
                }
            }

            // TODO right place to do that?
            Settings.TotalTeams = Teams.Count();

            Log.Info($" - {Teams.Count} teams loaded.");

            var orderedTeams = this.Teams.OrderByDescending(t => t.PreSeasonPonits).ToList();

            for (int i = 0; i < 4; i++)
            {
                orderedTeams[i].Seed = i + 1;
            }

            Configuration.Load(Teams.Count());

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

                if (cells.Count() == 4)
                {
                    var home = cells[2].Trim()[0];
                    var away = cells[3].Trim()[0];
                    Log.Info($"Overruling match {matchup.Id}: {home} - {away}\t(previous: {matchup.Home} - {matchup.Away})");
                    matchup.Home = Teams.Single(t => t.Id == home);
                    matchup.Away = Teams.Single(t => t.Id == away);
                    matchup.IsOverrule = true;
                }
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

            this.SaveFileName = Path.GetFileName(fileName);

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

            var dialog = new SaveFileDialog()
            {
                InitialDirectory = Settings.SavePath,
                FileName = SaveFileName,
                CheckPathExists = true,
                DefaultExt = ".data",
                Filter = "JlzQualiData (*.data)|*.data",
                FilterIndex = 1,
                AddExtension = true
            };

            if (dialog.ShowDialog() == true)
            {
                var path = Path.Combine(dialog.FileName);

                var strm = dialog.OpenFile();

                //var path = Path.Combine(Settings.SavePath, @$"jlz-standing-{ DateTime.Now.ToString("yyyyMMdd-HHmmss")}.data");
                StreamWriter file = new StreamWriter(strm);
                foreach (var team in Teams)
                {
                    file.WriteLine($"{team.Name}, {team.PreSeasonPonits}, {team.SelfAssessmentPoints}");
                }

                file.WriteLine(TimeMatchupSeparator);

                foreach (var matchup in Matchups.Where(m => m.IsPlayed))
                {
                    var overruleAddition = "";
                    if (matchup.IsOverrule)
                    {
                        overruleAddition = $", {matchup.Home.Id}, {matchup.Away.Id}";
                    }
                    file.WriteLine($"{matchup.Id}, {matchup.HomeGoal}:{matchup.AwayGoal}{overruleAddition}");
                }

                file.Close();
            }
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

            // Updating summary
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

        private void UpdateRankings()
        {
            // No ranking for round 1
            for (int i = 1; i < Rounds.Count; i++)
            {
                var next = i + 1;
                if (Rounds[i].HasStarted && (next == Rounds.Count() || !Rounds[next].HasStarted))
                {
                    Rounds[i].UpdateRanking(Rounds.Where(r => r.Number < i + 2).SelectMany(x => x.Matchups.Where(m => m.IsPlayed)));
                }
            }
        }

        public class CommandHandler : ICommand
        {
            private readonly Action action;
            private readonly bool canExecute;

            public event EventHandler? CanExecuteChanged;

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
        }

        public class ParameterCommandHandler : ICommand
        {
            private readonly Action<Matchup> action;
            private readonly bool canExecute;

            public event EventHandler? CanExecuteChanged;

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
                this.action((Matchup) parameter);
            }
        }
    }
}