﻿using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows.Input;

namespace JlzQualiTool
{
    using log4net;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Json;
    using System.Windows;
    using System.Windows.Threading;

    [DataContract]
    public class ViewModel //: INotifyPropertyChanged
    {
        private static ILog Log = log4net.LogManager.GetLogger(typeof(ViewModel));

        public ViewModel()
        {
            // TODO check about using CollectionViewSource instead for data grid binding
            // cf https://stackoverflow.com/questions/19112922/sort-observablecollectionstring-through-c-sharp
            this.Teams = new ObservableCollection<Team>();
            this.Rounds = new ObservableCollection<Round>();

            // TODO remove eventually
            LoadData();
            //CreateFirstRoundMatchups();
            InitializeMatchups();
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
            // TODO add correct ranking logic to delegates
            Rounds.Add(new Round(1, new InitialOrderStrategy(Teams.ToList()), Round.Zero, m => RankingSnapshot.None));
            Rounds.Add(new Round(2, new KoStrategy(), Rounds[0],
                m => new RankingSnapshot(m, new List<int> { 1, 2, 3 })));
            // TODO use correct strategies
            Rounds.Add(new Round(3, new KoStrategy(), Rounds[1],
                m => new RankingSnapshot(m, new List<int> { 3, 5 })));
            Rounds.Add(new Round(4, new KoStrategy(), Rounds[2],
                m => new RankingSnapshot(m, new List<int>())));
            Rounds.Add(new Round(5, new KoStrategy(), Rounds[3],
                m => new RankingSnapshot(m, new List<int>())));
        }

        public void LoadData()
        {
            this.Teams.Clear();

            // TODO file selector instead
            var lines = File.ReadLines("../../../SampleData.txt", Encoding.Default);
            foreach (var line in lines)
            {
                Team team = Team.FromLine(line);
                this.Teams.Add(team);
                // TODO improve ID behavior
                team.Id = Teams.Count;
            }

            var orderedTeams = this.Teams.OrderByDescending(t => t.PreSeasonPonits).ToList();

            for (int i = 0; i < 4; i++)
            {
                orderedTeams[i].Seed = i + 1;
            }
        }

        public void SaveData()
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

        public void SaveScore(Matchup matchup)
        {
            Log.Debug($"Updating score: {matchup.Home.Name} - {matchup.Away.Name} {matchup.HomeGoal} : {matchup.AwayGoal}");

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

                    Log.Info($" - Matchup {matchup.Id}: {matchup.HomeGoal} : {matchup.AwayGoal}");

                    this.SaveScore(matchup);

                    // TODO needed?
                    matchup.Publish();
                }
            }

            this.UpdateScores();
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