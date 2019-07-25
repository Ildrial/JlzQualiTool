using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows.Input;

namespace QualiTool
{
    using JlzQualiTool;
    using log4net;
    using QualiTool.Extensions;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Json;

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
            Log.Debug("ViewModel started.");
        }

        public ICommand CreateMatchups1Command => new CommandHandler(this.CreateFirstRoundMatchups, true);

        public ICommand CreateMatchups2Command => new CommandHandler(this.CreateSecondRoundMatchups, true);

        public ICommand LoadCommand => new CommandHandler(this.LoadData, true);

        [DataMember]
        public ObservableCollection<Round> Rounds { get; }

        public ICommand SaveCommand => new CommandHandler(this.SaveData, true);

        [DataMember]
        public ObservableCollection<Team> Teams { get; set; }

        public ICommand UpdateScoresCommand => new CommandHandler(this.UpdateScores, true);

        public void CreateFirstRoundMatchups()
        {
            var firstRound = new Round();

            var shuffeledTeams = this.Teams.Shuffle();
            for (int i = 0; i < 4; i++)
            {
                var home = shuffeledTeams.First(t => t.Seed == i + 1);
                var away = shuffeledTeams.First(t => t.Seed == 0 && t.Matchups.Count == 0 && t != home);
                firstRound.CreateAndAddMatchup(home, away);

                var home2 = shuffeledTeams.First(t => t.Seed == 0 && t.Matchups.Count == 0);
                var away2 = shuffeledTeams.First(t => t.Seed == 0 && t.Matchups.Count == 0 && t != home2);
                firstRound.CreateAndAddMatchup(home2, away2);
            }
            this.Rounds.Add(firstRound);
        }

        public void CreateSecondRoundMatchups()
        {
            var secondRound = new Round();
            this.Rounds.Add(secondRound);

            var frm = this.Rounds[0].Matchups.ToList().OrderBy(m => m.Id).ToList();

            if (frm.Count() % 2 == 0)
            {
                for (int i = 0; i < frm.Count(); i = i + 2)
                {
                    secondRound.CreateAndAddMatchup(frm[i].Winner, frm[i + 1].Winner);
                    secondRound.CreateAndAddMatchup(frm[i].Loser, frm[i + 1].Loser);
                }
            }
            else
            {
                // TODO
            }
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

            var lines = File.ReadLines("../../../SampleData.txt", Encoding.Default);
            if (!Directory.Exists(Settings.SavePath))
            {
                Directory.CreateDirectory(Settings.SavePath);
            }
            File.WriteAllText(Path.Combine(Settings.SavePath, $"jlz-standing-{ DateTime.Now.ToString("yyyyMMdd-HHmmss")}.json"), Encoding.UTF8.GetString(json, 0, json.Length));
        }

        public void UpdateScores()
        {
            for (int t = 0; t < this.Teams.Count; t++)
            {
                var team = this.Teams[t];
                for (int i = 0; i < this.Rounds.Count; i++)
                {
                    var matchup = this.Rounds[i].Matchups.Single(m => m.Home == team || m.Away == team);

                    team.Matches++;

                    if (matchup.Home == team)
                    {
                        team.GoalsScored += matchup.HomeGoal;
                        team.GoalsReceived += matchup.AwayGoal;

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
                        team.GoalsScored += matchup.AwayGoal;
                        team.GoalsReceived += matchup.HomeGoal;

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

            public event EventHandler CanExecuteChanged;
        }
    }
}